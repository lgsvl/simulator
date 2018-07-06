/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;

public class CamFixTo : MonoBehaviour
{
    public Transform fixTo;

    void LateUpdate()
    {
        // TODO: position should by Lerp'd if distance is more than a couple of units,
        // however Lerping position when starting play outside of the select screen
        // absolutely freaks out, for some reason I can't identify -Eric

        transform.position = fixTo.transform.position;
        if (Quaternion.Angle(transform.rotation, fixTo.transform.rotation) > 5)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, fixTo.transform.rotation, Time.deltaTime * 4);
        }
        else
        {
            transform.rotation = fixTo.transform.rotation;
        }
    }
}
