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
using System.Runtime.InteropServices;

/*
  Wrapper for Objective-C iOS SDK
*/
public class GAIHandler {
#if UNITY_IPHONE && !UNITY_EDITOR
  [DllImport("__Internal")]
  private static extern void setOptOut(bool optOut);
  public void _setOptOut(bool optOut){
    setOptOut(optOut);
  }

  [DllImport("__Internal")]
  private static extern void setDispatchInterval(int time);
  public void _setDispatchInterval(int time){
    setDispatchInterval(time);
  }

  [DllImport("__Internal")]
  private static extern void anonymizeIP();
  public void _anonymizeIP(){
    anonymizeIP();
  }

  [DllImport("__Internal")]
  private static extern void enableIDFACollection();
  public void _enableIDFACollection(){
    enableIDFACollection();
  }

  [DllImport("__Internal")]
  private static extern void setTrackUncaughtExceptions(bool trackUncaughtExceptions);
  public void _setTrackUncaughtExceptions(bool trackUncaughtExceptions){
    setTrackUncaughtExceptions(trackUncaughtExceptions);
  }

  [DllImport("__Internal")]
  private static extern void setDryRun(bool dryRun);
  public void _setDryRun(bool dryRun){
    setDryRun(dryRun);
  }

  [DllImport("__Internal")]
  private static extern void setSampleFrequency(int sampleFrequency);
  public void _setSampleFrequency(int sampleFrequency){
    setSampleFrequency(sampleFrequency);
  }

  [DllImport("__Internal")]
  private static extern void setLogLevel(int logLevel);
  public void _setLogLevel(int logLevel){
    setLogLevel(logLevel);
  }

  [DllImport("__Internal")]
  private static extern void startSession();
  public void _startSession(){
    startSession();
  }

  [DllImport("__Internal")]
  private static extern void stopSession();
  public void _stopSession(){
    stopSession();
  }

  [DllImport("__Internal")]
  private static extern IntPtr trackerWithName(string name, string trackingId);
  public IntPtr _getTrackerWithName(string name, string trackingId){
    return trackerWithName(name, trackingId);
  }

  [DllImport("__Internal")]
  private static extern IntPtr trackerWithTrackingId(string trackingId);
  public IntPtr _getTrackerWithTrackingId(string trackingId){
    return trackerWithTrackingId(trackingId);
  }

  [DllImport("__Internal")]
  private static extern void set(string parameterName, string value);
  public void _set(string parameterName, object value){
    set(parameterName, value.ToString());
  }

  [DllImport("__Internal")]
  private static extern void setBool(string parameterName, bool value);
  public void _setBool(string parameterName, bool value){
    setBool(parameterName, value);
  }

  [DllImport("__Internal")]
  private static extern string get(string parameterName);
  public string _get(string parameterName){
    return get(parameterName);
  }

  [DllImport("__Internal")]
  private static extern void dispatch();
  public void _dispatchHits(){
    dispatch();
  }

  [DllImport("__Internal")]
  private static extern void sendAppView(string screenName);
  public void _sendAppView(AppViewHitBuilder builder){
    _buildCustomMetricsDictionary(builder);
    _buildCustomDimensionsDictionary(builder);
    _buildCampaignParametersDictionary(builder);
    sendAppView(builder.GetScreenName());
  }

  [DllImport("__Internal")]
  private static extern void sendEvent(string category, string action, string label, long value);
  public void _sendEvent(EventHitBuilder builder){
    _buildCustomMetricsDictionary(builder);
    _buildCustomDimensionsDictionary(builder);
    _buildCampaignParametersDictionary(builder);
    sendEvent(builder.GetEventCategory(), builder.GetEventAction(), builder.GetEventLabel(), builder.GetEventValue());
  }

  [DllImport("__Internal")]
  private static extern void sendTransaction(string transactionID, string affiliation, double revenue, double tax, double shipping, string currencyCode);
  public void _sendTransaction(TransactionHitBuilder builder){
    _buildCustomMetricsDictionary(builder);
    _buildCustomDimensionsDictionary(builder);
    _buildCampaignParametersDictionary(builder);
    sendTransaction(builder.GetTransactionID(), builder.GetAffiliation(), builder.GetRevenue(), builder.GetTax(), builder.GetShipping(), builder.GetCurrencyCode());
  }

