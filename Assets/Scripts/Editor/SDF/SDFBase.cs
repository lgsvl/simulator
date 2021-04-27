/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Xml.Linq;
using UnityEngine;
using System.Globalization;

public class SDFBase
{
    public readonly SDFDocument document;

    protected SDFBase(SDFDocument document)
    {
        this.document = document;
    }

    public static Vector3 ParseSDFVector(XElement element)
    {
        var parts = element.Value.Split(' ');
        return new Vector3(-Convert.ToSingle(parts[1], CultureInfo.InvariantCulture),
                            Convert.ToSingle(parts[2], CultureInfo.InvariantCulture),
                            Convert.ToSingle(parts[0], CultureInfo.InvariantCulture));
    }

    public static float ParseSingle(XElement element, float defaultValue)
    {
        try
        {
            if (element == null)
            {
                return defaultValue;
            }

            return Convert.ToSingle(element.Value, CultureInfo.InvariantCulture);
        }
        catch (Exception e)
        {
            Debug.Log("Could not parse as single: " + element.Value + " " + e);
            return defaultValue;
        }
    }
    public static (float, float) ParseXY(XElement element, (float, float) defaultValue)
    {
        try
        {
            if (element == null)
            {
                return defaultValue;
            }
            var parts = element.Value.Split(' ');
            return (Convert.ToSingle(parts[0], CultureInfo.InvariantCulture),
                    Convert.ToSingle(parts[1], CultureInfo.InvariantCulture));
        }
        catch (Exception e)
        {
            Debug.Log("Could not parse as XY: " + element.Value + " " + e);
            return defaultValue;
        }
    }
    public static bool ParseIntBool(XElement element, bool defaultValue)
    {
        try
        {
            if (element == null)
            {
                return defaultValue;
            }

            if (element.Value.Trim() == "true")
            {
                return true;
            }

            if (element.Value.Trim() == "false")
            {
                return false;
            }

            return Convert.ToInt32(element.Value) > 0;
        }
        catch (Exception e)
        {
            Debug.Log("Could not parse as single: " + element.Value + " " + e);
            return defaultValue;
        }

    }

    public static int ParseInt32(XElement element, int defaultValue)
    {
        try
        {
            if (element == null)
            {
                return defaultValue;
            }

            return Convert.ToInt32(element.Value);
        }
        catch (Exception e)
        {
            Debug.Log("Could not parse as single: " + element.Value + " " + e);
            return defaultValue;
        }
    }
    public static Pose ParsePose(XElement poseElement)
    {
        if (poseElement == null || string.IsNullOrWhiteSpace(poseElement.Value))
        {
            return Pose.identity;
        }

        string[] poseStr = poseElement.Value.Split(' ');
        return new Pose(new Vector3(-Convert.ToSingle(poseStr[1], CultureInfo.InvariantCulture),
                                     Convert.ToSingle(poseStr[2], CultureInfo.InvariantCulture),
                                     Convert.ToSingle(poseStr[0], CultureInfo.InvariantCulture)),
                        Quaternion.Euler(Convert.ToSingle(poseStr[4], CultureInfo.InvariantCulture) * Mathf.Rad2Deg,
                                        -Convert.ToSingle(poseStr[5], CultureInfo.InvariantCulture) * Mathf.Rad2Deg,
                                        -Convert.ToSingle(poseStr[3], CultureInfo.InvariantCulture) * Mathf.Rad2Deg));
    }

    public static Color ParseColor(XElement element)
    {
        var parts = element.Value.Split(' ');
        return new Color(Convert.ToSingle(parts[0], CultureInfo.InvariantCulture),
                         Convert.ToSingle(parts[1], CultureInfo.InvariantCulture),
                         Convert.ToSingle(parts[2], CultureInfo.InvariantCulture),
                         Convert.ToSingle(parts[3], CultureInfo.InvariantCulture));
    }

    public static void HandlePose(XElement poseElement, GameObject go)
    {
        // TBD : for SDF 1.7
        // var relative_to = poseElement.Attribute("relative_to")?.Value;
        var pose = ParsePose(poseElement);
        (go.transform.localPosition, go.transform.localRotation) = (pose.position, pose.rotation);
    }
    public static void ApplyPose(XElement element, GameObject go)
    {
        var poseNode = element.Element("pose");
        if (poseNode != null)
        {
            HandlePose(poseNode, go);
        }
    }
    public static Transform FindParentModel(Transform tr)
    {
        var go = tr.gameObject.GetComponentInParent<ModelHelper>();
        return go?.transform;
    }
}
