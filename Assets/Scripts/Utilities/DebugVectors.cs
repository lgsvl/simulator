/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

public class DebugVectors : MonoBehaviour
{
    Transform lightTransform;
    Transform cameraTransform;
    Vector3 sunCamVector;

    private void Update()
    {
        if (lightTransform == null)
        {
            if (SimulatorManager.Instance == null)
                return;
            if (SimulatorManager.Instance.EnvironmentEffectsManager == null)
                return;
            if (SimulatorManager.Instance.EnvironmentEffectsManager.SunGO == null)
                return;

            //Locate the sun
            lightTransform = SimulatorManager.Instance.EnvironmentEffectsManager.SunGO.transform;
        }

        if (cameraTransform == null)
        {
            if (Camera.main == null)
                return;

            //Locate the cam
            cameraTransform = Camera.main.transform;
        }

        //Get the vector between cam and sun
        sunCamVector = lightTransform.position - cameraTransform.position;

        Debug.Log("<color=red>sunCamVector: <color=red>" + sunCamVector);
        Debug.DrawLine(lightTransform.position, cameraTransform.position, Color.red, 1f);
        Debug.DrawRay(lightTransform.position, sunCamVector, Color.yellow, 1f);
        Debug.Break();
    }
}
