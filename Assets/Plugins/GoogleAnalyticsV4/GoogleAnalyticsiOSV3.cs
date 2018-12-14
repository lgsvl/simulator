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
using System.Collections.Generic;

/*
  GoogleAnalyticsiOSV3 handles building hits using the iOS SDK.
  Developers should call the methods in GoogleAnalyticsV4, which will call the
  appropriate methods in this class if the application is built for iOS.
*/
public class GoogleAnalyticsiOSV3 {

#if UNITY_IPHONE && !UNITY_EDITOR
  private string trackingCode;
  private string appName;
  private string bundleIdentifier;
  private string appVersion;
  private int dispatchPeriod;
  private int sampleFrequency;
  private GoogleAnalyticsV4.DebugMode logLevel;
  private bool anonymizeIP;
  private bool adIdCollection;
  private bool dryRun;
  private GAIHandler handler;

  internal void InitializeTracker () {
    Debug.Log ("Initializing Google Analytics iOS Tracker.");
    handler = new GAIHandler();
    handler._setDispatchInterval(dispatchPeriod);
    handler._setDryRun(dryRun);
    handler._setTrackUncaughtExceptions(true);
    SetLogLevel(logLevel);
    handler._getTrackerWithTrackingId(trackingCode);

    handler._setSampleFrequency(sampleFrequency);
    SetTrackerVal(Fields.APP_NAME, appName);
    SetTrackerVal(Fields.APP_ID, bundleIdentifier);
    SetTrackerVal(Fields.APP_VERSION, appVersion);

    if (adIdCollection) {
      handler._enableIDFACollection();
    }

    if(anonymizeIP) {
      handler._anonymizeIP();
    }
  }

  private void SetLogLevel(GoogleAnalyticsV4.DebugMode logLevel) {
    switch(logLevel)
    {
      case GoogleAnalyticsV4.DebugMode.ERROR:
        handler._setLogLevel(1);
        break;
      case GoogleAnalyticsV4.DebugMode.VERBOSE:
        handler._setLogLevel(4);
        break;
      case GoogleAnalyticsV4.DebugMode.INFO:
        handler._setLogLevel(3);
        break;
      default:
        handler._setLogLevel(2);
        break;
    }
  }

  public void ClearUserIDOverride() {
    SetTrackerVal(Fields.USER_ID, null);
  }

  public void DispatchHits(){
    handler._dispatchHits();
  }

  public void StartSession(){
    handler._startSession();
  }

  public void StopSession(){
    handler._stopSession();
  }

  public void LogScreen(AppViewHitBuilder builder){
    handler._sendAppView(builder);
  }

  public void LogEvent(EventHitBuilder builder){
    handler._sendEvent(builder);
  }

  internal void LogTransaction(TransactionHitBuilder builder) {
    handler._sendTransaction(builder);
  }

  internal void LogItem(ItemHitBuilder builder) {
    handler._sendItemWithTransaction(builder);
  }

  public void LogException(ExceptionHitBuilder builder) {
    handler._sendException(builder);
  }

  public void LogSocial(SocialHitBuilder builder) {
    handler._sendSocial(builder);
  }

  public void LogTiming(TimingHitBuilder builder) {
    handler._sendTiming(builder);
  }

  public void SetOptOut(bool optOut) {
    handler._setOptOut(optOut);
  }

  public void SetTrackerVal(Field fieldName, object value){
    handler._set(fieldName.ToString(), value);
  }

  public void SetTrackingCode(string trackingCode) {
    this.trackingCode = trackingCode;
  }

  public void SetAppName(string appName) {
    this.appName = appName;
  }

  public void SetBundleIdentifier(string bundleIdentifier) {
    this.bundleIdentifier = bundleIdentifier;
  }

  public void SetAppVersion(string appVersion) {
    this.appVersion = appVersion;
  }

  public void SetDispatchPeriod(int dispatchPeriod) {
    this.dispatchPeriod = dispatchPeriod;
  }

  public void SetSampleFrequency(int sampleFrequency) {
    this.sampleFrequency = sampleFrequency;
  }

  public void SetLogLevelValue(GoogleAnalyticsV4.DebugMode logLevel) {
    this.logLevel = logLevel;
  }

  public void SetAnonymizeIP(bool anonymizeIP) {
    this.anonymizeIP = anonymizeIP;
  }

  public void SetAdIdCollection(bool adIdCollection) {
    this.adIdCollection = adIdCollection;
  }

  public void SetDryRun(bool dryRun) {
    this.dryRun = dryRun;
  }

#endif
}
