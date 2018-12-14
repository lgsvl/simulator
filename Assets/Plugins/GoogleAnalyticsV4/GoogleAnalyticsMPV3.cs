/*
  Copyright 2014 Google Inc. All rights reserved.

  Licensed under the Apache License, Version 2.0 (the "License");
  you may not use this file except in compliance with the License.
  You may obtain a copy of the License at

      http://www.apache.org/licenses/LICENSE-2.0

  Unless required by applicable law or agreed to in writing, software
  distributed under the License is distributed on an "AS IS" BASIS,
  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  See the License for the specific language governing permissions and
  limitations under the License.
*/

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

/*
  GoogleAnalyticsMPV3 handles building hits using the Measurement Protocol.
  Developers should call the methods in GoogleAnalyticsV4, which will call the
  appropriate methods in this class if the application is built for platforms
  other than Android and iOS.
*/
public class GoogleAnalyticsMPV3 {
#if UNITY_ANDROID && !UNITY_EDITOR
#elif UNITY_IPHONE && !UNITY_EDITOR
#else
  private string trackingCode;
  private string bundleIdentifier;
  private string appName;
  private string appVersion;
  private GoogleAnalyticsV4.DebugMode logLevel;
  private bool anonymizeIP;
  private bool dryRun;
  private bool optOut;
  private int sessionTimeout;
  private string screenRes;
  private string clientId;
  private string url;
  private float timeStarted;
  private Dictionary<Field, object> trackerValues = new Dictionary<Field, object>();
  private bool startSessionOnNextHit = false;
  private bool endSessionOnNextHit = false;
  private bool trackingCodeSet = true;

  public void InitializeTracker() {
    if(String.IsNullOrEmpty(trackingCode)){
      Debug.Log("No tracking code set for 'Other' platforms - hits will not be set");
      trackingCodeSet = false;
      return;
    }
    if (GoogleAnalyticsV4.belowThreshold(logLevel, GoogleAnalyticsV4.DebugMode.INFO)) {
      Debug.Log("Platform is not Android or iOS - " +
          "hits will be sent using measurement protocol.");
    }
    screenRes = Screen.width + "x" + Screen.height;
    clientId = SystemInfo.deviceUniqueIdentifier;
    string language = Application.systemLanguage.ToString();
    optOut = false;
#if !UNITY_WP8
    CultureInfo[] cultureInfos = CultureInfo.GetCultures(CultureTypes.AllCultures);
    foreach (CultureInfo info in cultureInfos) {
      if (info.EnglishName == Application.systemLanguage.ToString()) {
        language = info.Name;
      }
    }
#endif
    try {
      url = "https://www.google-analytics.com/collect?v=1"
        + AddRequiredMPParameter(Fields.LANGUAGE, language)
        + AddRequiredMPParameter(Fields.SCREEN_RESOLUTION, screenRes)
        + AddRequiredMPParameter(Fields.APP_NAME, appName)
        + AddRequiredMPParameter(Fields.TRACKING_ID, trackingCode)
        + AddRequiredMPParameter(Fields.APP_ID, bundleIdentifier)
        + AddRequiredMPParameter(Fields.CLIENT_ID, clientId)
        + AddRequiredMPParameter(Fields.APP_VERSION, appVersion);
      if(anonymizeIP){
        url += AddOptionalMPParameter(Fields.ANONYMIZE_IP, 1);
      }
      if (GoogleAnalyticsV4.belowThreshold(logLevel, GoogleAnalyticsV4.DebugMode.VERBOSE)) {
        Debug.Log("Base URL for hits: " + url);
      }
    } catch (Exception) {
      if (GoogleAnalyticsV4.belowThreshold(logLevel, GoogleAnalyticsV4.DebugMode.WARNING)) {
        Debug.Log("Error building url.");
      }
    }
  }

  public void SetTrackerVal(Field field, object value) {
    trackerValues[field] = value;
  }

  private string AddTrackerVals() {
    if(!trackingCodeSet){
      return "";
    }
    string vals = "";
    foreach (KeyValuePair<Field, object> pair in trackerValues){
      vals += AddOptionalMPParameter(pair.Key, pair.Value);
    }
    return vals;
  }

  internal void StartSession() {
    startSessionOnNextHit = true;
  }

  internal void StopSession() {
    endSessionOnNextHit = true;
  }

