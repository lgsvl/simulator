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

/*
  Ranged Tooltip attribute for displaying properties with range and
  a tooltip in the inspector.
*/
public class RangedTooltipAttribute : PropertyAttribute {
  public readonly float min;
  public readonly float max;
  public readonly string text;

  public RangedTooltipAttribute(string text, float min, float max) {
    this.text = text;
    this.min = min;
    this.max = max;
  }
}
