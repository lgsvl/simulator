/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;

public abstract class BaseAlphaFade : MonoBehaviour {

    public float fadeSpeed = 1f;
    public float targetAlpha = 1f;

    public void SetTarget(float newAlpha)
    {
        targetAlpha = newAlpha;
    }

    public void SetStraight(float newAlpha) {
        targetAlpha = newAlpha;
        currentAlpha = newAlpha;
    }

    protected float currentAlpha;

    protected virtual void Start()
    {
        currentAlpha = targetAlpha;
    }

    protected virtual void Update()
    {
        if(currentAlpha != targetAlpha)
        {
            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
        }
    }
}

