/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum IntersectionType { LEFT, STRAIGHT, RIGHT }

public class IntersectionPath : TrafficPath {

    public const float userCarDistance = 20f;
    public const float possibilityLimit = 3f;

    //paths that we have to give way to
    public List<IntersectionPath> giveWayTo;

    public bool debug = false;

    protected override void Awake()
    {
        base.Awake();

        WayPoint last = null;
        WayPoint last2 = null;
        foreach(var wp in waypoints)
        {
            wp.intersection = this;
            last2 = last;
            last = wp;
            if(last != null)
                last.forwardVector = wp.GetPosition() - last.GetPosition();
        }
        last.forwardVector = last2.forwardVector;
    }

    public int carsInside;

    public override IEnumerator<WayPoint> GetEnumerator()
    {
        foreach(var wp in waypoints)
        {
            yield return wp;
        }
    }

    public bool IsClear()
    {
        if(giveWayTo.Count(p => p.carsInside > 0) > 0)
            return false;
        else
        {
            //check for user car position
            //var carPos = TrackController.Instance.car.transform;
            var carPos = transform; //Need to be fixed later
            if (Vector3.Distance(transform.position, carPos.position) > userCarDistance)
                return true;

            var intersectionWps = giveWayTo.SelectMany(g => g.waypoints);
            
            foreach(var wp in intersectionWps) {

                float angularDistance1 = Vector3.Angle(wp.GetPosition() - carPos.position, carPos.forward);
                float angularDistance = Vector3.Angle(wp.forwardVector, carPos.forward) *0.2f +  angularDistance1 * 0.6f;
                float linearDistance = Vector3.Distance(carPos.position, wp.GetPosition());
                float carSpeed = carPos.GetComponent<Rigidbody>().velocity.magnitude * Mathf.Cos(angularDistance1);
                float distRatio = carSpeed * 1.1f - linearDistance * 0.1f - angularDistance * 0.8f;
                if(distRatio > possibilityLimit)
                    return true;
            }

            return false;
        }
    }

    public void RegisterCar()
    {
        carsInside++;
    }

    public void DeRegisterCar()
    {
        carsInside--;
    }
}
