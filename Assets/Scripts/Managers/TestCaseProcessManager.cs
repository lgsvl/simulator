/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Simulator.Web;
using Docker.DotNet;

namespace Simulator
{
    public struct TestCaseFinishedArgs
    {
        public int ExitCode;
        public string OutputData;
        public string ErrorData;

        public bool Failed => ExitCode != 0;

        public override string ToString()
        {
            var builder = new StringBuilder();

            builder.AppendFormat("TestCaseFinishedArgs(ExitCode={0}, output: {1} bytes error: {2} bytes)",
                                 ExitCode,
                                 OutputData.Length,
                                 ErrorData.Length);

            return builder.ToString();
        }
    }

    public class TestCaseProcessManager : MonoBehaviour
    {
        readonly DockerClient dockerClient;

        class ContainerData
        {
            public DockerConsole console;
            public string imageName;
        }

        readonly Dictionary<string, ContainerData> containers = new Dictionary<string, ContainerData>();

        TestCaseProcessManager()
        {
            Uri dockerUri;
            if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
            {
                // named pipe appears to be broken on unity/mono/windows
                //                dockerUri = new Uri("npipe://./pipe/docker_engine");
                dockerUri = new Uri("tcp://127.0.0.1:2375");
            }
            else
            {
                dockerUri = new Uri("unix:///var/run/docker.sock");
            }

            dockerClient = new Docker.DotNet.DockerClientConfiguration(dockerUri, defaultTimeout: TimeSpan.FromSeconds(1)).CreateClient();
        }

        void Awake()
        {
            Debug.Log("TC runner initialized");
        }

        bool terminating = false;

        public async Task<string> RunContainer(Docker.DotNet.Models.AuthConfig authConfig, string registry, string image, string imageTag, IList<string> command, IDictionary<string, string> environment, string hostWorkingDirectory)
        {
            List<string> env = environment.Select(kv => kv.Key + "=" + kv.Value).ToList();
            env.Add("LC_ALL=en_EN.utf8");
            env.Add("TERM=dumb");
            var console = new DockerConsole(this, image);
            SimulatorConsole.Instance.AddTab(console);
            SimulatorConsole.Instance.ChangeTab(console);
            string registryImage = string.IsNullOrEmpty(registry) ? image : registry + "/" + image;

            var registryImageTag = $"{registryImage}:{imageTag}";

            var createConfig = new Docker.DotNet.Models.ImagesCreateParameters
            {
                FromImage = registryImage,
                Tag = imageTag
            };

            await dockerClient.Images.CreateImageAsync(createConfig, authConfig, console);
            Debug.Log("pulled image");

            var createParams = new Docker.DotNet.Models.CreateContainerParameters()
            {
                Hostname = "",
                Domainname = "",
                Image = registryImageTag,
                Cmd = command,
                User = "1000:1000",
                AttachStdout = true,
                AttachStderr = true,
                AttachStdin = true,
                Tty = true,
                OpenStdin = true,
                StdinOnce = true,
                HostConfig = new Docker.DotNet.Models.HostConfig
                {
                    Binds = new[] { hostWorkingDirectory + ":/scenarios" },
                    NetworkMode = "host",
                    RestartPolicy = new Docker.DotNet.Models.RestartPolicy { Name = Docker.DotNet.Models.RestartPolicyKind.No, MaximumRetryCount = 0 },
                    AutoRemove = false,
                    Privileged = false,
                    PublishAllPorts = false,
                    ReadonlyRootfs = false,
                },
                Env = env,
                WorkingDir = "/scenarios",
            };
            var response = await dockerClient.Containers.CreateContainerAsync(createParams);
            console.CompleteProgress();
            Debug.Log("created container " + response.ID + " " + string.Join(", ", response.Warnings));
            var container = await dockerClient.Containers.InspectContainerAsync(response.ID);

            var containerAttachParameters = new Docker.DotNet.Models.ContainerAttachParameters
            {
                Stream = true,
                Stderr = true,
                Stdin = true,
                Stdout = true,
            };

            var containerStream = await dockerClient.Containers.AttachContainerAsync(container.ID, true, containerAttachParameters);

            console.WriteLine($"Image: {image}");
            console.WriteLine($"Created container: {container.ID} {container.Name}");
            console.WriteLine($"working directory: {hostWorkingDirectory}");
            console.WriteLine($"environment: {string.Join("; ", env)}");
            console.WriteLine( "---------------------------------");
            console.ReadStream(containerStream);

            var startParams = new Docker.DotNet.Models.ContainerStartParameters();

            containers.Add(
                container.ID,
                new ContainerData
                {
                    imageName = registryImageTag,
                    console = console,
                });
            var result = await dockerClient.Containers.StartContainerAsync(container.ID, startParams);
            if (!result)
            {
                throw new Exception("Docker launch failed");
            }

            return container.ID;
        }

