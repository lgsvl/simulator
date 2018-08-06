using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneController : UnitySingleton<SceneController>
{
    static SceneController instance;

    void Update ()
    {
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            //Turn off all light's indirect intensity to free GI calculation
            foreach (var light in FindObjectsOfType<Light>())
            {
                light.bounceIntensity = 0.0f;
            }
        }
    }
}