  private void SendGaHitWithMeasurementProtocol(string url) {
    if (String.IsNullOrEmpty(url)) {
      if (GoogleAnalyticsV4.belowThreshold(logLevel, GoogleAnalyticsV4.DebugMode.WARNING)) {
        Debug.Log("No tracking code set for 'Other' platforms - hit will not be sent.");
      }
      return;
    }
    if (dryRun || optOut) {
      if (GoogleAnalyticsV4.belowThreshold(logLevel, GoogleAnalyticsV4.DebugMode.WARNING)) {
        Debug.Log("Dry run or opt out enabled - hits will not be sent.");
      }
      return;
    }
    if (startSessionOnNextHit) {
      url += AddOptionalMPParameter(Fields.SESSION_CONTROL, "start");
      startSessionOnNextHit = false;
    } else if (endSessionOnNextHit) {
      url += AddOptionalMPParameter(Fields.SESSION_CONTROL, "end");
      endSessionOnNextHit = false;
    }
    // Add random z to avoid caching
    string newUrl = url + "&z=" + UnityEngine.Random.Range(0, 500);
    if (GoogleAnalyticsV4.belowThreshold(logLevel, GoogleAnalyticsV4.DebugMode.VERBOSE)) {
      Debug.Log(newUrl);
    }
    AnalyticsManager.Instance?.StartCoroutine(this.HandleWWW(new WWW(newUrl)));
  }

  /*
    Make request using yield and coroutine to prevent lock up waiting on request to return.
  */
  public IEnumerator HandleWWW(WWW request)
  {
    while (!request.isDone)
    {
      yield return request;
      if (request.responseHeaders.ContainsKey("STATUS")) {
        if (request.responseHeaders["STATUS"].Contains("200 OK")) {
          if (GoogleAnalyticsV4.belowThreshold(logLevel, GoogleAnalyticsV4.DebugMode.INFO)) {
            Debug.Log("Successfully sent Google Analytics hit.");
          }
        } else {
          if (GoogleAnalyticsV4.belowThreshold(logLevel, GoogleAnalyticsV4.DebugMode.WARNING)) {
            Debug.LogWarning("Google Analytics hit request rejected with " +
                "status code " + request.responseHeaders["STATUS"]);
          }
        }
      } else {
        if (GoogleAnalyticsV4.belowThreshold(logLevel, GoogleAnalyticsV4.DebugMode.WARNING)) {
          Debug.LogWarning("Google Analytics hit request failed with error "
              + request.error);
        }
      }
    }
  }

  private string AddRequiredMPParameter(Field parameter, object value) {
    if(!trackingCodeSet){
      return "";
    } else if (value == null) {
      if (GoogleAnalyticsV4.belowThreshold(logLevel, GoogleAnalyticsV4.DebugMode.WARNING)) {
        Debug.LogWarning("Value was null for required parameter " + parameter + ". Hit cannot be sent");
      }
      throw new ArgumentNullException();
    } else {
      return parameter + "=" + WWW.EscapeURL(value.ToString());
    }
  }

  private string AddRequiredMPParameter(Field parameter, string value) {
    if(!trackingCodeSet){
      return "";
    } else if (value == null) {
      if (GoogleAnalyticsV4.belowThreshold(logLevel, GoogleAnalyticsV4.DebugMode.WARNING)) {
        Debug.LogWarning("Value was null for required parameter " + parameter + ". Hit cannot be sent");
      }
      throw new ArgumentNullException();
    } else {
      return parameter + "=" + WWW.EscapeURL(value);
    }
  }

  private string AddOptionalMPParameter(Field parameter, object value) {
    if (value == null || !trackingCodeSet) {
      return "";
    } else {
      return parameter + "=" +  WWW.EscapeURL(value.ToString());
    }
  }

  private string AddOptionalMPParameter(Field parameter, string value) {
    if (String.IsNullOrEmpty(value) || !trackingCodeSet) {
      return "";
    } else {
      return parameter + "=" + WWW.EscapeURL(value);
    }
  }

  private string AddCustomVariables<T>(HitBuilder<T> builder) {
    if(!trackingCodeSet){
      return "";
    }
    String url = "";
    foreach(KeyValuePair<int, string> entry in builder.GetCustomDimensions())
    {
      if (entry.Value != null) {
        url += Fields.CUSTOM_DIMENSION.ToString() + entry.Key + "=" +
            WWW.EscapeURL(entry.Value.ToString());
      }
    }
    foreach(KeyValuePair<int, float> entry in builder.GetCustomMetrics())
    {
      if (entry.Value != null) {
        url += Fields.CUSTOM_METRIC.ToString() + entry.Key + "=" +
            WWW.EscapeURL(entry.Value.ToString());
      }
    }

    if(!String.IsNullOrEmpty(url)){
      if (GoogleAnalyticsV4.belowThreshold(logLevel, GoogleAnalyticsV4.DebugMode.VERBOSE)) {
        Debug.Log("Added custom variables to hit.");
      }
    }
    return url;
  }


