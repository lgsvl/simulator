/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Xml.Linq;
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class SDFDocument
{
    public string Version
    {
        get => doc.Element("sdf")?.Attribute("version")?.Value;
    }

    private readonly XDocument doc;
    public readonly string modelPath;
    public readonly string fileName;
    static public float cylinderUseMeshRadiusLengthFactor = 4.0f;
    private static readonly Regex uriRegExp = new Regex(@"(\w+)://(.+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public List<GameObject> models = new List<GameObject>();

    public PhysicMaterial defaultPhysicMaterial = new PhysicMaterial()
    {
        name = "Stone",
        dynamicFriction = 0.6f,
        staticFriction = 0.6f,
        bounciness = 0.0f,
        frictionCombine = PhysicMaterialCombine.Average,
        bounceCombine = PhysicMaterialCombine.Average,
    };

    public SDFDocument(string fileName, string modelPath)
    {
        doc = XDocument.Load(fileName);
        this.fileName = fileName;
        this.modelPath = modelPath;
    }

    public GameObject LoadURI(XElement uriNode, GameObject parent)
    {
        var res = uriRegExp.Match(uriNode.Value);
        if (res.Success)
        {
            var schema = res.Groups[1].Captures[0].Value;
            var path = res.Groups[2].Captures[0].Value;
            if (schema == "model")
            {
                var loader = new SDFDocument($"{modelPath}/{path}/model.sdf", modelPath);
                return loader.LoadModel(parent).gameObject;
            }
        }
        return null;
    }

    public T LoadAsset<T>(XElement uriElement) where T : UnityEngine.Object
    {
        var uri = uriElement.Value;
        if (string.IsNullOrEmpty(uri))
            return null;

        var res = uriRegExp.Match(uri);
        if (!res.Success)
            return null;

        var schema = res.Groups[1].Captures[0].Value;
        var path = res.Groups[2].Captures[0].Value;

        var prefabPath = modelPath + "/" + path;
        var meshAsset = AssetDatabase.LoadAssetAtPath<T>(prefabPath);
        if (!meshAsset)
        {
            Debug.LogError("could not load mesh from " + prefabPath);
            return null;
        }
        return meshAsset;
    }

    public void CreateAsset<T>(T asset, GameObject owner) where T : UnityEngine.Object
    {
        string ext = "";
        if (asset.GetType() == typeof(Mesh))
        {
            ext = "obj";
        }
        else if (asset.GetType() == typeof(Material))
        {
            ext = "material";
        }
        else if (asset.GetType() == typeof(PhysicMaterial))
        {
            ext = "physicMaterial";
        }
        else if (asset.GetType() == typeof(GameObject))
        {
            ext = "prefab";
        }
        else
        {
            throw new NotImplementedException();
        }

        var parentModel = SDFBase.FindParentModel(owner.transform);
        string name = owner.name;
        for (var t = owner.transform.parent; t != parentModel; t = t.parent)
        {
            name = t.name + "_" + name;
        }

        var path = AssetDatabase.GenerateUniqueAssetPath($"{modelPath}/sdfgen_{name}.{ext}");
        AssetDatabase.CreateAsset(asset, path);
    }

    public void LoadWorld(GameObject rootObject, Camera mainCamera = null)
    {
        if (rootObject == null)
        {
            rootObject = new GameObject("unnamed");
        }

        models.Clear();

        var worldNode = doc.Element("sdf").Element("world");
        rootObject.name = worldNode.Attribute("name")?.Value ?? "unnamed";

        var cameraNode = worldNode.Element("gui").Element("camera");
        if (cameraNode != null)
        {
            mainCamera.name = cameraNode.Attribute("name")?.Value ?? "unnamed";
            SDFBase.ApplyPose(cameraNode, mainCamera.gameObject);
        }
        else
        {
            Debug.Log("not found: camera");
        }
        foreach (var child in worldNode.Elements())
        {
            switch (child.Name.ToString())
            {
                case "include":
                    models.Add(HandleInclude(child, rootObject));
                    break;
                case "gui":
                    break;
                default:
                    Debug.LogWarning($"Unhandled Element: {child}");
                    break;
            }
        }
    }

    public ModelHelper LoadModel(GameObject parent)
    {
        var modelElement = doc.Element("sdf").Element("model");
        var model = new SDFModel(this);
        return model.Parse(modelElement, parent);
    }

    public GameObject HandleInclude(XElement includeElement, GameObject go)
    {
        var includedObject = LoadURI(includeElement.Element("uri"), go);

        foreach (var childElement in includeElement.Elements())
        {
            switch (childElement.Name.ToString())
            {
                case "pose":
                    SDFBase.HandlePose(childElement, includedObject);
                    break;
                case "name":
                    includedObject.name = childElement.Value;
                    break;
                case "uri":
                    //
                    break;
                default:
                    Debug.LogWarning("unhandled element: " + childElement + " within " + includeElement);
                    break;
            }
        }
        return includedObject;
    }
}