        public async Task<TestCaseFinishedArgs> GetContainerResult(string containerId)
        {
            await dockerClient.Containers.WaitContainerAsync(containerId);
            return await HandleFinished(containerId);
        }

        public async Task<TestCaseFinishedArgs> StartProcess(TemplateData template, string workingDirectory)
        {
            var environment = new Dictionary<string, string>();
            SimulationConfigUtils.UpdateTestCaseEnvironment(template, environment);
            var runArgs = new List<string> { "run" };
            if (environment.ContainsKey("SIMULATOR_TC_FILENAME"))
            {
                runArgs.Add(environment["SIMULATOR_TC_FILENAME"]);
            }

            var imageUri = new Uri(template.Runner.Docker.Image, UriKind.RelativeOrAbsolute);
            if (!imageUri.IsAbsoluteUri)
            {
                imageUri = new Uri(new Uri(Config.CloudUrl), imageUri);
            }

            var registry = imageUri.Authority;
            var parts = imageUri.PathAndQuery.Split(':');
            string dockerImage = parts[0];
            string dockerTag = parts.Length > 1 ? parts.Last() : "latest";

            var authConfig = AuthConfigFromTemplate(template);
            var id = await RunContainer(authConfig, registry, dockerImage, dockerTag, runArgs, environment, workingDirectory);
            return await GetContainerResult(id);
        }

        Docker.DotNet.Models.AuthConfig AuthConfigFromTemplate(TemplateData template)
        {
            return new Docker.DotNet.Models.AuthConfig
            {
                Username = template.Runner.Docker.Auth.Username,
                Password = template.Runner.Docker.Auth.Password,
                RegistryToken = template.Runner.Docker.Auth.PullToken
            };
        }

        public async Task Terminate(uint seconds = 5)
        {
            Debug.Log("TCRunner: terminating containers");
            while (terminating)
            {
                await Task.Delay(1000);
            }

            try
            {
                terminating = true;
                var stopParams = new Docker.DotNet.Models.ContainerStopParameters
                {
                    WaitBeforeKillSeconds = seconds,
                };
                foreach (var containerId in containers.Keys)
                {
                    var data = containers[containerId];
                    Debug.Log($"Terminating container {containerId.Substring(0, 12)} {data.imageName}");
                    await dockerClient.Containers.StopContainerAsync(containerId, stopParams);
                }
            }
            finally
            {
                terminating = false;
            }
        }

        async void OnApplicationQuit()
        {
            try
            {
                Debug.Log("TCRunner: terminating containers on application quit");
                await Terminate(1);
            }
            catch (Exception e)
            {
                Debug.LogError("failed to stop some containers");
                Debug.LogException(e);
            }
        }

