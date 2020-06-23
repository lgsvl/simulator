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
        Config.SimID = settings.simid;
        API = new CloudAPI(new Uri(Config.CloudUrl), settings.simid);

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
            simInfo = GetInfo();
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
                    var stream = await API.Connect(simInfo);
                    await ReadResponse(stream);
                    break;
                }
                catch(CloudAPI.NoSuccessException)
                {
                    await Task.Delay(1000 * timeOut);
                }
            }
        }
        catch (TaskCanceledException)
        {
            Debug.Log("Linking task canceled.");
            Disconnect();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Disconnect();
        }
    }

    async Task ReadResponse(Stream stream)
    {
        await Task.Run(() =>
        {
            if (stream != null)
            {
                using (var reader = new StreamReader(stream))
                {
                    while (!reader.EndOfStream)
                    {
                        //We are ready to read the stream
                        Parse(reader.ReadLine());
                    }
                }
            }
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
        catch(ObjectDisposedException ex)
        {

        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }
    }

    void Parse(string s)
    {
        //Debug.Log(s);
        if (string.IsNullOrEmpty(s)) return;
        try
        {
            if (s.StartsWith("data:") && !string.IsNullOrEmpty(s.Substring(6)))
            {
                JObject deserialized = JObject.Parse(s.Substring(5));
                if (deserialized != null && deserialized.HasValues)
                {
                    if (deserialized.GetValue("status") != null) {
                        switch (deserialized.GetValue("status").ToString())
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
                                    SimulationData simData = Newtonsoft.Json.JsonConvert.DeserializeObject<SimulationData>(deserialized.GetValue("data").ToString());
                                    Loader.StartSimulation(simData);
                                    instance.UpdateStatus("Starting", simData.Id);
                                });
                                break;
                            case "Disconnect":
                                RunOnUnityThread(() =>
                                {
                                    Disconnect();
                                });
                                break;
                            case "Stop":
                                Loader.StopAsync();
                                break;
                            default:
                                Debug.LogError("Unknown Status! Disconnecting.");
                                RunOnUnityThread(() =>
                                {
                                    Disconnect();
                                });
                                break;
                        }
                    }
                    /* //nothing to do for now.
                    if (deserialized.GetValue("ping") != null)
                    {
                        Debug.Log("got ping");
                    }
                    */
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
        // is this right?
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

    public async void UpdateStatus(string status, string simGuid)
    {
        try
        {
            await API.UpdateStatus(status, simGuid);
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
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
                    action = runInUpdate.Dequeue();
            }
            action?.Invoke();
        }
    }

    async void RunConnectTask()
    {
        await Connect();
    }

    public SimulatorInfo GetInfo()
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

    public CloudAPI(Uri instanceURL, string simId)
    {
        InstanceURL = instanceURL;
        SimId = simId;
    }

    public class NoSuccessException: Exception
    {
        public NoSuccessException(string status) :base(status) { }
    }

    public async Task<Stream> Connect(SimulatorInfo simInfo)
    {
        try
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(simInfo);
            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, new Uri(InstanceURL, "/api/v1/clusters/connect"));
            message.Content = new StringContent(json, Encoding.UTF8, "application/json");
            message.Headers.Add("SimID", Config.SimID);
            message.Headers.Add("Accept", "application/json");
            message.Headers.Add("Connection", "Keep-Alive");
            message.Headers.Add("X-Accel-Buffering", "no");
            var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, requestTokenSource.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new NoSuccessException(response.StatusCode.ToString());
            }
            return await response.Content.ReadAsStreamAsync();
        }
        catch
        {
            return await Task.FromResult<Stream>(null);
        }
    }

    public Task<DetailData> Get<DetailData>(string cloudId) where DetailData: CloudAssetDetails
    {
        var meta = (CloudData) Attribute.GetCustomAttribute(typeof(DetailData), typeof (CloudData));
        return GetApi<DetailData>($"{meta.ApiPath}/{cloudId}");
    }

    public async Task<DetailData[]> GetLibrary<DetailData>() where DetailData: CloudAssetDetails
    {
        var meta = (CloudData) Attribute.GetCustomAttribute(typeof(DetailData), typeof (CloudData));
        var result = await GetApi<LibraryList<DetailData>>($"{meta.ApiPath}?display=fav");
        return result.rows;
    }

    public async Task<DetailData> GetByIdOrName<DetailData>(string cloudIdOrName) where DetailData: CloudAssetDetails

    {
        var meta = (CloudData) Attribute.GetCustomAttribute(typeof(DetailData), typeof (CloudData));
        Guid guid;
        if (Guid.TryParse(cloudIdOrName, out guid)) 
        {
            try
            {   
                return await GetApi<DetailData>($"{meta.ApiPath}/{guid.ToString()}");
            }
            catch(Exception e)
            {
                throw new Exception($"Could not find asset with Id {guid} ({e.Message})");
            }
        }
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
        return matches[0];
    }

    public async Task<ApiModelType> GetApi<ApiModelType>(string routeAndParams) 
    {
        HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, new Uri(InstanceURL, routeAndParams));
        message.Headers.Add("SimID", SimId);
        message.Headers.Add("Accept", "application/json");
        var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, requestTokenSource.Token).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new NoSuccessException(response.StatusCode.ToString());
        }
        using (var stream = await response.Content.ReadAsStreamAsync())
        using (var reader = new StreamReader(stream))
        {
            var jsonString = await reader.ReadToEndAsync();
            return Newtonsoft.Json.JsonConvert.DeserializeObject<ApiModelType>(jsonString);
        }
    }

    public async Task UpdateStatus(string status, string simGuid)
    {
        var message = new StatusMessage
        {
            message = "",
            status = status,
        };
        await PostApi<StatusMessage>($"/api/v1/simulations/{simGuid}/status", message);
    }

    public async Task PostApi<ApiData>(string route, ApiData data)
    {
        HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, new Uri(InstanceURL, route));
        message.Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
        message.Headers.Add("simid", Config.SimID);
        message.Headers.Add("Accept", "application/json");
        var response = await client.SendAsync(message);
        if (response.IsSuccessStatusCode)
        {
            Debug.Log("Receiving response of " + response.StatusCode + " " + response.Content);
        }
    }

    public void Disconnect()
    {
        requestTokenSource?.Cancel();
        requestTokenSource?.Dispose();
        requestTokenSource = new CancellationTokenSource();
        client?.CancelPendingRequests();
        client?.Dispose();
        client = new HttpClient();
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

public struct StatusMessage
{
    public string status;
    public string message;
}
