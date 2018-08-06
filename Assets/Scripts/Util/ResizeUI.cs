/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;

public class ResizeUI : MonoBehaviour {

    //resize UI elements
	void Awake() {

        float scale = Screen.height / 1080f;
        foreach(Transform t in transform) {
            if(t.GetComponent<GUITexture>())
            {
                Rect r = t.GetComponent<GUITexture>().pixelInset;
                r.x = r.x * scale;
                r.y = r.y * scale;
                r.width = r.width * scale;
                r.height = r.height * scale;
                t.GetComponent<GUITexture>().pixelInset = r;
            }
            else if(t.GetComponent<GUIText>())
            {
                Vector2 v = t.GetComponent<GUIText>().pixelOffset;
                v.x = v.x * scale;
                v.y = v.y * scale;
                t.GetComponent<GUIText>().pixelOffset = v;
                t.GetComponent<GUIText>().fontSize = Mathf.RoundToInt(t.GetComponent<GUIText>().fontSize * scale);
            }
        }

	}
	

}
