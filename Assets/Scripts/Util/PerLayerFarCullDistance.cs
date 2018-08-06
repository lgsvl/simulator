/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;

public class PerLayerFarCullDistance : MonoBehaviour {

    public float closeItemsCullDistance = 500f;

	// Use this for initialization
	void Start () {
        float[] distances = new float[32];
        distances[LayerMask.NameToLayer("CloseCulling")] = closeItemsCullDistance;
        GetComponent<Camera>().layerCullDistances = distances;
	}
	
}
