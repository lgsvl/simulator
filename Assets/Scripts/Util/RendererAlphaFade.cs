/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;

public class RendererAlphaFade : MonoBehaviour {

    public float fadeSpeed = 1f;
    public float targetAlpha = 1f;

    private Renderer rend;
    protected float currentAlpha;

    protected virtual void Start()
    {
        currentAlpha = targetAlpha;
        rend = GetComponent<Renderer>();
    }

    public void SetTarget(float newAlpha)
    {
        targetAlpha = newAlpha;
    }

    public void SetStraight(float newAlpha)
    {
        targetAlpha = newAlpha;
        currentAlpha = newAlpha;
    }


    protected virtual void Update()
    {
        if(currentAlpha != targetAlpha) {
            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime );
        }
        UpdateObject();
    }

    protected virtual void UpdateObject()
    {
        Color c = rend.material.color;
        c.a = currentAlpha;
        rend.material.color = c;
    }
    
}
