
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

class SDFVisual : SDFParserBase
{
    readonly SDFDocument document;
    public SDFVisual(SDFDocument document)
    {
        this.document = document;
    }

    public override GameObject Parse(XElement visualElement, GameObject parentLink)
    {
        var geometry = visualElement.Element("geometry");
        if (geometry == null)
        {
            Debug.LogWarning("Visual missing geometry! " + visualElement);
            return null;
        }

        GameObject visual = HandleVisualGeometry(geometry, parentLink);
        if (visual == null) return null;
        visual.isStatic = parentLink.isStatic;
        foreach (var childElement in visualElement.Elements())
        {
            switch (childElement.Name.ToString())
            {
                case "material":
                    HandleVisualMaterial(childElement, visual);
                    break;
                case "pose":
                    HandlePose(childElement, visual);
                    break;
                case "geometry":
                    break;
                default:
                    Debug.LogWarning("unhandled element: " + childElement + " within " + visualElement);
                    break;
            }
        }
        return visual;
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
        var sphere = geometry.Element("sphere");
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
            scale = ParseSDFSize(box.Element("size"));
        }
        else if (sphere != null)
        {
            visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            GameObject.DestroyImmediate(visual.GetComponent<Collider>());
            var r = ParseSingle(sphere.Element("radius"), 1.0f);
            scale = new Vector3(r, r, r);
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

        if (visual != null)
        {
            visual.transform.SetParent(parentLink.transform, false);
            visual.transform.localScale = scale;
            visual.transform.localRotation *= rotation;
        }
        return visual;
    }

}