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
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using Simulator.Utilities;
using System.Text.RegularExpressions;
using static Simulator.BundleConfig;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ConnectionManager : MonoBehaviour
{
    public enum ConnectionStatus
    {
        Offline,
        Connecting,
        Connected,
        Online
    }

    static ConnectionStatus _status = ConnectionStatus.Offline;
    public static ConnectionStatus Status
    {
        get => _status;
        private set
        {
            _status = value;
            OnStatusChanged(value, ConnectionMessage);
        }
    }
    public static string ConnectionMessage
    {
        get => _disconnectReason;
        private set
        {
            Debug.Log(value);
            _disconnectReason = value;
            OnStatusChanged(_status, value);
        }
    }

    public static string _disconnectReason = null;

    public static ConnectionManager instance;
    public static CloudAPI API;
    public SimulatorInfo simInfo;
    static int[] timeOutSequence = new[] { 1, 5, 15, 30 };
    ClientSettingsService service;

    public static Action<ConnectionStatus, string> OnStatusChanged = delegate { };

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
        service = new ClientSettingsService();
        ClientSettings settings = service.GetOrMake();

        if (string.IsNullOrEmpty(Config.CloudProxy))
        {
            API = new CloudAPI(new Uri(Config.CloudUrl), Config.SimID);
        }
        else
        {
            API = new CloudAPI(new Uri(Config.CloudUrl), Config.SimID, new Uri(Config.CloudProxy));
        }

#if UNITY_EDITOR
        EditorApplication.playModeStateChanged += HandlePlayMode;
#endif

        if (settings.onlineStatus)
        {
            ConnectionStatusEvent();
        }
    }

#if UNITY_EDITOR
    private static void HandlePlayMode(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            Debug.Log("Disconnecting before leaving playmode");
            Status = ConnectionStatus.Offline;
            API.Disconnect();
        }
    }