  [DllImport("__Internal")]
  private static extern void sendItemWithTransaction(string transactionID, string name, string sku, string category, double price, long quantity, string currencyCode);
  public void _sendItemWithTransaction(ItemHitBuilder builder){
    _buildCustomMetricsDictionary(builder);
    _buildCustomDimensionsDictionary(builder);
    _buildCampaignParametersDictionary(builder);
    sendItemWithTransaction(builder.GetTransactionID(), builder.GetName(), builder.GetSKU(), builder.GetCategory(), builder.GetPrice(), builder.GetQuantity(),builder.GetCurrencyCode());
  }

  [DllImport("__Internal")]
  private static extern void sendException(string exceptionDescription, bool isFatal);
  public void _sendException(ExceptionHitBuilder builder){
    _buildCustomMetricsDictionary(builder);
    _buildCustomDimensionsDictionary(builder);
    _buildCampaignParametersDictionary(builder);
    sendException(builder.GetExceptionDescription(), builder.IsFatal());
  }

  [DllImport("__Internal")]
  private static extern void sendSocial(string socialNetwork, string socialAction, string targetUrl);
  public void _sendSocial(SocialHitBuilder builder){
    _buildCustomMetricsDictionary(builder);
    _buildCustomDimensionsDictionary(builder);
    _buildCampaignParametersDictionary(builder);
    sendSocial(builder.GetSocialNetwork(), builder.GetSocialAction(), builder.GetSocialTarget());
  }

  [DllImport("__Internal")]
  private static extern void sendTiming(string timingCategory,long timingInterval, string timingName, string timingLabel);
  public void _sendTiming(TimingHitBuilder builder){
    _buildCustomMetricsDictionary(builder);
    _buildCustomDimensionsDictionary(builder);
    _buildCampaignParametersDictionary(builder);
    sendTiming(builder.GetTimingCategory(), builder.GetTimingInterval(), builder.GetTimingName(), builder.GetTimingLabel());
  }

  [DllImport("__Internal")]
  private static extern void addCustomDimensionToDictionary(int key, string value);
  public void _buildCustomDimensionsDictionary<T>(HitBuilder<T> builder){
    foreach(KeyValuePair<int, string> entry in builder.GetCustomDimensions())
    {
      addCustomDimensionToDictionary(entry.Key, entry.Value);
    }
  }

  [DllImport("__Internal")]
  private static extern void addCustomMetricToDictionary(int key, string value);
  public void _buildCustomMetricsDictionary<T>(HitBuilder<T> builder){
    foreach(KeyValuePair<int, float> entry in builder.GetCustomMetrics())
    {
      addCustomMetricToDictionary(entry.Key, entry.Value.ToString());
    }
  }

  [DllImport("__Internal")]
  private static extern void buildCampaignParametersDictionary(string source, string medium, string name, string content, string keyword);
  public void _buildCampaignParametersDictionary<T>(HitBuilder<T> builder){
    if(!String.IsNullOrEmpty(builder.GetCampaignSource())){
      buildCampaignParametersDictionary(builder.GetCampaignSource(),
          builder.GetCampaignMedium() != null ? builder.GetCampaignMedium() : "",
          builder.GetCampaignName() != null? builder.GetCampaignName() : "",
          builder.GetCampaignContent() != null? builder.GetCampaignContent() : "",
          builder.GetCampaignKeyword() != null? builder.GetCampaignKeyword() : "");
    } else if(!String.IsNullOrEmpty(builder.GetCampaignMedium()) ||
        !String.IsNullOrEmpty(builder.GetCampaignName()) ||
        !String.IsNullOrEmpty(builder.GetCampaignMedium()) ||
        !String.IsNullOrEmpty(builder.GetCampaignContent()) ||
        !String.IsNullOrEmpty(builder.GetCampaignKeyword())) {
      Debug.Log("A required parameter (campaign source) is null or empty. No campaign parameters will be added to hit.");
    }
  }
  #endif
}
