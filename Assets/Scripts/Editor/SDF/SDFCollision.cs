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

class SDFCollision : SDFParserBase
{
    readonly SDFDocument document;
    public SDFCollision(SDFDocument document)
    {
        this.document = document;
    }

    public override GameObject Parse(XElement collisionElement, GameObject parentLink)
    {
        // FIXME we should be able to collapse collision objects into a component of the parent object if there is no transform on it
        GameObject collisionObject = new GameObject(collisionElement.Attribute("name")?.Value ?? "unnamed collision");
        collisionObject.transform.parent = parentLink.transform;
        collisionObject.transform.localPosition = Vector3.zero;
        collisionObject.transform.localRotation = Quaternion.identity;
        collisionObject.isStatic = parentLink.isStatic;
        PhysicMaterial material = document.DefaultPhysicMaterial;

        foreach (var childElement in collisionElement.Elements())
        {
            switch (childElement.Name.ToString())
            {
                case "geometry":
                    HandleCollisionGeometry(childElement, collisionObject);
                    break;
                case "surface":
                    material = HandleSurface(childElement, collisionObject);
                    break;
                case "pose":
                    HandlePose(childElement, collisionObject);
                    break;
                default:
                    Debug.LogWarning("unhandled element: " + childElement + " within " + collisionElement);
                    break;
            }
        }

        var colliders = collisionObject.GetComponentsInChildren<Collider>();
        foreach (var collider in colliders)
        {
            collider.material = material;
        }
        return collisionObject;
    }

    private PhysicMaterial HandleSurface(XElement surfaceElement, GameObject collisionObject)
    {
        var material = new PhysicMaterial()
        {
            bounciness = ParseSingle(surfaceElement.Element("bounce")?.Element("restitution_coefficient"), 0.0f),
            frictionCombine = PhysicMaterialCombine.Average,
            bounceCombine = PhysicMaterialCombine.Average
        };

        var frictionElement = surfaceElement.Element("friction")?.Element("ode");
        if (frictionElement != null)
        {
            foreach (var childElement in frictionElement.Elements())
            {
                switch (childElement.Name.ToString())
                {
                    case "mu":
                        material.staticFriction = ParseSingle(childElement, 0.0f);
                        material.dynamicFriction = material.staticFriction * 0.7f;
                        break;
                    default:
                        Debug.LogWarning("unhandled element: " + childElement + " within " + surfaceElement);
                        break;
                }
            }
        }

        document.CreateAsset(material, collisionObject);
        return material;
    }

    private void HandleCollisionGeometry(XElement geometryElement, GameObject collisionObject)
    {
        var scale = Vector3.one;
        var rotation = Quaternion.identity;

        var box = geometryElement.Element("box");
        var cylinder = geometryElement.Element("cylinder");
        var mesh = geometryElement.Element("mesh");
        var plane = geometryElement.Element("plane");
        var sphere = geometryElement.Element("sphere");

        if (mesh != null)
        {
            var scaleElem = mesh.Element("scale");

            if (scaleElem != null)
            {
                scale = ParseSDFSize(scaleElem);
            }

            var meshAsset = document.LoadAsset<Mesh>(mesh.Element("uri"));
            if (meshAsset != null)
            {
                var collider = collisionObject.AddComponent<MeshCollider>();
                collider.sharedMesh = meshAsset;
                collider.convex = !collisionObject.isStatic;
                rotation = Quaternion.Euler(-90, 90, 0);
            }
        }
        else if (box != null)
        {
            var collider = collisionObject.AddComponent<BoxCollider>();
            collider.size = ParseSDFSize(box.Element("size"));
        }
        else if (sphere != null)
        {
            var collider = collisionObject.AddComponent<SphereCollider>();
            collider.radius = ParseSingle(sphere.Element("radius"), 1.0f);
        }
        else if (cylinder != null)
        {
            float length = Convert.ToSingle(cylinder.Element("length")?.Value, CultureInfo.InvariantCulture);
            float radius = Convert.ToSingle(cylinder.Element("radius")?.Value, CultureInfo.InvariantCulture);
            if (length < radius * SDFDocument.cylinderUseMeshRadiusLengthFactor)
            {
                var collider = collisionObject.AddComponent<MeshCollider>();
                var helper = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                collider.sharedMesh = helper.GetComponent<MeshFilter>().sharedMesh;
                GameObject.DestroyImmediate(helper);
                collider.convex = !collisionObject.isStatic;
                scale = new Vector3(2 * radius, length * 0.5f, 2 * radius);
            }
            else
            {
                // FIXME a cylinder is not a capsule actually
                var collider = collisionObject.AddComponent<CapsuleCollider>();
                collider.height = length;
                collider.radius = radius;
            }
        }
        else if (plane != null)
        {
            var helper = GameObject.CreatePrimitive(PrimitiveType.Plane);
            var collider = collisionObject.AddComponent<MeshCollider>();
            collider.sharedMesh = helper.GetComponent<MeshFilter>().sharedMesh;
            GameObject.DestroyImmediate(helper);

            var normal = ParseSDFVector(plane.Element("normal"));
            var (scaleX, scaleZ) = ParseXY(plane.Element("size"), (1.0f, 1.0f));
            scale = new Vector3(scaleX, 1.0f, scaleZ);
            rotation = Quaternion.LookRotation(normal != Vector3.forward ? Vector3.forward : Vector3.left, normal);
        }
        else
        {
            Debug.Log("unhandled collision type in " + geometryElement);
        }

        if (collisionObject.GetComponent<Collider>() != null)
        {
            collisionObject.transform.localScale = scale;
            collisionObject.transform.localRotation *= rotation;
        }
    }
}