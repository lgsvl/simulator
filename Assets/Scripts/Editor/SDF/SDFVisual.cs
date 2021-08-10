/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Xml.Linq;
using System.Linq;
using UnityEngine;
using UnityEditor;

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

        foreach (var childElement in geometry.Elements())
        {
            var scale = Vector3.one;
            var rotation = Quaternion.identity;
            visual = null;
            switch (childElement.Name.ToString())
            {
                case "mesh":
                    var scaleElem = childElement.Element("scale");
                    if (scaleElem != null)
                    {
                        scale = ParseSDFSize(scaleElem);
                    }

                    var uri = childElement.Element("uri").Value;
                    var prefab = document.LoadAsset<GameObject>(uri);
                    if (prefab != null)
                    {
                        visual = PrefabUtility.InstantiatePrefab(prefab, parentLink.transform) as GameObject;
                        rotation = Quaternion.Euler(-90, 90, 0);
                        if (uri.ToLower().EndsWith(".stl"))
                        {
                            // STL import package has weird behaviour wrt right handed coordinate system
                            scale.Scale(new Vector3(-1, 1, 1));
                        }
                    }
                    break;
                case "box":
                    visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    scale = ParseSDFSize(childElement.Element("size"));
                    break;
                case "sphere":
                    visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    var r = ParseSingle(childElement.Element("radius"), 1.0f);
                    scale = new Vector3(r, r, r);
                    break;
                case "cylinder":
                    visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    var radius = ParseSingle(childElement.Element("radius"), 1.0f);
                    var length = ParseSingle(childElement.Element("length"), 1.0f);
                    scale = new Vector3(radius * 2.0f, length * 0.5f, radius * 2.0f);
                    break;
                case "plane":
                    visual = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    var normal = ParseSDFVector(childElement.Element("normal"));
                    var (scaleX, scaleZ) = ParseXY(childElement.Element("size"), (1.0f, 1.0f));
                    scale = new Vector3(scaleX, 1.0f, scaleZ);
                    rotation = Quaternion.LookRotation(normal != Vector3.forward ? Vector3.forward : Vector3.left, normal);
                    break;
                default:
                    Debug.Log("unhandled visual type " + childElement + " in " + geometry);
                    break;
            }

            if (visual != null)
            {
                foreach (var collider in visual.GetComponentsInChildren<Collider>())
                {
                    GameObject.DestroyImmediate(collider);
                }
                visual.isStatic = parentLink.isStatic;
                visual.transform.SetParent(parentLink.transform, false);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localScale = scale;
                visual.transform.localRotation = rotation;
            }
        }
        return visual;
    }
}