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

/*
  Available fields to use with SetOnTracker(Field fieldName, object value)
*/
public class Fields {

  //General
  public readonly static Field ANONYMIZE_IP = new Field("&aip");
  public readonly static Field HIT_TYPE = new Field("&t");
  public readonly static Field SESSION_CONTROL = new Field("&sc");

  public readonly static Field SCREEN_NAME = new Field("&cd");
  public readonly static Field LOCATION = new Field("&dl");
  public readonly static Field REFERRER = new Field("&dr");
  public readonly static Field PAGE = new Field("&dp");
  public readonly static Field HOSTNAME = new Field("&dh");
  public readonly static Field TITLE = new Field("&dt");
  public readonly static Field LANGUAGE = new Field("&ul");
  public readonly static Field ENCODING = new Field("&de");

  // System
  public readonly static Field SCREEN_COLORS = new Field("&sd");
  public readonly static Field SCREEN_RESOLUTION = new Field("&sr");
  public readonly static Field VIEWPORT_SIZE = new Field("&vp");

  // Application
  public readonly static Field APP_NAME = new Field("&an");
  public readonly static Field APP_ID = new Field("&aid");
  public readonly static Field APP_INSTALLER_ID = new Field("&aiid");
  public readonly static Field APP_VERSION = new Field("&av");

  // Visitor
  public readonly static Field CLIENT_ID = new Field("&cid");
  public readonly static Field USER_ID = new Field("&uid");

  // Campaign related fields; used in all hits.
  public readonly static Field CAMPAIGN_NAME = new Field("&cn");
  public readonly static Field CAMPAIGN_SOURCE = new Field("&cs");
  public readonly static Field CAMPAIGN_MEDIUM = new Field("&cm");
  public readonly static Field CAMPAIGN_KEYWORD = new Field("&ck");
  public readonly static Field CAMPAIGN_CONTENT = new Field("&cc");
  public readonly static Field CAMPAIGN_ID = new Field("&ci");
  // Autopopulated campaign fields
  public readonly static Field GCLID = new Field("&gclid");
  public readonly static Field DCLID = new Field("&dclid");


  // Event Hit (&t=event)
  public readonly static Field EVENT_CATEGORY = new Field("&ec");
  public readonly static Field EVENT_ACTION = new Field("&ea");
  public readonly static Field EVENT_LABEL = new Field("&el");
  public readonly static Field EVENT_VALUE = new Field("&ev");

  // Social Hit (&t=social)
  public readonly static Field SOCIAL_NETWORK = new Field("&sn");
  public readonly static Field SOCIAL_ACTION = new Field("&sa");
  public readonly static Field SOCIAL_TARGET = new Field("&st");

  // Timing Hit (&t=timing)
  public readonly static Field TIMING_VAR = new Field("&utv");
  public readonly static Field TIMING_VALUE = new Field("&utt");
  public readonly static Field TIMING_CATEGORY = new Field("&utc");
  public readonly static Field TIMING_LABEL = new Field("&utl");


  // Exception Hit (&t=exception)
  public readonly static Field EX_DESCRIPTION = new Field("&exd");
  public readonly static Field EX_FATAL = new Field("&exf");

  // Ecommerce (&t=transaction / &t=item)
  public readonly static Field CURRENCY_CODE = new Field("&cu");
  public readonly static Field TRANSACTION_ID = new Field("&ti");
  public readonly static Field TRANSACTION_AFFILIATION = new Field("&ta");
  public readonly static Field TRANSACTION_SHIPPING = new Field("&ts");
  public readonly static Field TRANSACTION_TAX = new Field("&tt");
  public readonly static Field TRANSACTION_REVENUE = new Field("&tr");
  public readonly static Field ITEM_SKU = new Field("&ic");
  public readonly static Field ITEM_NAME = new Field("&in");
  public readonly static Field ITEM_CATEGORY = new Field("&iv");
  public readonly static Field ITEM_PRICE = new Field("&ip");
  public readonly static Field ITEM_QUANTITY = new Field("&iq");

  // General Configuration
  public readonly static Field TRACKING_ID = new Field("&tid");
  public readonly static Field SAMPLE_RATE = new Field("&sf");
  public readonly static Field DEVELOPER_ID = new Field("&did");

  public readonly static Field CUSTOM_METRIC = new Field("&cm");
  public readonly static Field CUSTOM_DIMENSION = new Field("&cd");

  // Advertiser Id Fields
  public readonly static Field ADID = new Field("&adid");
  public readonly static Field IDFA = new Field("&idfa");
  public readonly static Field ATE = new Field("&ate");
}
