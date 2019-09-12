/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Simulator.Map;
using System.Collections.ObjectModel;
using System.Linq;

[CustomEditor(typeof(MapOrigin))]
public class MapOriginEditor : Editor
{
    TimeZoneInfo[] TimeZones;

    private void Awake()
    {
        TimeZones = TimeZoneInfo.GetSystemTimeZones().OrderBy(tz => tz.BaseUtcOffset).ToArray();
    }

    public override void OnInspectorGUI()
    {
        MapOrigin origin = (MapOrigin)target;
        DrawDefaultInspector();

        int currentlySelected = -1;
        if (origin.TimeZoneSerialized != null)
        {
            currentlySelected = Array.FindIndex(TimeZones, tz => tz.DisplayName == origin.TimeZoneString);
            if (currentlySelected == -1)
            {
                var timeZone = origin.TimeZone;
                currentlySelected = Array.FindIndex(TimeZones, tz => tz.BaseUtcOffset == timeZone.BaseUtcOffset);
            }
        }

        var values = TimeZones.Select(tz => tz.DisplayName.Replace("&", "&&")).ToArray();
        currentlySelected = EditorGUILayout.Popup(currentlySelected, values);
        if (currentlySelected != -1)
        {
            origin.TimeZoneSerialized = TimeZones[currentlySelected].ToSerializedString();
            origin.TimeZoneString = TimeZones[currentlySelected].DisplayName;

            EditorUtility.SetDirty(origin);
        }
    }
}