  private string AddCampaignParameters<T>(HitBuilder<T> builder) {
    if(!trackingCodeSet){
      return "";
    }
    String url = "";
    url += AddOptionalMPParameter(Fields.CAMPAIGN_NAME, builder.GetCampaignName());
    url += AddOptionalMPParameter(Fields.CAMPAIGN_SOURCE, builder.GetCampaignSource());
    url += AddOptionalMPParameter(Fields.CAMPAIGN_MEDIUM, builder.GetCampaignMedium());
    url += AddOptionalMPParameter(Fields.CAMPAIGN_KEYWORD, builder.GetCampaignKeyword());
    url += AddOptionalMPParameter(Fields.CAMPAIGN_CONTENT, builder.GetCampaignContent());
    url += AddOptionalMPParameter(Fields.CAMPAIGN_ID, builder.GetCampaignID());
    url += AddOptionalMPParameter(Fields.GCLID, builder.GetGclid());
    url += AddOptionalMPParameter(Fields.DCLID, builder.GetDclid());

    if(!String.IsNullOrEmpty(url)){
      if (GoogleAnalyticsV4.belowThreshold(logLevel, GoogleAnalyticsV4.DebugMode.VERBOSE)) {
        Debug.Log("Added campaign parameters to hit. url:" + url);
      }
    }
    return url;
  }

  public void LogScreen(AppViewHitBuilder builder) {
    trackerValues[Fields.SCREEN_NAME] = null;

    SendGaHitWithMeasurementProtocol(url
        + AddRequiredMPParameter(Fields.HIT_TYPE, "appview")
        + AddRequiredMPParameter(Fields.SCREEN_NAME, builder.GetScreenName())
        + AddCustomVariables(builder)
        + AddCampaignParameters(builder)
        + AddTrackerVals());
  }

  public void LogEvent(EventHitBuilder builder) {
    trackerValues[Fields.EVENT_CATEGORY] = null;
    trackerValues[Fields.EVENT_ACTION] = null;
    trackerValues[Fields.EVENT_LABEL] = null;
    trackerValues[Fields.EVENT_VALUE] = null;

    SendGaHitWithMeasurementProtocol(url
        + AddRequiredMPParameter(Fields.HIT_TYPE, "event")
        + AddOptionalMPParameter(Fields.EVENT_CATEGORY, builder.GetEventCategory())
        + AddOptionalMPParameter(Fields.EVENT_ACTION, builder.GetEventAction())
        + AddOptionalMPParameter(Fields.EVENT_LABEL, builder.GetEventLabel())
        + AddOptionalMPParameter(Fields.EVENT_VALUE, builder.GetEventValue())
        + AddCustomVariables(builder)
        + AddCampaignParameters(builder)
        + AddTrackerVals());
  }

  public void LogTransaction(TransactionHitBuilder builder) {
    trackerValues[Fields.TRANSACTION_ID] = null;
    trackerValues[Fields.TRANSACTION_AFFILIATION] = null;
    trackerValues[Fields.TRANSACTION_REVENUE] = null;
    trackerValues[Fields.TRANSACTION_SHIPPING] = null;
    trackerValues[Fields.TRANSACTION_TAX] = null;
    trackerValues[Fields.CURRENCY_CODE] = null;

    SendGaHitWithMeasurementProtocol(url
        + AddRequiredMPParameter(Fields.HIT_TYPE, "transaction")
        + AddRequiredMPParameter(Fields.TRANSACTION_ID, builder.GetTransactionID())
        + AddOptionalMPParameter(Fields.TRANSACTION_AFFILIATION, builder.GetAffiliation())
        + AddOptionalMPParameter(Fields.TRANSACTION_REVENUE, builder.GetRevenue())
        + AddOptionalMPParameter(Fields.TRANSACTION_SHIPPING, builder.GetShipping())
        + AddOptionalMPParameter(Fields.TRANSACTION_TAX, builder.GetTax())
        + AddOptionalMPParameter(Fields.CURRENCY_CODE, builder.GetCurrencyCode())
        + AddCustomVariables(builder)
        + AddCampaignParameters(builder)
        + AddTrackerVals());
  }

