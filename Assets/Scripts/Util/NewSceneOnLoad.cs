/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;

public class NewSceneOnLoad : MonoBehaviour
{

    public string sceneToLoad;

    void Start()
    {
#pragma warning disable 0618
        Application.LoadLevel(sceneToLoad);
#pragma warning restore 0618
    }

}