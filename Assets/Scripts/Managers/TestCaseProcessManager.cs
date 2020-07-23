/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

using System;

using System.Collections;
using System.Collections.Generic;

using System.Diagnostics;
using System.IO;
using System.Text;


namespace Simulator
{
    public class TestCaseProcess : Process
    {
        enum Signal : int
        {
            SIGHUP  = 1,
            SIGINT  = 2,
            SIGKILL = 9,
            SIGUSR1 = 10,
            SIGUSR2 = 12,
            SIGTERM = 15,
        }

        private StringBuilder outputData = new StringBuilder();
        private StringBuilder errorData = new StringBuilder();

        public String GetOutputData()
        {
            return outputData.ToString();
        }

        public bool HasErrorData()
        {
            return (errorData.Length > 0);
        }

        public String GetErrorData()
        {
            return errorData.ToString();
        }

        public TestCaseProcess(string executibleName, IDictionary<string, string> environment, string workingDirectory)
        {
            StartInfo.FileName = executibleName;
            StartInfo.UseShellExecute = false;
            StartInfo.RedirectStandardOutput = true;
            StartInfo.RedirectStandardInput = true;
            StartInfo.RedirectStandardError = true;

            StartInfo.WorkingDirectory = workingDirectory;

            if (environment != null)
            {
                foreach (var envvar in environment)
                {
                    StartInfo.EnvironmentVariables.Add(envvar.Key.ToString(), envvar.Value.ToString());
                }
            }

            EnableRaisingEvents = true;
            OutputDataReceived += new DataReceivedEventHandler( DataReceived );
            ErrorDataReceived += new DataReceivedEventHandler( ErrorReceived );
        }

        void SendSignal(Signal signal)
        {
#if UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
            var kill_process = new Process();

            using (Process proc = new Process())
            {
                Console.WriteLine("[PROC][{0}] Sending signal {1}", Id, signal);
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.FileName = "kill";
                proc.StartInfo.Arguments = $"--signal {(int)signal} {Id}";
                proc.Start();

                proc.WaitForExit();
                Console.WriteLine("[PROC][{0}] Send signal done with status {1}", Id, proc.ExitCode);
            }
#else
            UnityEngine.Debug.LogError($"[PROC] TestCaseProcess.SendSignal is supported on {Application.platform}");
#endif
        }

        public new void Start()
        {
            base.Start();
            BeginOutputReadLine();
            BeginErrorReadLine();
        }

        void DataReceived( object sender, DataReceivedEventArgs eventArgs )
        {
            var proc = (Process)sender;
            // UnityEngine.Debug.Log($"[DataReceived] from:{sender}");
            Console.WriteLine("[PROC][{0}][OUT] {1}", proc.Id, eventArgs.Data);

            if (outputData != null)
            {
                outputData.Append(eventArgs.Data);
            }
        }
        
        void ErrorReceived( object sender, DataReceivedEventArgs eventArgs )
        {
            var proc = (Process)sender;
            Console.WriteLine("[PROC][{0}][ERR] {1}", proc.Id, eventArgs.Data);

            if (errorData != null)
            {
                errorData.Append(eventArgs.Data);
            }
        }

        public void Terminate(int timeout)
        {
            try
            {
                if (HasExited)
                {
                    Console.WriteLine($"[PROC][{Id}] Process is already exited");
                    return;
                }

                Console.WriteLine($"[PROC][{Id}] Sending SIGINT");
                SendSignal(Signal.SIGINT);

                WaitForExit(timeout);

                if (!HasExited)
                {
                    Console.WriteLine($"[PROC][{Id}] Sending SIGKILL");
                    SendSignal(Signal.SIGKILL);
                    WaitForExit(timeout);
                }

                if (!HasExited)
                {
                    Console.WriteLine($"[PROC][{Id}] Killing process");
                    Kill();
                }
            }
            catch (InvalidOperationException e)
            {
                UnityEngine.Debug.LogError($"[PROC][{Id}] Failed to terminate process: {e.Message}");
            }
        }
    }

    public struct TestCaseFinishedArgs
    {
        public int ExitCode;
        public string OutputData;
        public string ErrorData;

        public bool Failed {get {return ExitCode != 0;} }

        public TestCaseFinishedArgs(int exitCode, string outputData, string errorData)
        {
            ExitCode = exitCode;
            OutputData = outputData;
            ErrorData = errorData;
        }

        public string ToString()
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
        enum ErrorCodes : int
        {
            ProcessStartError = 127,
        }

        public delegate void Finshed(TestCaseFinishedArgs args);
        public event Finshed OnFinished;

        TestCaseProcess Process;

        public bool StartProcess(string runtimeType, IDictionary<string,string> environment, string workingDirectory)
        {
            if (Process != null) {
                UnityEngine.Debug.LogError($"[PROC][main] Failed to start test case process: process is already running");
                return false;
            }

            UnityEngine.Debug.Log($"[PROC][main] Prepare external test case type:{runtimeType} workingDirectory:{workingDirectory}");

            var testCaseRunner = GetTestCaseRunnerEntrypoint(runtimeType);

            try
            {
                var proc = new TestCaseProcess(testCaseRunner, environment, workingDirectory);

                // Event handlers
                proc.Exited += new EventHandler(ProcessExited);
                proc.Start();

                UnityEngine.Debug.Log($"[PROC][main] Successfully launched app Id={proc.Id}");

                Process = proc;
            }
            catch( Exception e )
            {
                UnityEngine.Debug.LogError($"Unable to launch app '{testCaseRunner}': {e.Message}");
            }

            return Process != null;
        }

        void ProcessExited(object sender, System.EventArgs e)
        {
            var proc = (TestCaseProcess)sender;
            UnityEngine.Debug.Log($"[PROC][main] Process #{proc.Id} exited with result {proc.ExitCode}");

            var args = new TestCaseFinishedArgs(proc.ExitCode, proc.GetOutputData(), proc.GetErrorData());
            OnFinished?.Invoke(args);
        }
        
        public void Terminate()
        {
            if (Process != null)
            {
                Process.Terminate(5000);
                Process = null;
            }
            else 
            {
                Console.WriteLine("[PROC][main] Process is not running. Nothing to terminate");
            }
        }

        void OnApplicationQuit()
        {
            Terminate();
        }

        private string GetTestCaseRunnerEntrypoint(string runtimeType)
        {
            /* Minimal runtime folder layout:
                <SimulatorRoot>
                    ├── simulator
                    ├── ...
                    └── TestCaseRunner
                        └── pythonAPI        <- runtime type
                            └── run          <- executible with predefined name
            */

            var runnersRoot = Path.Combine(Application.dataPath, "..", "TestCaseRunner");
            return Path.Combine(runnersRoot, runtimeType, "run");
        }
    }
}