#endif

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

            int timeOutIndex = 0;
            while (true)
            {
                int timeOut = timeOutSequence[timeOutIndex];

                try
                {
                    ConnectionMessage = "Connecting to " + Config.CloudUrl;
                    var stream = await API.Connect(simInfo);
                    await ReadResponseLoop(stream);
                    // WISE closed our connection without any network error
                    // as of now, maintenance mode also causes this
                    break;
                }
                catch (HttpRequestException ex)
                {
                    var message = ex.InnerException?.Message ?? ex.Message;
                    // temporary network issue, we'll retry
                    for (int i = 0; i < timeOut; i++)
                    {
                        ConnectionMessage = $"{message}, reconnecting after {timeOut - i} seconds";
                        await Task.Delay(1000);
                    }
                }
                finally
                {
                    API.Disconnect();
                }

                if (!Config.RetryForever && Status == ConnectionStatus.Offline)
                {
                    ConnectionMessage = "Disconnected.";
                    break;
                }

                if (timeOutIndex < timeOutSequence.Length - 1)
                {
                    timeOutIndex++;
                }
                else if (!Config.RetryForever)
                {
                    ConnectionMessage = $"Failed to connect to cloud after {timeOutSequence.Length + 1} attempts, giving up.";
                    break;
                }
            }
        }
        catch (CloudAPI.NoSuccessException ex)
        {
            // testcase: cloud_url = https://google.com
            // testcase: wise in maintenance mode
            ConnectionMessage = ex.StatusCode switch
            {
                HttpStatusCode.NotFound => "Cannot find cloud (404), giving up.",
                HttpStatusCode.ServiceUnavailable => "Cloud unavailable, giving up.",
                // maintenance mode issues 302 redirect on POST
                HttpStatusCode.Redirect => "Cloud unavailable (302), giving up.",
                _ => $"{(int)ex.StatusCode}: {ex.Message}, giving up."
            };
        }
        catch (TaskCanceledException)
        {
            if (Status == ConnectionStatus.Connecting)
            {
                // TaskCancelledException occurs when a connection times out after 100 sec (default HttpClient.Timeout timespan)
                // FIXME: should this case be part of the the retry loop?
                // testcase: cloud_url=https://1.2.3.4
                ConnectionMessage = "Connection to cloud timed out, giving up";
            }
            else
            {
                // but also when user cancels the connection during tcp connection phase
                // testcase: cloud_url=https://1.2.3.4 then select online from dropdown within 100s
                ConnectionMessage = "Connection cancelled.";
            }
        }
        catch (System.Net.Sockets.SocketException se)
        {
            // FIXME: should this case be part of the the retry loop?
            ConnectionMessage = $"Could not reach cloud at {Config.CloudUrl}: {se.Message}, giving up";
        }
        catch (Exception ex)
        {
            ConnectionMessage = $"Connection error: {ex.Message}";
            Debug.LogException(ex);
        }

        Disconnect();
    }

    async Task ReadResponseLoop(Stream stream)
    {
        using (var reader = new StreamReader(stream))
        {
            try
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    await Parse(line);
                }
            }
            catch (WebException) when (Status == ConnectionStatus.Offline)
            {
                // when disconnecting, it is expected the task is cancelled.
            }
        }

        Debug.Log("WISE connection closed");
    }

    public void Disconnect()
    {
        try
        {
            API.Disconnect();
            Status = ConnectionStatus.Offline;
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
                                Status = ConnectionStatus.Connected;
                                break;
                            case "OK":
                                Status = ConnectionStatus.Online;
                                break;
                            case "Config":
                                {
                                    Status = ConnectionStatus.Online;

                                    SimulationData simData;
                                    try
                                    {
                                        simData = deserialized["data"].ToObject<SimulationData>();
                                        SimulationConfigUtils.ProcessKnownTemplates(ref simData);
                                    }
                                    catch (Exception e) when (e is InvalidCastException || e is NullReferenceException)
                                    {
                                        Debug.LogError($"[CONN] Failed to parse Config data: '{s}'");
                                        Debug.LogException(e);
                                        throw;
                                    }
                                    Loader.Instance.StartSimulation(simData);
                                }
                                break;
                            case "Disconnect":
                                ConnectionMessage = "Disconnected: " + deserialized.GetValue("reason")?.ToString() ?? "unknown reason";
                                Disconnect();
                                break;
                            case "Timeout":
                                ConnectionMessage = "Disconnected: " + deserialized.GetValue("reason")?.ToString() ?? "unknown reason";
                                Disconnect();
                                break;
                            case "Stop":
                                {
                                    if (Loader.Instance.Status == SimulatorStatus.Idle || Loader.Instance.Status == SimulatorStatus.Stopping)
                                    {
                                        SimulationData simData = Newtonsoft.Json.JsonConvert.DeserializeObject<SimulationData>(deserialized.GetValue("data").ToString());
                                        await API.UpdateStatus("Stopping", simData.Id, "stop requested but was not running simulation");
                                        await API.UpdateStatus("Idle", simData.Id, "");
                                        return;
                                    }
                                    Loader.Instance.StopAsync();
                                    break;
                                }
                            default:
                                Debug.LogWarning($"Unknown Status '{status.ToString()}'! Disconnecting.");
                                Disconnect();
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
    HttpClient client;
    HttpClientHandler handler;
    CancellationTokenSource requestTokenSource;

    Uri CloudURL;
    Uri ProxyURL;
    string SimId;

    private const uint fetchLimit = 50;
    Stream onlineStream;

    // TODO: rename this property to something more appropritate
    public string CloudType { get => CloudURL.AbsoluteUri; }

    public CloudAPI(Uri cloudURL, CookieContainer cookieContainer, Uri proxyURL = null)
    {
        CloudURL = cloudURL;
        ProxyURL = proxyURL;
        SimId = null;
        handler = new HttpClientHandler();
        handler.CookieContainer = cookieContainer;
        handler.AllowAutoRedirect = false;
        handler.UseCookies = true;
        if (proxyURL != null)
        {
            WebProxy webProxy = new WebProxy(new Uri(Config.CloudProxy));
            handler.Proxy = webProxy;
            Console.WriteLine("[CONN] Cloud URL {0}, cookie auth, via proxy {1}", CloudURL.AbsoluteUri, ProxyURL.AbsolutePath);
        }
        client = new HttpClient(handler);
        requestTokenSource = new CancellationTokenSource();

        Console.WriteLine("[CONN] Cloud URL {0}, cookie auth", CloudURL.AbsoluteUri);
    }

    public CloudAPI(Uri cloudURL, string simId, Uri proxyURL = null)
    {
        CloudURL = cloudURL;
        ProxyURL = proxyURL;
        SimId = simId;

        HttpClientHandler handler = new HttpClientHandler();
        handler.AllowAutoRedirect = false;
        if (proxyURL != null)
        {
            WebProxy webProxy = new WebProxy(new Uri(Config.CloudProxy));
            handler.Proxy = webProxy;
            Console.WriteLine("[CONN] Cloud URL {0} via proxy {1}", CloudURL.AbsoluteUri, ProxyURL.AbsolutePath);
        }
        else
        {
            Console.WriteLine("[CONN] Cloud URL {0}", CloudURL.AbsoluteUri);
        }
        client = new HttpClient(handler);
        requestTokenSource = new CancellationTokenSource();

    }

    public async Task<bool> Login(string email, string password)
    {
        var login = new
        {
            email,
            password
        };

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(login, JsonSettings.camelCase);
        HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, new Uri(CloudURL, "/api/v1/auth/login"));
        message.Content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.SendAsync(message, HttpCompletionOption.ResponseContentRead, requestTokenSource.Token);
        // work around for bug where cookies do not happen to be set by the client
        if (response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string> cookies))
        {
            handler.CookieContainer.SetCookies(CloudURL, string.Join(",", cookies));
        }

        return response.IsSuccessStatusCode;
    }

    public class NoSuccessException : Exception
    {
        public NoSuccessException(string status, HttpStatusCode statusCode) : base(status)
        {
            StatusCode = statusCode;
        }
        public readonly HttpStatusCode StatusCode;
    }

    public async Task<Stream> Connect(SimulatorInfo simInfo)
    {
        if (onlineStream != null)
        {
            onlineStream.Close();
            onlineStream.Dispose();
            onlineStream = null;
        }

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(simInfo, JsonSettings.camelCase);
        HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, new Uri(CloudURL, "/api/v1/clusters/connect"));
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
            throw new NoSuccessException($"{content} ({(int)response.StatusCode})", response.StatusCode);
        }
        Console.WriteLine("[CONN] Connected to WISE.");
        Debug.Log("connected");
        onlineStream = await response.Content.ReadAsStreamAsync();
        return onlineStream;
    }

    // use this if you waqnt to make sure the connection succeeded but don't care about
    // messages before the connection ok message
    public async Task<StreamReader> EnsureConnectSuccess()
    {
        string line;
        var reader = new StreamReader(onlineStream);

        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (line.StartsWith("data:") && !string.IsNullOrEmpty(line.Substring(6)))
            {
                JObject deserialized = JObject.Parse(line.Substring(5));
                if (deserialized != null && deserialized.HasValues)
                {
                    switch (deserialized.GetValue("status").ToString())
                    {
                        case "OK": return reader;
                        case "Unrecognized": throw new Exception("Simulator is not linked. Enter play mode to re-link.");
                    }
                }
            }
        }

        onlineStream.Close();
        throw new Exception("Connection Closed");
    }

    public Task<DetailData> Get<DetailData>(string cloudId) where DetailData : CloudAssetDetails
    {
        var meta = (CloudData)Attribute.GetCustomAttribute(typeof(DetailData), typeof(CloudData));
        return GetApi<DetailData>($"{meta.ApiPath}/{cloudId}");
    }

    public async Task<LibraryList<DetailData>> GetLibraryPage<DetailData>(uint offset, uint limit = fetchLimit) where DetailData : CloudAssetDetails
    {
        var meta = (CloudData)Attribute.GetCustomAttribute(typeof(DetailData), typeof(CloudData));
        return await GetApi<LibraryList<DetailData>>($"{meta.ApiPath}?display=sim&limit={limit}&offset={offset}");
    }

    public async Task<DetailData[]> GetLibrary<DetailData>() where DetailData : CloudAssetDetails
    {
        List<DetailData> result = new List<DetailData>();
        LibraryList<DetailData> data;
        do
        {
            data = await GetLibraryPage<DetailData>((uint)result.Count);
            result.AddRange(data.Rows);
        }
        while (result.Count < data.Count && data.Count > 0);

        return result.ToArray();
    }

    public async Task<DetailData> GetByIdOrName<DetailData>(string cloudIdOrName) where DetailData : CloudAssetDetails
    {
        var meta = (CloudData)Attribute.GetCustomAttribute(typeof(DetailData), typeof(CloudData));
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
        HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, new Uri(CloudURL, routeAndParams));
        if (!string.IsNullOrEmpty(SimId)) message.Headers.Add("SimId", SimId);
        message.Headers.Add("Accept", "application/json");

        Console.WriteLine($"[CONN] GET {routeAndParams}");
        var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, requestTokenSource.Token).ConfigureAwait(false);
        Console.WriteLine($"[CONN] HTTP {(int)response.StatusCode} {response.StatusCode}");

        if (!response.IsSuccessStatusCode)
        {
            throw new NoSuccessException(response.StatusCode.ToString(), response.StatusCode);
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
        HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, new Uri(CloudURL, route));
        message.Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(data, JsonSettings.camelCase), Encoding.UTF8, "application/json");
        if (!string.IsNullOrEmpty(SimId)) message.Headers.Add("SimId", Config.SimID);
        message.Headers.Add("Accept", "application/json");

        Console.WriteLine($"[CONN] POST {route}");

        var response = await client.SendAsync(message);

        Console.WriteLine($"[CONN] HTTP {(int)response.StatusCode} {response.StatusCode}");

        if (!response.IsSuccessStatusCode)
        {
            Debug.Log("Receiving response of " + response.StatusCode + " " + response.Content);
        }
    }

    public void Disconnect()
    {
        Console.WriteLine("[CONN] Disconnecting");
        onlineStream?.Close();
        onlineStream?.Dispose();
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
            if (device.OperationalStatus != OperationalStatus.Up)
                continue;

            foreach (UnicastIPAddressInformation info in device.GetIPProperties().UnicastAddresses)
            {
                string address = info.Address.ToString();
                if (address.Contains(":")) continue;
                if (address.StartsWith("127.")) continue;
                ips.Add(address);
            }
        }

        var buildVersion = Application.version;