  public void LogItem(ItemHitBuilder builder) {

    trackerValues[Fields.TRANSACTION_ID] = null;
    trackerValues[Fields.ITEM_NAME] = null;
    trackerValues[Fields.ITEM_SKU] = null;
    trackerValues[Fields.ITEM_CATEGORY] = null;
    trackerValues[Fields.ITEM_PRICE] = null;
    trackerValues[Fields.ITEM_QUANTITY] = null;
    trackerValues[Fields.CURRENCY_CODE] = null;

    SendGaHitWithMeasurementProtocol(url
        + AddRequiredMPParameter(Fields.HIT_TYPE, "item")
        + AddRequiredMPParameter(Fields.TRANSACTION_ID, builder.GetTransactionID())
        + AddRequiredMPParameter(Fields.ITEM_NAME, builder.GetName())
        + AddOptionalMPParameter(Fields.ITEM_SKU, builder.GetSKU())
        + AddOptionalMPParameter(Fields.ITEM_CATEGORY, builder.GetCategory())
        + AddOptionalMPParameter(Fields.ITEM_PRICE, builder.GetPrice())
        + AddOptionalMPParameter(Fields.ITEM_QUANTITY, builder.GetQuantity())
        + AddOptionalMPParameter(Fields.CURRENCY_CODE, builder.GetCurrencyCode())
        + AddCustomVariables(builder)
        + AddCampaignParameters(builder)
        + AddTrackerVals());
  }

  public void LogException(ExceptionHitBuilder builder) {

    trackerValues[Fields.EX_DESCRIPTION] = null;
    trackerValues[Fields.EX_FATAL] = null;

    SendGaHitWithMeasurementProtocol(url
        + AddRequiredMPParameter(Fields.HIT_TYPE, "exception")
        + AddOptionalMPParameter(Fields.EX_DESCRIPTION, builder.GetExceptionDescription())
        + AddOptionalMPParameter(Fields.EX_FATAL, builder.IsFatal())
        + AddTrackerVals());
  }

  public void LogSocial(SocialHitBuilder builder) {

    trackerValues[Fields.SOCIAL_NETWORK] = null;
    trackerValues[Fields.SOCIAL_ACTION] = null;
    trackerValues[Fields.SOCIAL_TARGET] = null;

    SendGaHitWithMeasurementProtocol(url
        + AddRequiredMPParameter(Fields.HIT_TYPE, "social")
        + AddRequiredMPParameter(Fields.SOCIAL_NETWORK, builder.GetSocialNetwork())
        + AddRequiredMPParameter(Fields.SOCIAL_ACTION, builder.GetSocialAction())
        + AddRequiredMPParameter(Fields.SOCIAL_TARGET, builder.GetSocialTarget())
        + AddCustomVariables(builder)
        + AddCampaignParameters(builder)
        + AddTrackerVals());
  }

  public void LogTiming(TimingHitBuilder builder) {

    trackerValues[Fields.TIMING_CATEGORY] = null;
    trackerValues[Fields.TIMING_VALUE] = null;
    trackerValues[Fields.TIMING_LABEL] = null;
    trackerValues[Fields.TIMING_VAR] = null;

    SendGaHitWithMeasurementProtocol(url
        + AddRequiredMPParameter(Fields.HIT_TYPE, "timing")
        + AddOptionalMPParameter(Fields.TIMING_CATEGORY, builder.GetTimingCategory())
        + AddOptionalMPParameter(Fields.TIMING_VALUE, builder.GetTimingInterval())
        + AddOptionalMPParameter(Fields.TIMING_LABEL, builder.GetTimingLabel())
        + AddOptionalMPParameter(Fields.TIMING_VAR, builder.GetTimingName())
        + AddCustomVariables(builder)
        + AddCampaignParameters(builder)
        + AddTrackerVals());
  }

  public void ClearUserIDOverride() {
    SetTrackerVal(Fields.USER_ID, null);
  }

  public void SetTrackingCode(string trackingCode) {
    this.trackingCode = trackingCode;
  }

  public void SetBundleIdentifier(string bundleIdentifier) {
    this.bundleIdentifier = bundleIdentifier;
  }

  public void SetAppName(string appName) {
    this.appName = appName;
  }

  public void SetAppVersion(string appVersion) {
    this.appVersion = appVersion;
  }

  public void SetLogLevelValue(GoogleAnalyticsV4.DebugMode logLevel) {
    this.logLevel = logLevel;
  }

  public void SetAnonymizeIP(bool anonymizeIP) {
    this.anonymizeIP = anonymizeIP;
  }

  public void SetDryRun(bool dryRun) {
    this.dryRun = dryRun;
  }

  public void SetOptOut(bool optOut) {
    this.optOut = optOut;
  }

#endif
}
