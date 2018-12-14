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
using System.Collections.Generic;
using System;

public class TimingHitBuilder : HitBuilder<TimingHitBuilder> {

  private string timingCategory = "";
  private long timingInterval;
  private string timingName = "";
  private string timingLabel = "";

  public string GetTimingCategory(){
    return timingCategory;
  }

  public TimingHitBuilder SetTimingCategory(string timingCategory) {
    if(timingCategory != null){
      this.timingCategory = timingCategory;
    }
    return this;
  }

  public long GetTimingInterval(){
    return timingInterval;
  }

  public TimingHitBuilder SetTimingInterval(long timingInterval) {
    this.timingInterval = timingInterval;
    return this;
  }

  public string GetTimingName(){
    return timingName;
  }

  public TimingHitBuilder SetTimingName(string timingName) {
    if(timingName != null){
      this.timingName = timingName;
    }
    return this;
  }

  public string GetTimingLabel(){
    return timingLabel;
  }

  public TimingHitBuilder SetTimingLabel(string timingLabel) {
    if(timingLabel != null){
      this.timingLabel = timingLabel;
    }
    return this;
  }

  public override TimingHitBuilder GetThis(){
    return this;
  }

  public override TimingHitBuilder Validate(){
    if(String.IsNullOrEmpty(timingCategory)){
      Debug.LogError("No timing category provided - Timing hit cannot be sent");
      return null;
    }
    if(timingInterval == 0L){
      Debug.Log("Interval in timing hit is 0.");
    }
    return this;
  }
}
