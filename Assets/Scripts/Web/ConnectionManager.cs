/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Newtonsoft.Json.Linq;
using Simulator;
using Simulator.Database;
using Simulator.Database.Services;
using Simulator.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;

public class ConnectionManager : MonoBehaviour
{
    public enum ConnectionStatus
    {
        Offline,
        Connecting,
        Connected,
        Online
    }
    
    public static ConnectionStatus Status = ConnectionStatus.Offline;
    public static ConnectionManager instance;
    public static CloudAPI API;
    public SimulatorInfo simInfo;
    static int unityThread;
    static Queue<Action> runInUpdate = new Queue<Action>();
    static int[] timeOutSequence = new[] { 1, 5, 15, 30};
    int timeoutAttempts;
    ClientSettingsService service;
    public string LinkUrl => Config.CloudUrl + "/clusters/link?token=" + simInfo.linkToken;

    private void Start()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(this);
        instance = this;
        unityThread = Thread.CurrentThread.ManagedThreadId;
        service = new ClientSettingsService();
        ClientSettings settings = service.GetOrMake();
        API = new CloudAPI(new Uri(Config.CloudUrl), Config.SimID);

        if (settings.onlineStatus)
        {
            ConnectionStatusEvent();
        }
    }

    private void OnDestroy()
    {
        if (instance == this && Status != ConnectionStatus.Offline)
        {
            Disconnect();
        }
    }

    async Task Connect()
    {
        try
        {
            simInfo = CloudAPI.GetInfo();
            Status = ConnectionStatus.Connecting;
            RunOnUnityThread(() =>
            {
                ConnectionUI.instance.UpdateStatus();
                ConnectionUI.instance.statusButton.interactable = false;
            });
            
            foreach (var timeOut in timeOutSequence)
            {
                try
                {
                    var reader = await API.Connect(simInfo);
                    await ReadResponse(reader);
                    break;
                }
                catch (HttpRequestException ex)
                {
                    // temporary network issue, we'll retry
                    Debug.Log(ex.Message+", reconnecting after "+timeOut+" seconds");
                    await Task.Delay(1000 * timeOut);
                }
                if (Status == ConnectionStatus.Offline)
                {
                    Debug.Log("User cancelled connection.");
                    break;
                }
            }
        }
        catch (CloudAPI.NoSuccessException ex)
        {
            // WISE told us it does not like us, so stop reconnecting
            Debug.Log($"WISE backend reported error: {ex.Message}, will not reconnect");
        }
        catch (TaskCanceledException)
        {
            Debug.Log("Linking task canceled.");
        }
        catch (System.Net.Sockets.SocketException se)
        {
            Debug.Log($"Could not reach WISE SSE at {Config.CloudUrl}: {se.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }

        Debug.Log("Giving up reconnecting.");
        Disconnect();
    }

    async Task ReadResponse(StreamReader reader)
    {
        await Task.Run(async () =>
        {
            while (!reader.EndOfStream)
            {
                //We are ready to read the stream
                var line = await reader.ReadLineAsync();
                await Parse(line);
            }
            Debug.Log("WISE connection closed");
            reader.Dispose();
        });
    }

    public void Disconnect()
    {
        try
        {
            API.Disconnect();
            RunOnUnityThread(() =>
            {
                Status = ConnectionStatus.Offline;
                ConnectionUI.instance?.UpdateStatus();
            });
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    async Task Parse(string s)
    {
        // Debug.Log(s);
        if (string.IsNullOrEmpty(s))
            return;

        try
        {
            if (s.StartsWith("data:") && !string.IsNullOrEmpty(s.Substring(6)))
            {
                JObject deserialized = JObject.Parse(s.Substring(5));
                if (deserialized != null && deserialized.HasValues)
                {
                    var status = deserialized.GetValue("status");
                    if (status != null)
                    {
                        switch (status.ToString())
                        {
                            case "Unrecognized":
                                RunOnUnityThread(() =>
                                {
                                    Status = ConnectionStatus.Connected;
                                    ConnectionUI.instance.UpdateStatus();
                                });
                                break;
                            case "OK":
                                RunOnUnityThread(() =>
                                {
                                    Status = ConnectionStatus.Online;
                                    ConnectionUI.instance.UpdateStatus();
                                });
                                break;
                            case "Config":
                                RunOnUnityThread(() =>
                                {
                                    Status = ConnectionStatus.Online;
                                    ConnectionUI.instance.UpdateStatus();
                                    ConnectionUI.instance.statusButton.interactable = false;

                                    SimulationData simData;
                                    try 
                                    {
                                        simData = deserialized["data"].ToObject<SimulationData>();
                                        SimulationConfigUtils.ProcessKnownTemplates(ref simData);
                                    }
                                    catch (Exception e) when (e is InvalidCastException 
                                                              || e is NullReferenceException)
                                    {
                                        Debug.LogError($"[CONN] Failed to parse Config data: '{s}'");
                                        Debug.LogException(e);
                                        throw;
                                    }
                                    Loader.StartSimulation(simData);
                                });
                                break;
                            case "Disconnect":
                                RunOnUnityThread(() =>
                                {
                                    Disconnect();
                                });
                                break;
                            case "Stop":
                                if (Loader.Instance.Status == SimulatorStatus.Idle || Loader.Instance.Status == SimulatorStatus.Stopping)
                                {
                                    SimulationData simData = Newtonsoft.Json.JsonConvert.DeserializeObject<SimulationData>(deserialized.GetValue("data").ToString());
                                    Debug.Log("not running");
                                    await API.UpdateStatus("Stopping", simData.Id, "stop requested but was not running simulation");
                                    await API.UpdateStatus("Idle", simData.Id, "");
                                    return;
                                }
                                Loader.StopAsync();
                                break;
                            default:
                                Debug.LogError($"Unknown Status '{status.ToString()}'! Disconnecting.");
                                RunOnUnityThread(() =>
                                {
                                    Disconnect();
                                });
                                break;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    public static void RunOnUnityThread(Action action)
    {
        if (unityThread == Thread.CurrentThread.ManagedThreadId)
        {
            action();
        }
        else
        {
            lock (runInUpdate)
            {
                runInUpdate.Enqueue(action);
            }

        }
    }

    public async void UpdateStatus(string status, string simGuid, string message = "")
    {
        try
        {
            await API.UpdateStatus(status, simGuid, message);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    private void Update()
    {
        while (runInUpdate.Count > 0)
        {
            Action action = null;
            lock (runInUpdate)
            {
                if (runInUpdate.Count > 0)
                {
                    action = runInUpdate.Dequeue();
                }
            }
            action?.Invoke();
        }
    }

    async void RunConnectTask()
    {
        await Connect();
    }

    public void ConnectionStatusEvent()
    {
        switch (Status)
        {
            case ConnectionStatus.Offline:
                RunConnectTask();
                service.UpdateOnlineStatus(true);
                break;
            case ConnectionStatus.Connecting:
            case ConnectionStatus.Connected:
            case ConnectionStatus.Online:
                Disconnect();
                service.UpdateOnlineStatus(false);
                break;
            default:
                break;
        }
    }
}

public class CloudAPI 
{
    HttpClient client = new HttpClient();
    string SimId;
    Uri InstanceURL;

    CancellationTokenSource requestTokenSource = new CancellationTokenSource();
    private const uint fetchLimit = 50;
    StreamReader onlineStream;

    public CloudAPI(Uri instanceURL, string simId)
    {
        InstanceURL = instanceURL;
        SimId = simId;
        Console.WriteLine("[CONN] Instance URL {0}", InstanceURL.AbsoluteUri);
    }

    public class NoSuccessException: Exception
    {
        public NoSuccessException(string status) :base(status) { }
    }

    public async Task<StreamReader> Connect(SimulatorInfo simInfo)
    {
        if (onlineStream != null)
        {
            onlineStream.Close();
            onlineStream.Dispose();
            onlineStream = null;
        }

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(simInfo);
        HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, new Uri(InstanceURL, "/api/v1/clusters/connect"));
        message.Content = new StringContent(json, Encoding.UTF8, "application/json");
        message.Headers.Add("SimId", Config.SimID);
        message.Headers.Add("Accept", "application/json");
        message.Headers.Add("Connection", "Keep-Alive");
        message.Headers.Add("X-Accel-Buffering", "no");

        Console.WriteLine("[CONN] Connecting to WISE");

        var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, requestTokenSource.Token).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine("[CONN] Failed to connect to WISE");
            var content = await response.Content.ReadAsStringAsync();
            throw new NoSuccessException(response.StatusCode.ToString() + " " + content);
        }
        Console.WriteLine("[CONN] Connected to WISE.");
        onlineStream = new StreamReader(await response.Content.ReadAsStreamAsync());
        return onlineStream;
    }

    // use this if you waqnt to make sure the connection succeeded but don't care about
    // messages before the connection ok message
    public async Task EnsureConnectSuccess()
    {
        while (!onlineStream.EndOfStream)
        {
            var line = await onlineStream.ReadLineAsync();
            if (line.StartsWith("data:") && !string.IsNullOrEmpty(line.Substring(6)))
            {
                JObject deserialized = JObject.Parse(line.Substring(5));
                if (deserialized != null && deserialized.HasValues)
                {
                    switch (deserialized.GetValue("status").ToString())
                    {
                        case "OK": return;
                        case "Unrecognized": throw new Exception("Simulator is not linked. Enter play mode to re-link.");
                    }
                }
            }
        }
        throw new Exception("Connection Closed");
    }

    public Task<DetailData> Get<DetailData>(string cloudId) where DetailData: CloudAssetDetails
    {
        var meta = (CloudData) Attribute.GetCustomAttribute(typeof(DetailData), typeof (CloudData));
        return GetApi<DetailData>($"{meta.ApiPath}/{cloudId}");
    }

    public async Task<DetailData[]> GetLibrary<DetailData>() where DetailData: CloudAssetDetails
    {
        var meta = (CloudData)Attribute.GetCustomAttribute(typeof(DetailData), typeof(CloudData));
        List<DetailData> result = new List<DetailData>();
        LibraryList<DetailData> data;
        do
        {
            data = await GetApi<LibraryList<DetailData>>($"{meta.ApiPath}?display=sim&limit={fetchLimit}&offset={result.Count}");
            result.AddRange(data.rows);
        } while (result.Count < data.Count && data.Count > 0);
        return result.ToArray();
    }

    public async Task<DetailData> GetByIdOrName<DetailData>(string cloudIdOrName) where DetailData: CloudAssetDetails
    {
        var meta = (CloudData) Attribute.GetCustomAttribute(typeof(DetailData), typeof (CloudData));
        if (!Guid.TryParse(cloudIdOrName, out Guid guid))
        {
            var library = await GetLibrary<DetailData>();

            var matches = library.Where(m => m.Name == cloudIdOrName).ToList();
            if (matches.Count > 1)
            {
                throw new Exception($"multiple assets matching name '{cloudIdOrName}' in your library, please use Id");
            }
            if (matches.Count == 0)
            {
                throw new Exception($"no assets matching name '{cloudIdOrName}' in your library");
            }
            guid = Guid.Parse(matches[0].Id);
        }

        try
        {
            return await GetApi<DetailData>($"{meta.ApiPath}/{guid.ToString()}");
        }
        catch (Exception e)
        {
            throw new Exception($"Could not find asset with Id {guid} ({e.Message})");
        }
    }

    public async Task<ApiModelType> GetApi<ApiModelType>(string routeAndParams) 
    {
        HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, new Uri(InstanceURL, routeAndParams));
        message.Headers.Add("SimId", SimId);
        message.Headers.Add("Accept", "application/json");

        Console.WriteLine($"[CONN] GET {routeAndParams}");
        var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, requestTokenSource.Token).ConfigureAwait(false);
        Console.WriteLine($"[CONN] HTTP {(int)response.StatusCode} {response.StatusCode}");

        if (!response.IsSuccessStatusCode)
        {
            throw new NoSuccessException(response.StatusCode.ToString());
        }
        using (var stream = await response.Content.ReadAsStreamAsync())
        {
            using (var reader = new StreamReader(stream))
            {
                var jsonString = await reader.ReadToEndAsync();
                return Newtonsoft.Json.JsonConvert.DeserializeObject<ApiModelType>(jsonString);
            }
        }
    }

    public async Task SendAnalysis<DataType>(string testId, DataType result)
    {
        await PostApi<DataType>($"/api/v1/test-results/{testId}", result);
    }

    public async Task UpdateStatus(string status, string simGuid, string message)
    {
        Console.WriteLine($"[CONN] Updated simulation {simGuid} with status:{status} msg:{message}");

        var statusMessage = new StatusMessage
        {
            message = message,
            status = status,
        };
        await PostApi<StatusMessage>($"/api/v1/simulations/{simGuid}/status", statusMessage);
    }

    public async Task PostApi<ApiData>(string route, ApiData data)
    {
        HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, new Uri(InstanceURL, route));
        message.Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
        message.Headers.Add("SimId", Config.SimID);
        message.Headers.Add("Accept", "application/json");

        Console.WriteLine($"[CONN] POST {route}");

        var response = await client.SendAsync(message);

        Console.WriteLine($"[CONN] HTTP {(int)response.StatusCode} {response.StatusCode}");

        if (response.IsSuccessStatusCode)
        {
            Debug.Log("Receiving response of " + response.StatusCode + " " + response.Content);
        }
    }

    public void Disconnect()
    {
        Console.WriteLine("[CONN] Disconnecting");
        onlineStream?.Close();
        onlineStream = null;
        requestTokenSource?.Cancel();
        requestTokenSource?.Dispose();
        requestTokenSource = new CancellationTokenSource();
        client?.CancelPendingRequests();
        client?.Dispose();
        client = new HttpClient();
    }

    public static SimulatorInfo GetInfo()
    {
        List<string> ips = new List<string>();
        var os = Environment.OSVersion;
        NetworkInterface[] intf = NetworkInterface.GetAllNetworkInterfaces();
        foreach (NetworkInterface device in intf)
        {
            foreach (UnicastIPAddressInformation info in device.GetIPProperties().UnicastAddresses)
            {
                string address = info.Address.ToString();
                if (address.Contains(":")) continue;
                if (address.StartsWith("127.")) continue;
                ips.Add(address);
            }
        }
        return new SimulatorInfo()
        {
            linkToken = Guid.NewGuid().ToString(),
            hostName = Environment.MachineName,
            platform = os.ToString(),
            version = "2020.05",
            ip = ips,
            macAddress = "00:00:00:00:00:00"
        };
    }
}

public struct SimulatorInfo
{
    public string linkToken;
    public string hostName;
    public string platform;
    public string version;
    public List<string> ip;
    public string macAddress;
}

public struct ResultMessage
{
    public bool success;
    public int resultCount;
    public string results;
}

public struct StatusMessage
{
    public string status;
    public string message;
}