#if UNITY_EDITOR
        var DevSettings = (Simulator.Editor.DevelopmentSettingsAsset)AssetDatabase.LoadAssetAtPath("Assets/Resources/Editor/DeveloperSettings.asset", typeof
        (Simulator.Editor.DevelopmentSettingsAsset));
        if (DevSettings != null && !string.IsNullOrWhiteSpace(DevSettings.VersionOverride))
        {
            buildVersion = DevSettings.VersionOverride;
        }
#endif
        var buildInfo = Resources.Load<BuildInfo>("BuildInfo");
        if (buildInfo != null && !string.IsNullOrWhiteSpace(buildInfo.Version))
        {
            buildVersion = buildInfo.Version;
        }

        var macadds = new List<string>();
        var regex = String.Concat(Enumerable.Repeat("([a-fA-F0-9]{2})", 6));
        var replace = "$1:$2:$3:$4:$5:$6";
        var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
        foreach (var adapter in networkInterfaces)
        {
            if (adapter.NetworkInterfaceType != NetworkInterfaceType.Ethernet)
                continue;

            var address = adapter.GetPhysicalAddress();
            if (address.ToString() != "")
            {
                macadds.Add(Regex.Replace(address.ToString(), regex, replace));
            }
        }

        return new SimulatorInfo()
        {
            linkToken = Guid.NewGuid().ToString(),
            hostName = Environment.MachineName,
            platform = os.ToString(),
            version = buildVersion,
            ip = ips,
            macAddress = macadds[0] ?? "00:00:00:00:00:00", // TODO need to remove and accept array instead
            macAddresses = macadds,
            bundleVersions = BundleConfig.Versions,
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
    public List<string> macAddresses;
    public Dictionary<BundleTypes, string> bundleVersions;
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
