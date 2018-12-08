/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

// To add headlights to a vehicle, find it in the CarSelect scene (NewCarSelect 2), under
// SelectSceneOrigin/Available Car Models/ .. Attach a CarHeadlights script to it, and add
// two lights where the headlights should be. Assign them to the "Right" and "Left" properties
// here, respectively.  Adjust their default properties in the Light inspector window, and then
// adjust the angles for the high beam, low beam, and beam separation here.

// TODO: How do we do light shafts?

using UnityEngine;

public enum LightMode { OFF, LOWBEAM, HIGHBEAM }

public class CarHeadlights : MonoBehaviour
{
    // TODO: add beam angles to admin config panel?
    public float lowBeamAngle = 30f;
    public float highBeamAngle = 10f;
    public float lowBeamIntensity = 1f;
    public float highBeamIntensity = 2f;
    public LightMode lightMode = LightMode.OFF;

    public float separationAngle = 5f;
    public Light left;
    public Light right;
    public Light left_tail;
    public Light right_tail;

    public void Awake()
    {
        if (left) {
            left.enabled = false;
            left.gameObject.SetActive(false);
        }
        if (right) {
            right.enabled = false;
            right.gameObject.SetActive(false);
        }
        if (left_tail)
        {
            left_tail.enabled = false;
            left_tail.gameObject.SetActive(false);
        }
        if (right_tail)
        {
            right_tail.enabled = false;
            right_tail.gameObject.SetActive(false);
        }
    }

    public void Update()
    {
        if (GetState())
        {
            left.transform.localRotation = Quaternion.Euler(lightMode == LightMode.LOWBEAM ? lowBeamAngle : highBeamAngle, -separationAngle, 0f);
            right.transform.localRotation = Quaternion.Euler(lightMode == LightMode.LOWBEAM ? lowBeamAngle : highBeamAngle, separationAngle, 0f);

            left.intensity = lightMode == LightMode.LOWBEAM ? lowBeamIntensity : highBeamIntensity;
            right.intensity = lightMode == LightMode.LOWBEAM ? lowBeamIntensity : highBeamIntensity;
        }
    }

    public void SetRange(float newRange)
    {
        left.range = newRange;
        right.range = newRange;
    }

    public bool GetState()
    {
        if (left)
            return left.gameObject.activeInHierarchy;
        return false;
    }

    public bool Headlights
    {
        get { return left.enabled; }
        set
        {
            left.enabled = value;
            right.enabled = value;
            left.gameObject.SetActive(value);
            right.gameObject.SetActive(value);
        }
    }

    public bool Reverselights
    {
        get { return left_tail.enabled; }
        set
        {
            //left_tail.enabled = value;
            //right_tail.enabled = value;
            //left_tail.gameObject.SetActive(value);
            //right_tail.gameObject.SetActive(value);
        }
    }

    public void SetMode(LightMode lm)
    {
        lightMode = lm;
    }
}
