/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Linq;
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
        GameObject collisionObject = CreateChildObject(collisionElement, parentLink);
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

        bool createSubObjects = geometryElement.Elements().Count() > 1;

        foreach (var childElement in geometryElement.Elements())
        {
            switch (childElement.Name.ToString())
            {
                case "mesh":
                    {
                        var geometryObject = CreateChildObject(geometryElement, collisionObject);
                        var uri = childElement.Element("uri").Value;
                        var meshAssets = document.LoadSubAsset<Mesh>(uri);
                        foreach (var meshAsset in meshAssets)
                        {
                            var meshCollider = geometryObject.AddComponent<MeshCollider>();
                            meshCollider.sharedMesh = meshAsset;
                            meshCollider.convex = !collisionObject.isStatic;
                        }
                        var scaleElem = childElement.Element("scale");
                        var scale = Vector3.one;
                        if (scaleElem != null)
                        {
                            scale = ParseSDFSize(scaleElem);
                        }
                        if (uri.ToLower().EndsWith(".stl"))
                        {
                            // STL import package has weird behaviour wrt right handed coordinate system
                            scale.Scale(new Vector3(-1, 1, 1));
                        }
                        geometryObject.transform.localScale = scale;
                        geometryObject.transform.localRotation = Quaternion.Euler(-90, 90, 0);
                        break;
                    }
                case "box":
                    {
                        var boxCollider = collisionObject.AddComponent<BoxCollider>();
                        boxCollider.size = ParseSDFSize(childElement.Element("size"));
                        break;
                    }
                case "sphere":
                    {
                        var sphereCollider = collisionObject.AddComponent<SphereCollider>();
                        sphereCollider.radius = ParseSingle(childElement.Element("radius"), 1.0f);
                        break;
                    }
                case "cylinder":
                    {
                        float length = ParseSingle(childElement.Element("length"), 1.0f);
                        float radius = ParseSingle(childElement.Element("radius"), 1.0f);
                        if (length < radius * SDFDocument.cylinderUseMeshRadiusLengthFactor)
                        {
                            var geometryObject = CreateChildObject(geometryElement, collisionObject);
                            var meshCollider = geometryObject.AddComponent<MeshCollider>();
                            var helper = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                            meshCollider.sharedMesh = helper.GetComponent<MeshFilter>().sharedMesh;
                            GameObject.DestroyImmediate(helper);
                            meshCollider.convex = !collisionObject.isStatic;
                            geometryObject.transform.localScale = new Vector3(2 * radius, length * 0.5f, 2 * radius);
                        }
                        else
                        {
                            // FIXME a cylinder is not a capsule actually
                            var capsuleCollider = collisionObject.AddComponent<CapsuleCollider>();
                            capsuleCollider.height = length;
                            capsuleCollider.radius = radius;
                        }
                        break;
                    }
                case "plane":
                    {
                        var geometryObject = CreateChildObject(geometryElement, collisionObject);
                        var helper = GameObject.CreatePrimitive(PrimitiveType.Plane);
                        var meshCollider = geometryObject.AddComponent<MeshCollider>();
                        meshCollider.sharedMesh = helper.GetComponent<MeshFilter>().sharedMesh;
                        GameObject.DestroyImmediate(helper);

                        var normal = ParseSDFVector(childElement.Element("normal"));
                        var (scaleX, scaleZ) = ParseXY(childElement.Element("size"), (1.0f, 1.0f));
                        geometryObject.transform.localScale = new Vector3(scaleX, 1.0f, scaleZ);
                        geometryObject.transform.localRotation = Quaternion.LookRotation(normal != Vector3.forward ? Vector3.forward : Vector3.left, normal);
                        break;
                    }
                default:
                    Debug.Log("unhandled collision type " + childElement + " in " + geometryElement);
                    break;
            }
        }


    }
}