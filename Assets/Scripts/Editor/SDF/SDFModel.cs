/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */
using System;
using System.Xml.Linq;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class SDFModel : SDFParserBase
{
    public SDFModel(SDFDocument document)
    {
        this.document = document;
    }

    public readonly SDFDocument document;

    Dictionary<(GameObject, string), Transform> originalChildObjects = new Dictionary<(GameObject, string), Transform>();

    public override GameObject Parse(XElement modelElement, GameObject parent)
    {
        GameObject modelObject = CreateChildObject(modelElement, parent);
        var ab = modelObject.AddComponent<ArticulationBody>();
        var modelHelper = modelObject.AddComponent<ModelHelper>();
        modelObject.isStatic = false;

        // assume static is inherited from parent model
        if (parent.GetComponent<ModelHelper>())
        {
            modelObject.isStatic = parent.isStatic;
        }

        modelObject.isStatic = ParseIntBool(modelElement.Element("static"), modelObject.isStatic);
        ab.enabled = !modelObject.isStatic;

        foreach (var childElement in modelElement.Elements("link"))
        {
            var link = new SDFLink(document);
            modelHelper.links.Add(link.Parse(childElement, modelObject));
        }

        foreach (var childElement in modelElement.Elements("include"))
        {
            var included = document.HandleInclude(childElement, modelObject);
            var model = included.GetComponent<ModelHelper>();
            if (model != null) modelHelper.models.Add(model);
        }

        foreach (var childElement in modelElement.Elements())
        {
            switch (childElement.Name.ToString())
            {
                case "pose":
                    HandlePose(childElement, modelObject);
                    break;
                case "allow_auto_disable":
                    HandleAutoDisable(childElement, modelObject);
                    break;
                case "model":
                    var model = new SDFModel(document);
                    modelHelper.models.Add(model.Parse(childElement, modelObject).GetComponent<ModelHelper>());
                    break;
                case "self_collide":
                    modelHelper.self_collide = ParseIntBool(childElement, false);
                    break;
                case "joint":
                    HandleJoint(childElement, modelObject);
                    break;
                case "plugin":
                    HandlePlugin(childElement, modelObject);
                    break;
                case "include": //handled above
                case "link": // handled above
                case "static": // handled above
                case "enable_wind": //unsupported
                    break;
                default:
                    Debug.LogWarning("unhandled element: " + childElement + " within " + modelElement);
                    break;
            }
        }
        return modelObject;
    }

    private void HandlePlugin(XElement pluginElement, GameObject parentObject)
    {
        Debug.Log("adding stub "+parentObject.name+" "+pluginElement.ToString());
        var stub = parentObject.AddComponent<SDFPluginStub>();
        stub.data = pluginElement.ToString();
    }

    //FIXME
    private void HandleAutoDisable(XElement childElement, GameObject go)
    {
        var body = go.GetComponentInChildren<ArticulationBody>();
        if (body == null)
        {
            Debug.LogWarning("no Body on " + go);
            return;
        }
        if (ParseIntBool(childElement, true) == false)
        {
            // this does not seem to do anything
            // body.sleepThreshold = -100.0f;
        }
    }

    private void HandleJoint(XElement jointElement, GameObject modelObject)
    {
        var typeName = jointElement.Attribute("type").Value;
        var parentPath = jointElement.Element("parent").Value;
        var childPath = jointElement.Element("child").Value;
        var jointParent = LookupChild(modelObject, parentPath);
        var jointChild = LookupChild(modelObject, childPath);

        if (jointParent == null)
        {
            Debug.LogWarning("could not find parent " + parentPath);
            return;
        }

        if (jointChild == null)
        {
            Debug.LogWarning("could not find child " + childPath);
            return;
        }

        var childModel = FindParentModel(jointChild);
        var parentModel = FindParentModel(jointParent);

        if (!jointChild.IsChildOf(jointParent))
        {
            if (childModel != parentModel)
            {
                //                Debug.Log($"{parentPath} / {childPath} not in parent/child relationship, next parent model is {childModel}, reparenting to {jointParent}");
                childModel.SetParent(jointParent);
            }
            else
            {
                //                Debug.Log($"{parentPath} / {childPath} not in parent/child relationship, link within model, reparenting link {jointChild} to link {jointParent}");
                jointChild.SetParent(jointParent);
            }
        }

        var disabler = jointChild.gameObject.AddComponent<JointSelfCollisionDisabler>();
        disabler.jointParent = jointParent;

        var childBody = jointChild.GetComponent<ArticulationBody>();
        childBody.linearDamping = 0.05f;
        childBody.angularDamping = 0.05f;

        var anchorPose = new Pose();

        if (childModel == parentModel)
        {
            anchorPose.position = jointChild.position;
            anchorPose.rotation = jointChild.rotation;
        }
        else
        {
            anchorPose.position = childModel.position;
            anchorPose.rotation = childModel.rotation;
        }

        var jointPose = ParsePose(jointElement.Element("pose"));
        anchorPose.position += jointPose.position;
        anchorPose.rotation *= jointPose.rotation;

        childBody.anchorPosition = Vector3.zero;
        childBody.anchorRotation = Quaternion.identity;
        childBody.parentAnchorPosition = anchorPose.position;
        childBody.parentAnchorRotation = anchorPose.rotation;

        switch (typeName)
        {
            case "revolute":
                MakeRevolute(childBody, jointElement);
                break;
            case "prismatic":
                MakePrismatic(childBody, jointElement);
                break;
            case "fixed":
                MakeFixed(childBody);
                break;
            case "ball":
                MakeBall(childBody);
                break;
            default:
                Debug.LogWarning("unsupported joint type " + typeName);
                return;
        }
    }

    public void MakeFixed(ArticulationBody body)
    {
        body.jointType = ArticulationJointType.FixedJoint;
        body.jointFriction = 0;
    }

    public void MakeBall(ArticulationBody body)
    {
        body.jointType = ArticulationJointType.SphericalJoint;
        body.swingYLock = ArticulationDofLock.FreeMotion;
        body.swingZLock = ArticulationDofLock.FreeMotion;
        body.twistLock = ArticulationDofLock.FreeMotion;
    }

    public void MakePrismatic(ArticulationBody body, XElement jointElement)
    {
        var axisElement = jointElement.Element("axis");
        body.jointType = ArticulationJointType.PrismaticJoint;
        var pose = ParsePose(jointElement.Element("pose"));

        body.parentAnchorRotation *= pose.rotation;

        var dynamics = axisElement?.Element("dynamics");
        body.jointFriction = ParseSingle(dynamics?.Element("friction"), 0.0f);

        var drive = new ArticulationDrive();

        var limit = axisElement?.Element("limit");
        bool limited = limit != null;
        drive.lowerLimit = ParseSingle(limit.Element("lower"), -1e16f);
        drive.upperLimit = ParseSingle(limit.Element("upper"), 1e16f);
        drive.forceLimit = ParseSingle(jointElement.Element("physics")?.Element("ode")?.Element("max_force"), float.MaxValue);

        drive.stiffness = ParseSingle(dynamics?.Element("spring_stiffness"), 1e+08f);
        drive.damping = ParseSingle(dynamics?.Element("damping"), drive.damping);

        var jointAxis = ParseSDFVector(axisElement.Element("xyz"));

        if (jointAxis.Equals(Vector3.right) || jointAxis.Equals(Vector3.left))
        {
            if (jointAxis.Equals(Vector3.left))
            {
                ReverseArticulationBodyAxis(body, Vector3.forward * 180);
            }

            body.xDrive = drive;
            body.linearLockX = limited ? ArticulationDofLock.LimitedMotion : ArticulationDofLock.FreeMotion;
            body.linearLockY = ArticulationDofLock.LockedMotion;
            body.linearLockZ = ArticulationDofLock.LockedMotion;
        }
        else if (jointAxis.Equals(Vector3.up) || jointAxis.Equals(Vector3.down))
        {
            if (jointAxis.Equals(Vector3.down))
            {
                ReverseArticulationBodyAxis(body, Vector3.right * 180);
            }

            body.yDrive = drive;
            body.linearLockX = ArticulationDofLock.LockedMotion;
            body.linearLockY = limited ? ArticulationDofLock.LimitedMotion : ArticulationDofLock.FreeMotion;
            body.linearLockZ = ArticulationDofLock.LockedMotion;
        }
        else if (jointAxis.Equals(Vector3.forward) || jointAxis.Equals(Vector3.back))
        {
            if (jointAxis.Equals(Vector3.back))
            {
                ReverseArticulationBodyAxis(body, Vector3.up * 180);
            }

            body.zDrive = drive;
            body.linearLockX = ArticulationDofLock.LockedMotion;
            body.linearLockY = ArticulationDofLock.LockedMotion;
            body.linearLockZ = limited ? ArticulationDofLock.LimitedMotion : ArticulationDofLock.FreeMotion;
        }
    }

    public void MakeRevolute(ArticulationBody body, XElement jointElement)
    {
        var axisElement = jointElement.Element("axis");
        body.jointType = ArticulationJointType.SphericalJoint; // TODO was spherical?

        var dynamics = axisElement?.Element("dynamics");
        body.jointFriction = ParseSingle(dynamics?.Element("friction"), 0.0f);
        var drive = new ArticulationDrive();

        var limit = axisElement?.Element("limit");
        bool limited = limit != null;
        SetRevoluteArticulationDriveLimit(limit, drive);

        drive.forceLimit = float.MaxValue;

        var jointAxis = ParseSDFVector(axisElement.Element("xyz"));

        if (jointAxis.Equals(Vector3.right) || jointAxis.Equals(Vector3.left))
        {
            if (jointAxis.Equals(Vector3.left))
            {
                ReverseArticulationBodyAxis(body, Vector3.forward * 180);
            }

            body.xDrive = drive;
            body.twistLock = limited ? ArticulationDofLock.LimitedMotion : ArticulationDofLock.FreeMotion;
            body.swingYLock = ArticulationDofLock.LockedMotion;
            body.swingZLock = ArticulationDofLock.LockedMotion;
        }
        else if (jointAxis.Equals(Vector3.up) || jointAxis.Equals(Vector3.down))
        {
            if (jointAxis.Equals(Vector3.down))
            {
                ReverseArticulationBodyAxis(body, Vector3.right * 180);
            }

            body.yDrive = drive;
            body.twistLock = ArticulationDofLock.LockedMotion;
            body.swingYLock = limited ? ArticulationDofLock.LimitedMotion : ArticulationDofLock.FreeMotion;
            body.swingZLock = ArticulationDofLock.LockedMotion;
        }
        else if (jointAxis.Equals(Vector3.forward) || jointAxis.Equals(Vector3.back))
        {
            if (jointAxis.Equals(Vector3.back))
            {
                ReverseArticulationBodyAxis(body, Vector3.up * 180);
            }

            body.zDrive = drive;
            body.twistLock = ArticulationDofLock.LockedMotion;
            body.swingYLock = ArticulationDofLock.LockedMotion;
            body.swingZLock = limited ? ArticulationDofLock.LimitedMotion : ArticulationDofLock.FreeMotion;
        }
    }

    private void SetRevoluteArticulationDriveLimit(XElement limitElement, ArticulationDrive drive)
    {
        drive.upperLimit = ParseSingle(limitElement?.Element("upper"), 1e+16f) * Mathf.Rad2Deg;
        drive.lowerLimit = ParseSingle(limitElement?.Element("lower"), -1e+16f) * Mathf.Rad2Deg;
    }

    private void ReverseArticulationBodyAxis(in ArticulationBody body, in Vector3 euler)
    {
        body.anchorRotation *= Quaternion.Euler(euler);
        body.parentAnchorRotation *= Quaternion.Euler(euler);
    }

    private Transform LookupChild(GameObject parent, string path)
    {
        if (!originalChildObjects.TryGetValue((parent, path), out Transform found))
        {
            var pathSplit = Regex.Split(path, "::");
            found = LookupChild(parent.transform, pathSplit, 0);
            originalChildObjects.Add((parent, path), found);
        }

        return found;
    }

    private Transform LookupChild(Transform parent, string[] path, int index)
    {
        var child = parent.Find(path[index]);
        if (index == path.Length - 1)
        {
            return child;
        }
        else
        {
            return LookupChild(child, path, index + 1);
        }
    }
}
