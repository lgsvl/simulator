/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;

public class TrafWheels : MonoBehaviour
{
    public Transform fl;
    public Transform fr;
    public Transform rl;
    public Transform rr;
    public float radius = 0.32f;

    private TrafAIMotor motor;
    private CarAIController carAIController;
    private float theta = 0f;
    private float newX = 0f;
    private float lastX = 0f;

    void Awake()
	{
		motor = GetComponent<TrafAIMotor>();
        carAIController = GetComponent<CarAIController>();
	}

	void Update ()
    {
        if (carAIController == null) return;

        if (carAIController.inRenderRange)
        {
            theta = motor.currentSpeed * Time.deltaTime / radius;
            newX = lastX + theta * Mathf.Rad2Deg;
            lastX = newX;
            if (lastX > 360)
                lastX -= 360;

            fl.localRotation = Quaternion.Euler(newX, motor.currentTurn, 0);
            fr.localRotation = Quaternion.Euler(newX, motor.currentTurn, 0);
            rl.localRotation = Quaternion.Euler(newX, 0, 0);
            rr.localRotation = Quaternion.Euler(newX, 0, 0);
        }
    }
}
