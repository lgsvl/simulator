/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using System.Collections.Generic;

namespace Simulator.Map
{
    public struct NavPose
    {
        // Unity: Right/Up/Forward
        // ROS: Forward/Left/Up

        public Vector3 position;
        public Quaternion orientation;
    }

    public class NavOrigin : MonoBehaviour
    {
        public float OriginX;
        public float OriginY;
        public float Rotation;  // Not supported yet

        public static NavOrigin SetNavOrigin(Vector3 position, Quaternion rotation, Vector3 offset)
        {
            var origin = FindObjectOfType<NavOrigin>();
            if (origin != null)
            {
                Destroy(origin.gameObject);
            }

            origin = new GameObject("NavOrigin").AddComponent<NavOrigin>();
            origin.transform.SetPositionAndRotation(position, rotation);
            origin.OriginX = offset.x;
            origin.OriginY = offset.y;
            origin.Rotation = offset.z;

            return origin;
        }

        public static NavOrigin Find()
        {
            var origin = FindObjectOfType<NavOrigin>();
            if (origin == null)
            {
                Debug.LogWarning("Map is missing NavOrigin component! Adding temporary NavOrigin. Please add to scene and set origin for Navigation2 stack");
                origin = SetNavOrigin(Vector3.zero, Quaternion.identity, Vector3.zero);
            }

            return origin;
        }

        public NavPose GetNavPose(Transform pose)
        {
            var nav_pose = new NavPose();

            var position = transform.InverseTransformPoint(pose.position);
            position.Set(position.z, -position.x, position.y);
            position.x += OriginX;
            position.y += OriginY;
            nav_pose.position = position;

            var heading = Vector3.SignedAngle(transform.forward, pose.transform.forward, -transform.up);
            Quaternion orientation = Quaternion.Euler(0, 0, heading);
            nav_pose.orientation = orientation;

            return nav_pose;
        }

        public Transform FromNavPose(Vector3 position, Quaternion orientation)
        {
            var point = new GameObject();

            position.x -= OriginX;
            position.y -= OriginY;
            position.Set(-position.y, position.z, position.x);
            point.transform.position = transform.TransformPoint(position);

            orientation.Set(orientation.y, -orientation.z, -orientation.x, orientation.w);
            var heading = orientation.eulerAngles.y - transform.rotation.eulerAngles.y;
            point.transform.rotation = Quaternion.Euler(0, heading, 0);

            var pose = point.transform;
            Destroy(point);

            return pose;
        }
    }
}
