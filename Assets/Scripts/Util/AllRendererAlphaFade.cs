/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AllRendererAlphaFade : RendererAlphaFade {

    private Renderer[] renderers;

	
	protected override void Start () {
        base.Start();
        renderers = GetComponentsInChildren<Renderer>();
	}

    protected override void UpdateObject()
    {
        foreach(var r in renderers)
        {
            Color c = r.material.color;
            c.a = currentAlpha;
            r.material.color = c;
        }
    }
}
