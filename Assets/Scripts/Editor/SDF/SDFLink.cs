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
        var linkObject = new GameObject(linkElement.Attribute("name")?.Value ?? "unnamed link");
        linkObject.transform.parent = parentModel.transform;
        linkObject.transform.localPosition = Vector3.zero;
        linkObject.transform.localRotation = Quaternion.identity;
        linkObject.isStatic = parentModel.isStatic;
        var body = linkObject.AddComponent<ArticulationBody>();
        body.enabled = !parentModel.isStatic;
        var helper = linkObject.AddComponent<LinkHelper>();

        foreach (var childElement in linkElement.Elements())
        {
            switch (childElement.Name.ToString())
            {
                case "visual":
                    HandleVisual(childElement, linkObject);
                    break;
                case "collision":
                    HandleCollision(childElement, linkObject);
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
                    body.mass = Convert.ToSingle(childElement.Value);
                    break;
                case "inertia":
                    const float minimumInertiaTensor = 1e-6f;

                    float iyy = Convert.ToSingle(childElement.Element("iyy").Value, CultureInfo.InvariantCulture);
                    float izz = Convert.ToSingle(childElement.Element("izz").Value, CultureInfo.InvariantCulture);
                    float ixx = Convert.ToSingle(childElement.Element("ixx").Value, CultureInfo.InvariantCulture);

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

        body.useGravity = ParseIntBool(childElement, true);
        if (body.useGravity)
        {
            body.gameObject.isStatic = false;
        }

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

    private void HandleCollision(XElement collisionElement, GameObject parentLink)
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

        if (mesh != null)
        {
            var scaleElem = mesh.Element("scale");

            if (scaleElem != null)
            {
                var parts = scaleElem.Value.Split(' ');
                scale = new Vector3(
                    Convert.ToSingle(parts[0], CultureInfo.InvariantCulture),
                    Convert.ToSingle(parts[1], CultureInfo.InvariantCulture),
                    Convert.ToSingle(parts[2], CultureInfo.InvariantCulture));
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
            var parts = box.Element("size")?.Value.Split(' ');
            collider.size = new Vector3(
                Convert.ToSingle(parts[0], CultureInfo.InvariantCulture),
                Convert.ToSingle(parts[1], CultureInfo.InvariantCulture),
                Convert.ToSingle(parts[2], CultureInfo.InvariantCulture));
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
            ApplyPose(geometryElement.Parent, collisionObject);
            collisionObject.transform.localScale = scale;
            collisionObject.transform.rotation *= rotation;
        }
    }

    private void HandleVisual(XElement visualElement, GameObject parentLink)
    {
        var geometry = visualElement.Element("geometry");
        if (geometry == null)
        {
            Debug.LogWarning("Visual missing geometry! " + visualElement);
            return;
        }

        GameObject visual = HandleVisualGeometry(geometry, parentLink);
        visual.isStatic = parentLink.isStatic;
        foreach (var childElement in visualElement.Elements())
        {
            switch (childElement.Name.ToString())
            {
                case "material":
                    HandleVisualMaterial(childElement, visual);
                    break;
                case "pose":
                    ApplyPose(visualElement, visual);
                    break;
                case "geometry":
                    break;
                default:
                    Debug.LogWarning("unhandled element: " + childElement + " within " + visualElement);
                    break;
            }
        }
    }

    private void HandleVisualMaterial(XElement materialElement, GameObject visual)
    {
        var renderer = visual.GetComponentInChildren<Renderer>();

        renderer.materials = renderer.sharedMaterials.Select(
            oldMat =>
            {
                var newMat = new Material(oldMat);
                foreach (var childElement in materialElement.Elements())
                {
                    switch (childElement.Name.ToString())
                    {
                        case "emissive":
                            newMat.SetColor("_EmissiveColor", ParseColor(childElement));
                            break;
                        case "diffuse":
                            var baseColor = ParseColor(childElement);
                            if (baseColor.a < 1.0f)
                            {
                                newMat.SetFloat("_SurfaceType", 1.0f);
                                newMat.SetFloat("_DstBlend", 10.0f);
                                newMat.SetFloat("_AlphaDstBlend", 10.0f);
                                newMat.SetFloat("_StencilRefDepth", 0);
                                newMat.SetFloat("_StencilRefGBuffer", 2);
                                newMat.SetFloat("_StencilRefMV", 32);
                                newMat.SetFloat("_ZTestDepthEqualForOpaque", 4);
                                newMat.SetFloat("_ZWrite", 0);

                                newMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                                newMat.EnableKeyword("_ENABLE_FOG_ON_TRANSPARENT");
                                newMat.renderQueue = 3000;
                                newMat.SetOverrideTag("RenderType", "Transparent");
                            }
                            newMat.SetColor("_BaseColor", baseColor);
                            break;
                        case "specular":
                            newMat.SetColor("_SpecularColor", ParseColor(childElement));
                            break;
                        default:
                            Debug.LogWarning("unhandled element: " + childElement + " within " + materialElement);
                            break;
                    }
                }
                document.CreateAsset(newMat, visual);

                return newMat;
            }
        ).ToArray();
    }


    private GameObject HandleVisualGeometry(XElement geometry, GameObject parentLink)
    {
        GameObject visual = null;
        var scale = Vector3.one;
        var rotation = Quaternion.identity;

        var box = geometry.Element("box");
        var cylinder = geometry.Element("cylinder");
        var mesh = geometry.Element("mesh");
        var plane = geometry.Element("plane");
        if (mesh != null)
        {
            var scaleElem = mesh.Element("scale");

            if (scaleElem != null)
            {
                var parts = scaleElem.Value.Split(' ');
                scale = new Vector3(
                    Convert.ToSingle(parts[0], CultureInfo.InvariantCulture),
                    Convert.ToSingle(parts[1], CultureInfo.InvariantCulture),
                    Convert.ToSingle(parts[2], CultureInfo.InvariantCulture));
            }

            var prefab = document.LoadAsset<GameObject>(mesh.Element("uri"));
            if (prefab != null)
            {
                visual = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                rotation = Quaternion.Euler(-90, 90, 0);
            }
        }
        else if (box != null)
        {
            visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject.DestroyImmediate(visual.GetComponent<Collider>());
            var parts = box.Element("size")?.Value.Split(' ');
            scale = new Vector3(
                Convert.ToSingle(parts[0], CultureInfo.InvariantCulture),
                 Convert.ToSingle(parts[1], CultureInfo.InvariantCulture),
                  Convert.ToSingle(parts[2], CultureInfo.InvariantCulture));
        }
        else if (cylinder != null)
        {
            visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            GameObject.DestroyImmediate(visual.GetComponent<Collider>());
            var radius = Convert.ToSingle(cylinder.Element("radius")?.Value, CultureInfo.InvariantCulture);
            var length = Convert.ToSingle(cylinder.Element("length")?.Value, CultureInfo.InvariantCulture);
            scale = new Vector3(radius * 2.0f, length * 0.5f, radius * 2.0f);
        }
        else if (plane != null)
        {
            visual = GameObject.CreatePrimitive(PrimitiveType.Plane);
            GameObject.DestroyImmediate(visual.GetComponent<Collider>());
            var normal = ParseSDFVector(plane.Element("normal"));
            var (scaleX, scaleZ) = ParseXY(plane.Element("size"), (1.0f, 1.0f));
            scale = new Vector3(scaleX, 1.0f, scaleZ);
            visual.transform.rotation = Quaternion.LookRotation(normal != Vector3.forward ? Vector3.forward : Vector3.left, normal);
        }
        else
        {
            Debug.Log("unhandled visual type in " + geometry);
        }

        visual.transform.SetParent(parentLink.transform, false);
        visual.transform.localScale = scale;
        visual.transform.rotation *= rotation;
        return visual;
    }
}