        private async Task<TestCaseFinishedArgs> HandleFinished(string containerId)
        {
            var console = containers[containerId].console;
            var inspect = await dockerClient.Containers.InspectContainerAsync(containerId);
            console.WriteLine("Exit Code: " + inspect.State.ExitCode);

            var args = new TestCaseFinishedArgs
            {
                ExitCode = (int)inspect.State.ExitCode,
                OutputData = console.cleanLog,
                ErrorData = inspect.State.ExitCode != 0 ? console.cleanLog : string.Empty,
            };

            var rmParams = new Docker.DotNet.Models.ContainerRemoveParameters
            {
                Force = true,
                RemoveLinks = false,
                RemoveVolumes = false,
            };
            await dockerClient.Containers.RemoveContainerAsync(containerId, rmParams);
            containers.Remove(containerId);
            if (inspect.State.ExitCode != 0 && Application.isPlaying)
            {
                SimulatorConsole.Instance.Visible = true;
                SimulatorConsole.Instance.ChangeTab(console);
            }

            return args;
        }
    }

    public class DockerConsole : ConsoleTab, IProgress<Docker.DotNet.Models.JSONMessage>
    {
        MultiplexedStream containerStream = null;
        public CancellationTokenSource readCancellationTokenSource;

        public string cleanLog = string.Empty;

        public DockerConsole(TestCaseProcessManager manager, string name)
        {
            readCancellationTokenSource = new CancellationTokenSource();
            Name = name;
        }

