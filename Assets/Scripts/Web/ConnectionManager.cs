using Newtonsoft.Json.Linq;
using Simulator;
using Simulator.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
    public SimulatorInfo simInfo;
    public float connectionStartTime;
    HttpClient client;
    HttpResponseMessage response;
    Task task;
    CancellationTokenSource requestTokenSource;
    CancellationTokenSource onlineTokenSource;
    static int unityThread;
    static Queue<Action> runInUpdate = new Queue<Action>();
    static int[] timeOutSequence = new[]{1, 5, 15, 30};
    int timeoutAttempts;

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
    }

    private void OnDestroy()
    {
        if(instance == this && Status != ConnectionStatus.Offline)
        Disconnect();
    }

    async Task Connect()
    {
        try
        {
            client = new HttpClient();

            simInfo = GetInfo();
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(simInfo);
            Status = ConnectionStatus.Connecting;
            RunOnUnityThread(() =>
            {
                ConnectionUI.instance.UpdateStatus();
                ConnectionUI.instance.statusButton.interactable = false;
                connectionStartTime = Time.time;
            });

            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, Config.CloudUrl + "/api/v1/clusters/connect");
            message.Content = new StringContent(json, Encoding.UTF8, "application/json");
            message.Headers.Add("SimID", Config.SimID);
            message.Headers.Add("Accept", "application/json");
            message.Headers.Add("Connection", "Keep-Alive");
            message.Headers.Add("X-Accel-Buffering", "no");
            response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, requestTokenSource.Token).ConfigureAwait(false);
            await ReadResponse(response.Content.ReadAsStreamAsync().Result);

            if (!response.IsSuccessStatusCode)
            {
                Status = ConnectionStatus.Offline;
                RunOnUnityThread(() =>
                {
                    ConnectionUI.instance.UpdateStatus();
                });

                throw new Exception(response.StatusCode.ToString());
            }
            else
            {
                await Task.Delay(1000 * timeOutSequence[timeOutSequence.Length - 1 > timeoutAttempts ? timeoutAttempts : timeOutSequence.Length -1]);
                timeoutAttempts = timeOutSequence.Length - 1 > timeoutAttempts ? timeoutAttempts++ : timeOutSequence.Length - 1;
                await Connect();
            }
        }
        catch (TaskCanceledException ex)
        {
            Debug.Log("Linking task canceled.");
            Debug.LogException(ex);
            Disconnect();
        }
        catch (Exception ex)
        {
            Debug.Log(Config.CloudUrl);
            Debug.LogException(ex);
            Disconnect();
        }
    }

    async Task ReadResponse(Stream stream)
    {
        await Task.Run(() =>
        {
            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    //We are ready to read the stream
                    Parse(reader.ReadLine());
                }
            }
        });
    }

    public void Disconnect()
    {
        try
        {
            requestTokenSource.Cancel();
            onlineTokenSource.Cancel();
            client.CancelPendingRequests();
            client.Dispose();
            RunOnUnityThread(() =>
            {
                Status = ConnectionStatus.Offline;
                ConnectionUI.instance?.UpdateStatus();
            });
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
            if (s.StartsWith("data:") && !string.IsNullOrEmpty(s.Substring(5)))
            {
                JObject deserialized = JObject.Parse(s.Substring(5));
                if (deserialized != null && deserialized.HasValues)
                {
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
            }
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
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
            StatusMessage messageBody = new StatusMessage();
            messageBody.status = status;
            messageBody.message = "Is this really necessary?";
            Debug.Log("Posting status of " + status + " to " + Config.CloudUrl + "/api/v1/simulations/" + simGuid + "/status");
            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, Config.CloudUrl + "/api/v1/simulations/" + simGuid + "/status");
            message.Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(messageBody), Encoding.UTF8, "application/json");
            message.Headers.Add("simid", Config.SimID);
            message.Headers.Add("Accept", "application/json");
            var response = await client.SendAsync(message);
            if (response.IsSuccessStatusCode)
            {
                Debug.Log("Receiving response of " + response.StatusCode + " " + response.Content);
            }
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
        requestTokenSource = new CancellationTokenSource();
        onlineTokenSource = new CancellationTokenSource();
        task = Connect();
        await task;
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
                break;
            case ConnectionStatus.Connecting:
            case ConnectionStatus.Connected:
            case ConnectionStatus.Online:
                Disconnect();
                break;
            default:
                break;
        }
    }

    public async Task<MapDetailData> GetMapDetailData(string mapId)
    {
        HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, Config.CloudUrl + "/api/v1/maps/" + mapId);
        message.Headers.Add("SimID", Config.SimID);
        message.Headers.Add("Accept", "application/json");
        response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, requestTokenSource.Token).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(response.StatusCode.ToString());
        }
        using (var stream = await response.Content.ReadAsStreamAsync())
        using (var reader = new StreamReader(stream))
        {
            var data = await reader.ReadToEndAsync();
            return Newtonsoft.Json.JsonConvert.DeserializeObject<MapDetailData>(data);
        }
    }

    public async Task<VehicleDetailData> GetVehicleDetailData(string vehicleId)
    {
        HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, Config.CloudUrl + "/api/v1/vehicles/" + vehicleId);
        message.Headers.Add("SimID", Config.SimID);
        message.Headers.Add("Accept", "application/json");
        response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, requestTokenSource.Token).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(response.StatusCode.ToString());
        }
        using (var stream = await response.Content.ReadAsStreamAsync())
        using (var reader = new StreamReader(stream))
        {
            var data = await reader.ReadToEndAsync();
            return Newtonsoft.Json.JsonConvert.DeserializeObject<VehicleDetailData>(data);
        }
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
