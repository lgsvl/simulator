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
using System;

public class SocialHitBuilder : HitBuilder<SocialHitBuilder> {

  private string socialNetwork = "";
  private string socialAction = "";
  private string socialTarget = "";

  public string GetSocialNetwork(){
    return socialNetwork;
  }

  public SocialHitBuilder SetSocialNetwork(string socialNetwork) {
    if(socialNetwork != null){
      this.socialNetwork = socialNetwork;
    }
    return this;
  }

  public string GetSocialAction(){
    return socialAction;
  }

  public SocialHitBuilder SetSocialAction(string socialAction) {
    if(socialAction != null){
      this.socialAction = socialAction;
    }
    return this;
  }

  public string GetSocialTarget(){
    return socialTarget;
  }

  public SocialHitBuilder SetSocialTarget(string socialTarget) {
    if(socialTarget != null){
      this.socialTarget = socialTarget;
    }
    return this;
  }

  public override SocialHitBuilder GetThis(){
    return this;
  }

  public override SocialHitBuilder Validate(){
    if(String.IsNullOrEmpty(socialNetwork)){
      Debug.LogError("No social network provided - Social hit cannot be sent");
      return null;
    }
    if(String.IsNullOrEmpty(socialAction)){
      Debug.LogError("No social action provided - Social hit cannot be sent");
      return null;
    }
    if(String.IsNullOrEmpty(socialTarget)){
      Debug.LogError("No social target provided - Social hit cannot be sent");
      return null;
    }
    return this;
  }
}
