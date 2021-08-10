/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Xml.Linq;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Globalization;


class SDFLink : SDFParserBase
{
    public SDFLink(SDFDocument document)
    {
        this.document = document;
    }

    readonly SDFDocument document;

    public override GameObject Parse(XElement linkElement, GameObject parentModel)
    {
        var linkObject = CreateChildObject(linkElement, parentModel);
        var body = linkObject.AddComponent<ArticulationBody>();
        body.enabled = !parentModel.isStatic;
        var helper = linkObject.AddComponent<LinkHelper>();

        foreach (var childElement in linkElement.Elements())
        {
            try
            {
                switch (childElement.Name.ToString())
                {
                    case "visual":
                        new SDFVisual(document).Parse(childElement, linkObject);
                        break;
                    case "collision":
                        new SDFCollision(document).Parse(childElement, linkObject);
                        break;
                    case "pose":
                        HandlePose(childElement, linkObject);
                        break;
                    case "kinematic":
                        HandleKinematic(childElement, linkObject);
                        break;
                    case "gravity":
                        HandleGravity(childElement, linkObject);
                        break;
                    case "inertial":
                        HandleInertial(childElement, linkObject);
                        break;
                    case "self_collide":
                        HandleSelfCollide(childElement, linkObject);
                        break;
                    case "enable_wind": //unsupported
                        break;
                    default:
                        Debug.LogWarning("unhandled element: " + childElement + " within " + linkElement);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Error parsing from link " + linkElement + ": " + ex);
            }
        }
        return linkObject;
    }

    private void HandleSelfCollide(XElement childElement, GameObject linkObject)
    {
        var helper = linkObject.GetComponent<LinkHelper>();
        helper.self_collide = ParseIntBool(childElement, false);
    }

    private void HandleInertial(XElement element, GameObject parentLink)
    {
        var body = parentLink.GetComponent<ArticulationBody>();
        if (body == null)
        {
            Debug.LogWarning("HandleInertia: no body on " + parentLink);
            return;
        }

        foreach (var childElement in element.Elements())
        {
            switch (childElement.Name.ToString())
            {
                case "mass":
                    body.mass = ParseSingle(childElement, 1.0f);
                    break;
                case "inertia":
                    const float minimumInertiaTensor = 1e-6f;

                    float iyy = ParseSingle(childElement.Element("iyy"), 0.0f);
                    float izz = ParseSingle(childElement.Element("izz"), 0.0f);
                    float ixx = ParseSingle(childElement.Element("ixx"), 0.0f);

                    body.inertiaTensor = new Vector3(
                        Mathf.Max(minimumInertiaTensor, iyy),
                        Mathf.Max(minimumInertiaTensor, izz),
                        Mathf.Max(minimumInertiaTensor, ixx));

                    body.inertiaTensorRotation = Quaternion.identity;
                    //FIXME what about ixy ixz iyz
                    break;
                case "pose":
                    body.centerOfMass = ParseSDFVector(childElement);
                    break;
                default:
                    Debug.LogWarning("unhandled element: " + childElement + " within " + element);
                    break;
            }
        }
    }

    private void HandleGravity(XElement childElement, GameObject go)
    {
        var body = go.GetComponent<ArticulationBody>();
        if (body == null)
        {
            Debug.LogWarning("no Body on " + go);
            return;
        }

        bool gravity = ParseIntBool(childElement, true);
        body.useGravity = gravity; // Unity bug? this setter does not seem to do anything
        if (body.useGravity != gravity)
            Debug.Log(body.name + " gravity " + gravity + " did not work!");
    }

    private void HandleKinematic(XElement childElement, GameObject go)
    {
        var body = go.GetComponent<ArticulationBody>();
        if (body == null)
        {
            Debug.LogWarning("no Body on " + go);
            return;
        }

        bool isKinematic = ParseIntBool(childElement, true);
        if (isKinematic)
        {
            go.isStatic = false;
        }
    }

}
