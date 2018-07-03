/*
* MIT License
* 
* Copyright (c) 2017 Philip Tibom, Jonathan Jansson, Rickard Laurenius, 
* Tobias Alldén, Martin Chemander, Sherry Davar
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/

using System;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable 0067

/// <summary>
/// Handles almost all interactions between the lidar menu and all other components of the simulator.
/// Main functionality: Sends the settings of the lidar menu GUI to the LidarLaserMimic, LidarSensor and the PointCloud.
/// 
/// @author: Jonathan Jansson
/// </summary>
public class LidarMenu : MonoBehaviour {

    public delegate void UpdateMimicValuesDelegate(int noLasers, float upFOV, float lowFOV, float offset, float upNorm, float lowNorm);
    public delegate void PassLidarValuesToPointCloudDelegate(int numberOfLasers, float rotationSpeed, float rotationAnglePerStep);
    public delegate void StartSimulationDelegate(int numberOfLasers, float rotationSpeed, float rotationAnglePerStep, float rayDistance, float upperFOV
        , float lowerFOV, float offset, float upperNormal, float lowerNormal);
    public delegate void StopSimulationDelegate();

    public static event UpdateMimicValuesDelegate OnPassValuesToLaserMimic;
    public static event PassLidarValuesToPointCloudDelegate OnPassLidarValuesToPointCloud;
    public static event StartSimulationDelegate OnPassValuesToLidarSensor;
    public static event StopSimulationDelegate OnStopSimulation;

    //Defines the initial value of the lidar sensor and the settings in the lidar menu
    public float numberOfLasersVal = 64f;
    public float rotationSpeedHzVal = 1f;
    public float rotationAnglePerStepVal = 0.9f;
    public float rayDistanceVal = 200f;
    public float upperFOVVal = 10.5f;
    public float lowerFOVVal = 16f;
    public float offsetVal = 7.24f;
    public float upperNormalVal = -3.3f;
    public float lowerNormalVal = 16.87f;

    public Slider numberOfLasers;
    public Slider rotationSpeedHz;
    public Slider rotationAnglePerStep;
    public Slider rayDistance;
    public Slider upperFOV;
    public Slider lowerFOV;
    public Slider offset;
    public Slider upperNormal;
    public Slider lowerNormal;

    private bool laserPreviewInitialized = false;
    private bool guiValsInitialized = false;

    void Awake()
    {
        PlayButton.OnPlayToggled += PassValuesToLidarSensor;
        // FIX ME LATER!!!
        // EditorController.OnPointCloudToggle += PassLidarValuesToPointCloud;
        // PreviewLidarRays.tellLidarMenuInitialized += LaserPreviewIsInitialized;
    }
    void OnDestroy()
    {
        PlayButton.OnPlayToggled -= PassValuesToLidarSensor;
        // FIX ME LATER!!!
        // EditorController.OnPointCloudToggle -= PassLidarValuesToPointCloud;
        // PreviewLidarRays.tellLidarMenuInitialized -= LaserPreviewIsInitialized;
    }

    /// <summary>
    /// Calls the initiallization of the lidar menu values. 
    /// If the "PreviewLidarRays" script has been initialized, it calls a function to sync the preview to the values of the lidar menu.
    /// </summary>
    void Start () 
    {
        InitializeGUIValues();
        if (laserPreviewInitialized)
        {
            UpdateLaserMimicValues();
        }
    }

    /// <summary>
    /// Sets all initial values in the lidar menu.
    /// </summary>
    void InitializeGUIValues()
    {
        numberOfLasers.value = numberOfLasersVal;
        rotationSpeedHz.value = rotationSpeedHzVal;
        rotationAnglePerStep.value = rotationAnglePerStepVal;
        rayDistance.value = rayDistanceVal;
        upperFOV.value = upperFOVVal;
        lowerFOV.value = lowerFOVVal;
        offset.value = offsetVal;
        upperNormal.value = upperNormalVal;
        lowerNormal.value = lowerNormalVal;

        guiValsInitialized = true;
    }

    /// <summary>
    /// Gets called from the "PreviewLidarRays" script when the script has been initialized.
    /// If the "LidarMenu" script has initialized the values of the GUI, it sends the values back to the preview.
    /// </summary>
    /// <param name="isInitialized"></param>
    void LaserPreviewIsInitialized(bool isInitialized)
    {
        laserPreviewInitialized = true;
        if (guiValsInitialized)
        {
            UpdateLaserMimicValues();
        }
    }

    /// <summary>
    /// Invokes an event to sync the values of the lidar preview with the values of the Lidar menu GUI
    /// </summary>
    void UpdateLaserMimicValues()
    {
        if (laserPreviewInitialized)
        {
            try
            {
                OnPassValuesToLaserMimic((int)numberOfLasers.value, upperFOV.value, lowerFOV.value, offset.value, upperNormal.value, lowerNormal.value);
            }
            catch (NullReferenceException e)
            {
                Debug.Log("Event has no delegates: " + e);
            }
        }
    }


    /// <summary>
    /// If the play button is toggled on, this method invokes an event to sync the lidar sensor with the values of the lidar menu GUI
    /// </summary>
    void PassValuesToLidarSensor(bool play)
    {
        if (play)
        {
            try
            {
                OnPassValuesToLidarSensor((int)numberOfLasers.value, rotationSpeedHz.value, rotationAnglePerStep.value, rayDistance.value,
                    upperFOV.value, lowerFOV.value, offset.value, upperNormal.value, lowerNormal.value);
            }
            catch (NullReferenceException e)
            {
                Debug.Log("Event has no delegates: " + e);
            }
        }
    }

    /// <summary>
    /// Invokes an event to sync the point clouds settings with the values of the lidar menu GUI
    /// </summary>
    void PassLidarValuesToPointCloud()
    {
        try
        {
            OnPassLidarValuesToPointCloud((int)numberOfLasers.value, rotationSpeedHz.value, rotationAnglePerStep.value);
        }
        catch (NullReferenceException e)
        {
            Debug.Log("Event has no delegates: " + e);
        }
    }
}
