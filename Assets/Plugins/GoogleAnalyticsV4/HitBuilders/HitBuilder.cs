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
using System.Collections;
using System.Collections.Generic;

/*
  Base class for building hits. This class stores data which can be sent with
  any hit type but cannot be sent independent of other hits.
  TODO(emmanuellemm): If we are able to get the referring intent from Unity,
  the campaign parameters should be set automatically.
 */
public abstract class HitBuilder<T> {

  private Dictionary<int, string> customDimensions =
      new Dictionary<int,string>();
  private Dictionary<int, float> customMetrics = new Dictionary<int,float>();

  private string campaignName = "";
  private string campaignSource = "";
  private string campaignMedium = "";
  private string campaignKeyword = "";
  private string campaignContent = "";
  private string campaignID = "";
  private string gclid = "";
  private string dclid = "";

  public abstract T GetThis();
  public abstract T Validate();

  public T SetCustomDimension(int dimensionNumber, string value) {
    customDimensions.Add(dimensionNumber, value);
    return GetThis();
  }

  public Dictionary<int, string> GetCustomDimensions() {
    return customDimensions;
  }

  public T SetCustomMetric(int metricNumber, float value) {
    customMetrics.Add(metricNumber, value);
    return GetThis();
  }

  public Dictionary<int, float> GetCustomMetrics() {
    return customMetrics;
  }

  public string GetCampaignName() {
    return campaignName;
  }

  public T SetCampaignName(string campaignName) {
    if (campaignName != null) {
      this.campaignName = campaignName;
    }
    return GetThis();
  }

  public string GetCampaignSource() {
    return campaignSource;
  }

  public T SetCampaignSource(string campaignSource) {
    if (campaignSource != null) {
      this.campaignSource = campaignSource;
    } else {
      Debug.Log("Campaign source cannot be null or empty");
    }
    return GetThis();
  }

  public string GetCampaignMedium() {
    return campaignMedium;
  }

  public T SetCampaignMedium(string campaignMedium) {
    if (campaignMedium != null) {
      this.campaignMedium = campaignMedium;
    }
    return GetThis();
  }

  public string GetCampaignKeyword() {
    return campaignKeyword;
  }

  public T SetCampaignKeyword(string campaignKeyword) {
    if (campaignKeyword != null) {
      this.campaignKeyword = campaignKeyword;
    }
    return GetThis();
  }

  public string GetCampaignContent() {
    return campaignContent;
  }

  public T SetCampaignContent(string campaignContent) {
    if (campaignContent != null) {
      this.campaignContent = campaignContent;
    }
    return GetThis();
  }

  public string GetCampaignID() {
    return campaignID;
  }

  public T SetCampaignID(string campaignID) {
    if (campaignID != null) {
      this.campaignID = campaignID;
    }
    return GetThis();
  }

  public string GetGclid() {
    return gclid;
  }

  public T SetGclid(string gclid) {
    if (gclid != null) {
      this.gclid = gclid;
    }
    return GetThis();
  }

  public string GetDclid() {
    return dclid;
  }

  public T SetDclid(string dclid) {
    if (dclid != null) {
      this.dclid = dclid;
    }
    return GetThis();
  }
}
