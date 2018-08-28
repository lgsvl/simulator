/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;

public class PedalInputController : MonoBehaviour {

    private static PedalInputController instance;

    public static PedalInputController Instance
    {
        get
        {
            if (instance == null)
            {
                var res = Resources.Load<GameObject>("PedalInputController");
                var go = GameObject.Instantiate(res) as GameObject;
                instance = go.GetComponent<PedalInputController>();
                DontDestroyOnLoad(instance.gameObject);
            }
            return instance;
        }
    }

    public AnimationCurve throttleInputCurve;
    public AnimationCurve brakeInputCurve;

}
