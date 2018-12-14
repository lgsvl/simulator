using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnalyticsManager : MonoBehaviour
{
    #region Singleton
    private static AnalyticsManager _instance = null;
    public static AnalyticsManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<AnalyticsManager>();
                if (_instance == null)
                    Debug.LogError("<color=red>AnalyticsManager Not Found!</color>");
            }
            return _instance;
        }
    }
    #endregion

    #region vars
    // TODO
    private bool sendLaunchEvent = true;

    public bool isTesting = false;
    public GoogleAnalyticsV4.DebugMode logLevel = GoogleAnalyticsV4.DebugMode.VERBOSE;

    private string trackingCode = "UA-130546445-3";
    private string productName = "LGSVL Simulator";
    private string bundleVersion = "1.0.0";
    private string bundleIdentifier = "com.lgsvlsimulator.simulator"; // ??? PC Linux not needed
    private bool anonymizeIP = false;
    private int sessionTimeout = 1800;
    private bool UncaughtExceptionReporting = false;
    private string uncaughtExceptionStackTrace = null;
    private bool initialized = false;

    private GoogleAnalyticsMPV3 mpTracker = new GoogleAnalyticsMPV3();

    private bool isMapActive = false;
    private float mapTimer = 0f;
    
    //public readonly static string currencySymbol = "USD";
    //public readonly static string EVENT_HIT = "createEvent";
    //public readonly static string APP_VIEW = "createAppView";
    //public readonly static string SET = "set";
    //public readonly static string SET_ALL = "setAll";
    //public readonly static string SEND = "send";
    //public readonly static string ITEM_HIT = "createItem";
    //public readonly static string TRANSACTION_HIT = "createTransaction";
    //public readonly static string SOCIAL_HIT = "createSocial";
    //public readonly static string TIMING_HIT = "createTiming";
    //public readonly static string EXCEPTION_HIT = "createException";
    #endregion

    #region mono
    private void Awake()
    {
        if (_instance == null)
            _instance = this;

        if (_instance != this)
        {
            DestroyImmediate(gameObject);
        }
        else
        {
            DontDestroyOnLoad(gameObject);
        }

        InitializeTracker();
    }

    private void Start()
    {
        StartSession();
        LaunchEvent();
    }

    private void Update()
    {
        if (isMapActive)
            mapTimer += Time.deltaTime;

        if (!string.IsNullOrEmpty(uncaughtExceptionStackTrace))
        {
            LogException(uncaughtExceptionStackTrace, true);
            uncaughtExceptionStackTrace = null;
        }
    }

    private void OnDestroy()
    {
        if (UncaughtExceptionReporting)
            Application.logMessageReceived -= HandleException;
    }

    private void OnApplicationQuit()
    {
        ExitEvent();
        StopSession();
        _instance = null;
        DestroyImmediate(gameObject);
    }
    #endregion

    #region init
    private void InitializeTracker()
    {
        if (!initialized)
        {
            if (string.IsNullOrEmpty(productName))
                productName = Application.productName;

            if (string.IsNullOrEmpty(bundleIdentifier))
                bundleIdentifier = Application.identifier;

            if (string.IsNullOrEmpty(bundleVersion))
                bundleVersion = Application.version;

            mpTracker.SetTrackingCode(trackingCode);
            mpTracker.SetBundleIdentifier(bundleIdentifier);
            mpTracker.SetAppName(productName);
            mpTracker.SetAppVersion(bundleVersion);
            mpTracker.SetLogLevelValue(logLevel);
            mpTracker.SetAnonymizeIP(anonymizeIP);
            mpTracker.SetDryRun(isTesting);
            mpTracker.InitializeTracker();

            //Debug.Log("Initializing Google Analytics 0.2.");
            if (UncaughtExceptionReporting && GoogleAnalyticsV4.belowThreshold(logLevel, GoogleAnalyticsV4.DebugMode.VERBOSE))
                Debug.Log("Enabling uncaught exception reporting.");

            initialized = true;
            //SetOnTracker(Fields.DEVELOPER_ID, "GbOCSs"); // ??? What does this string mean?

            if (UncaughtExceptionReporting)
                Application.logMessageReceived += HandleException;
        }
    }

    // Use values from Fields for the fieldName parameter ie. Fields.SCREEN_NAME
    public void SetOnTracker(Field fieldName, object value)
    {
        mpTracker.SetTrackerVal(fieldName, value);
    }

    public void SetAppLevelOptOut(bool optOut)
    {
        mpTracker.SetOptOut(optOut);
    }

    public void SetUserIDOverride(string userID)
    {
        SetOnTracker(Fields.USER_ID, userID);
    }

    public void ClearUserIDOverride()
    {
        mpTracker.ClearUserIDOverride();
    }

    public void DispatchHits()
    {
        // Standalone not supported
    }

    public void StartSession()
    {
        mpTracker.StartSession();
    }

    public void StopSession()
    {
        mpTracker.StopSession();
    }
    #endregion

    #region logs
    public void LogScreen(string title)
    {
        LogScreen(new AppViewHitBuilder().SetScreenName(title));
    }

    public void LogScreen(AppViewHitBuilder builder)
    {
        if (builder.Validate() == null) return;
        if (GoogleAnalyticsV4.belowThreshold(logLevel, GoogleAnalyticsV4.DebugMode.VERBOSE)) Debug.Log("Logging screen.");
        mpTracker.LogScreen(builder);
    }

    public void LogEvent(string eventCategory, string eventAction, string eventLabel = null, long value = -1)
    {
        EventHitBuilder builder = new EventHitBuilder();
        if (eventLabel == null)
        {
            builder.SetEventCategory(eventCategory);
            builder.SetEventAction(eventAction);
        }
        else if (value == -1)
        {
            builder.SetEventCategory(eventCategory);
            builder.SetEventAction(eventAction);
            builder.SetEventLabel(eventLabel);
        }
        else
        {
            builder.SetEventCategory(eventCategory);
            builder.SetEventAction(eventAction);
            builder.SetEventLabel(eventLabel);
            builder.SetEventValue(value);
        }
        LogEvent(builder);
    }

    public void LogEvent(EventHitBuilder builder)
    {
        if (builder.Validate() == null) return;
        if (GoogleAnalyticsV4.belowThreshold(logLevel, GoogleAnalyticsV4.DebugMode.VERBOSE)) Debug.Log("Logging event.");
        mpTracker.LogEvent(builder);
    }

    public void LogTransaction(string transID, string affiliation, double revenue, double tax, double shipping)
    {
        LogTransaction(transID, affiliation, revenue, tax, shipping, "");
    }

    public void LogTransaction(string transID, string affiliation, double revenue, double tax, double shipping, string currencyCode)
    {
        TransactionHitBuilder builder = new TransactionHitBuilder()
            .SetTransactionID(transID)
            .SetAffiliation(affiliation)
            .SetRevenue(revenue)
            .SetTax(tax)
            .SetShipping(shipping)
            .SetCurrencyCode(currencyCode);

        LogTransaction(builder);
    }

    public void LogTransaction(TransactionHitBuilder builder)
    {
        if (builder.Validate() == null) return;
        if (GoogleAnalyticsV4.belowThreshold(logLevel, GoogleAnalyticsV4.DebugMode.VERBOSE)) Debug.Log("Logging transaction.");
        mpTracker.LogTransaction(builder);
    }

    public void LogItem(string transID, string name, string sku, string category, double price, long quantity)
    {
        LogItem(transID, name, sku, category, price, quantity, null);
    }

    public void LogItem(string transID, string name, string sku, string category, double price, long quantity, string currencyCode)
    {
        ItemHitBuilder builder = new ItemHitBuilder()
            .SetTransactionID(transID)
            .SetName(name)
            .SetSKU(sku)
            .SetCategory(category)
            .SetPrice(price)
            .SetQuantity(quantity)
            .SetCurrencyCode(currencyCode);

        LogItem(builder);
    }

    public void LogItem(ItemHitBuilder builder)
    {
        if (builder.Validate() == null) return;
        if (GoogleAnalyticsV4.belowThreshold(logLevel, GoogleAnalyticsV4.DebugMode.VERBOSE)) Debug.Log("Logging item.");
        mpTracker.LogItem(builder);
    }

    public void LogSocial(string socialNetwork, string socialAction, string socialTarget)
    {
        SocialHitBuilder builder = new SocialHitBuilder()
            .SetSocialNetwork(socialNetwork)
            .SetSocialAction(socialAction)
            .SetSocialTarget(socialTarget);

        LogSocial(builder);
    }

    public void LogSocial(SocialHitBuilder builder)
    {
        if (builder.Validate() == null) return;
        if (GoogleAnalyticsV4.belowThreshold(logLevel, GoogleAnalyticsV4.DebugMode.VERBOSE)) Debug.Log("Logging social.");
        mpTracker.LogSocial(builder);
    }

    public void LogTiming(string timingCategory, long timingInterval, string timingName, string timingLabel)
    {
        TimingHitBuilder builder = new TimingHitBuilder()
            .SetTimingCategory(timingCategory)
            .SetTimingInterval(timingInterval)
            .SetTimingName(timingName)
            .SetTimingLabel(timingLabel);

        LogTiming(builder);
    }

    public void LogTiming(TimingHitBuilder builder)
    {
        if (builder.Validate() == null) return;
        if (GoogleAnalyticsV4.belowThreshold(logLevel, GoogleAnalyticsV4.DebugMode.VERBOSE)) Debug.Log("Logging timing.");
        mpTracker.LogTiming(builder);
    }

    public void Dispose()
    {
        initialized = false;
        // no mpTracker.Dispose()?
    }

    public void LogException(string exceptionDescription, bool isFatal)
    {
        LogException(new ExceptionHitBuilder().SetExceptionDescription(exceptionDescription).SetFatal(isFatal));
    }

    public void LogException(ExceptionHitBuilder builder)
    {
        if (builder.Validate() == null) return;
        if (GoogleAnalyticsV4.belowThreshold(logLevel, GoogleAnalyticsV4.DebugMode.VERBOSE)) Debug.Log("Logging exception.");
        mpTracker.LogException(builder);
    }
    #endregion

    #region exceptions
    private void HandleException(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Exception) uncaughtExceptionStackTrace = $"{condition}\n{stackTrace}{StackTraceUtility.ExtractStackTrace()}";
    }
    #endregion

    #region custom events
    public void LaunchEvent()
    {
        LogEvent("LGSVLSimulator", "Launch");
    }

    public void ExitEvent()
    {
        LogEvent("LGSVLSimulator", "Exit");
    }

    public void MenuButtonEvent(string name)
    {
        LogEvent("LGSVLSimulator", "MenuButton", name);
    }

    public void MapStartEvent(string name)
    {
        LogEvent("LGSVLSimulator", "MapStart", name);
        isMapActive = true;
    }

    public void EgoStartEvent(string name)
    {
        LogEvent("LGSVLSimulator", "EgoStart", name);
    }

    public void MapExitEvent(string name)
    {
        LogEvent("LGSVLSimulator", "MapExit", name, Mathf.RoundToInt(mapTimer % 60));
        isMapActive = false;
        mapTimer = 0f;
    }

    public void TotalMileageEvent(int total)
    {
        LogEvent("LGSVLSimulator", "MapExit", "TotalMileage", total);
    }

    public void MileTickEvent()
    {
        LogEvent("LGSVLSimulator", "MileTick", "Mileage", 1);
    }
    #endregion
}


