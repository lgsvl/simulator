/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;

public class WheelAlignment : MonoBehaviour {

    public WheelCollider CorrespondingCollider;
    public GameObject SlipPrefab;

    private float RotationValue = 0f;

    void Update()
    {
        RaycastHit raycastHit = default(RaycastHit);
        Vector3 vector = this.CorrespondingCollider.transform.TransformPoint(this.CorrespondingCollider.center);
        if(Physics.Raycast(vector, -this.CorrespondingCollider.transform.up, out raycastHit, this.CorrespondingCollider.suspensionDistance + this.CorrespondingCollider.radius))
        {
            this.transform.position = raycastHit.point + this.CorrespondingCollider.transform.up * this.CorrespondingCollider.radius;
        }
        else
        {
            this.transform.position = vector - this.CorrespondingCollider.transform.up * this.CorrespondingCollider.suspensionDistance;
        }
        this.transform.rotation = this.CorrespondingCollider.transform.rotation * Quaternion.Euler(this.RotationValue, this.CorrespondingCollider.steerAngle, (float)0);
        this.RotationValue += this.CorrespondingCollider.rpm * (float)6 * Time.deltaTime;
        WheelHit wheelHit = default(WheelHit);
        this.CorrespondingCollider.GetGroundHit(out wheelHit);
        if(Mathf.Abs(wheelHit.sidewaysSlip) > 14f && this.SlipPrefab)
        {
            UnityEngine.Object.Instantiate(this.SlipPrefab, wheelHit.point, Quaternion.identity);
        }
    }
}
