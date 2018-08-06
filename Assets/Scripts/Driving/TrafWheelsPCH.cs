/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;

public class TrafWheelsPCH : MonoBehaviour
{

    private Transform fl;
    private Transform fr;
    private Transform rl;
    private Transform rr;
    private TrafPCH motor;

    public float radius = 0.32f;


    private bool visible = false;
    private OnBecameVisiblePass visPasser;

    void Awake()
    {
        fl = transform.Find("Wheels/PivotFL");
        fr = transform.Find("Wheels/PivotFR");
        rl = transform.Find("Wheels/PivotRL");
        rr = transform.Find("Wheels/PivotRR");
        motor = GetComponent<TrafPCH>();

        var rend = GetComponentInChildren<Renderer>();
        visPasser = rend.gameObject.AddComponent<OnBecameVisiblePass>();
        visPasser.onVisbilityChange = OnVisibilityChange;

    }

    float lastX = 0f;

    // Update is called once per frame
    void Update()
    {
        if (visible)
        {
            float theta = motor.currentSpeed * Time.deltaTime / radius;
            float newX = lastX + theta * Mathf.Rad2Deg;
            lastX = newX;
            if (lastX > 360)
                lastX -= 360;

            fl.localRotation = Quaternion.Euler(newX, motor.currentTurn, 0);
            fr.localRotation = Quaternion.Euler(newX, motor.currentTurn, 0);
            rl.localRotation = Quaternion.Euler(newX, 0, 0);
            rr.localRotation = Quaternion.Euler(newX, 0, 0);
        }
    }

    void OnVisibilityChange(bool v)
    {
        visible = v;
    }
}