        internal override async void HandleCharReceived(char addedChar)
        {
            try
            {
                if (containerStream != null)
                {
                    var buffer = Encoding.UTF8.GetBytes(new[] { addedChar });
                    await containerStream.WriteAsync(buffer, 0, buffer.Length, readCancellationTokenSource.Token);
                }
                else if (addedChar == '\n')
                {
                    SimulatorConsole.Instance.RemoveTab(this);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static readonly Dictionary<KeyCode, byte[]> KeySequences = new Dictionary<KeyCode, byte[]>
        {
            [KeyCode.UpArrow] = new byte[] {0x1b, 0x5b, 0x41},
            [KeyCode.DownArrow] = new byte[] {0x1b, 0x5b, 0x42},
            [KeyCode.RightArrow] = new byte[] {0x1b, 0x5b, 0x43},
            [KeyCode.LeftArrow] = new byte[] {0x1b, 0x5b, 0x44},
            [KeyCode.Backspace] = new byte[] {0x7f},
            [KeyCode.Delete] = new byte[] {0x1b, 0x5b, 0x33, 0x7e},
            [KeyCode.Home] = new byte[] {0x1b, 0x5b, 0x48},
            [KeyCode.End] = new byte[] {0x1b, 0x5b, 0x46},
        };
        
        internal override async void HandleSpecialKey(KeyCode keyCode)
        {
            try
            {
                if(KeySequences.TryGetValue(keyCode, out var buffer))
                {
                    await containerStream.WriteAsync(buffer, 0, buffer.Length, readCancellationTokenSource.Token);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        class DockerProgress : IProgressTask
        {
            public DockerProgress(string description)
            {
                Description = description;
                Progress = 0;
            }

            public float Progress { get; internal set; }
            public string Description { get; internal set; }
            public event Action<IProgressTask> OnUpdated = delegate { };
            public event Action<IProgressTask, bool, Exception> OnCompleted = delegate { };
            public void Update(Docker.DotNet.Models.JSONMessage value)
            {
                Description = value.Status;
                if (value.Progress != null && value.Progress.Total > 0)
                {
                    Progress = (float)value.Progress.Current / value.Progress.Total;
                }
                else
                {
                    Progress = 1.0f;
                }
                OnUpdated(this);
                if (value.Status == "Pull complete")
                {
                    OnCompleted(this, true, null);
                }
            }
            public void Complete(bool success, Exception ex = null)
            {
                OnCompleted(this, success, ex);
            }
        }
        Dictionary<string, DockerProgress> progress = new Dictionary<string, DockerProgress>();

        public void Report(Docker.DotNet.Models.JSONMessage value)
        {
            if (!string.IsNullOrWhiteSpace(value.ErrorMessage))
            {
                WriteLine("Error: " + value.ErrorMessage);
            }

            if (!string.IsNullOrWhiteSpace(value.ID))
            {
                if (progress.TryGetValue(value.ID, out DockerProgress t))
                {
                    t.Update(value);
                }
                else
                {
                    var p = new DockerProgress("Docker: " + value.Status + " " + value.ID);
                    p.OnCompleted += (t, b, e) => { progress.Remove(value.ID); };
                    progress.Add(value.ID, p);
                    TaskProgressManager.Instance.AddTask(p);
                }
            }
        }

        internal void CompleteProgress()
        {
            // it is removed on completion, while we iterate over it
            var temp = progress.Values.ToList();
            foreach (var p in temp)
            {
                p.Complete(true);
            }
        }

        public async void ReadStream(MultiplexedStream stream)
        {
            containerStream = stream;
            try
            {
                MatchEvaluator matchEval = new MatchEvaluator(AnsiEscapeHandler);
                MultiplexedStream.ReadResult result;
                do
                {
                    var buffer = new byte[1024 * 4];
                    result = await containerStream.ReadOutputAsync(buffer, 0, buffer.Length, readCancellationTokenSource.Token);

                    var str = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    str = ansiEscape.Replace(str, matchEval);

                    cleanLog += ansiEscape.Replace(str, string.Empty);

                    int spanStart = 0;
                    while (spanStart < str.Length)
                    {
                        var foundIndex = str.IndexOfAny(new[] { '\u0007', '\u0008' }, spanStart);
                        if (foundIndex == -1)
                        {
                            Append(str.Substring(spanStart));
                            break;
                        }

                        if (spanStart < foundIndex)
                        {
                            Append(str.Substring(spanStart, foundIndex - spanStart));
                        }

                        switch (str[foundIndex])
                        {
                            case '\u0008': Backspace(); break;
                            case '\u0007': OnBell(); break;
                        }

                        spanStart = foundIndex + 1;
                    }
                }
                while (!result.EOF);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            containerStream = null;
            WriteLine("Process has ended, press enter key to close tab.");
        }

        static readonly Regex ansiEscape = new Regex("\u001b\\[([0-9;]*)([a-zA-Z])");
        Color lastColor = Color.clear;
        private string AnsiEscapeHandler(Match match)
        {
            var result = string.Empty;
            var codes = match.Groups[1].Value.Split(';');
            var makeBold = false;
            Color baseColor = Color.clear;

            foreach (var code in codes)
            {
                switch (code)
                {
                    case "0":
                    case "":
                        if (lastColor != Color.clear) result += "</color>";
                        lastColor = Color.clear;
                        break;
                    case "1": makeBold = true; break;
                    case "30": baseColor = Color.black; break;
                    case "31": baseColor = new Color(0.5f, 0, 0); break;
                    case "32": baseColor = new Color(0, 0.5f, 0); break;
                    case "33": baseColor = new Color(0.5f, 0.5f, 0); break;
                    case "34": baseColor = new Color(0, 0, 0.5f); break;
                    case "35": baseColor = new Color(0.5f, 0, 0.5f); break;
                    case "36": baseColor = new Color(0, 0.5f, 0.5f); break;
                    case "37": baseColor = new Color(0.75f, 0.75f, 0.75f); break;
                    case "90": baseColor = new Color(0.5f, 0.5f, 0.5f); break;
                    case "91": baseColor = Color.red; break;
                    case "92": baseColor = Color.green; break;
                    case "93": baseColor = Color.yellow; break;
                    case "94": baseColor = Color.blue; break;
                    case "95": baseColor = Color.magenta; break;
                    case "96": baseColor = Color.cyan; break;
                    case "97": baseColor = Color.white; break;
                }
            }

            if (makeBold)
            {
                if (baseColor == Color.clear) baseColor = Color.white;
                else baseColor *= 2.0f;
            }

            if (baseColor != Color.clear)
            {
                result += $"<color=#{ColorUtility.ToHtmlStringRGB(baseColor)}>";
            }

            lastColor = baseColor;
            return result;
        }
    }
}
