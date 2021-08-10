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
using System.IO;

public class SDFDocument
{
    public string Version
    {
        get => doc.Element("sdf")?.Attribute("version")?.Value;
    }

    public SDFParserBase RootElement { get; set; } = null;

    private readonly XDocument doc;
    public string ModelPath;
    public string FileName { get; }
    static public float cylinderUseMeshRadiusLengthFactor = 4.0f;
    private static readonly Regex uriRegExp = new Regex(@"(\w+)://(.+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public List<(GameObject, SDFModel)> Models = new List<(GameObject, SDFModel)>();
    public PhysicMaterial DefaultPhysicMaterial { get; } = new PhysicMaterial()
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
        FileName = fileName;
        ModelPath = modelPath;
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
                var fileName = Path.Combine(ModelPath, path, "model.sdf");
                if (File.Exists(fileName))
                {
                    var loader = new SDFDocument(fileName, ModelPath);
                    var includedObject = loader.Load(parent);
                    if (typeof(SDFModel).IsAssignableFrom(loader.RootElement.GetType()))
                        Models.Add((includedObject, (SDFModel)loader.RootElement));
                    return includedObject;
                }
            }
        }
        Debug.LogError($"could not load from uri {uriNode} (model path {ModelPath})");
        return null;
    }

    public T LoadAsset<T>(string uri) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(uri))
            return null;

        var res = uriRegExp.Match(uri);
        if (!res.Success)
            return null;

        var schema = res.Groups[1].Captures[0].Value;
        var path = res.Groups[2].Captures[0].Value;

        var prefabPath = Path.Combine(ModelPath, path);
        if (File.Exists(prefabPath))
        {
            var meshAsset = AssetDatabase.LoadAssetAtPath<T>(prefabPath);
            if (meshAsset != null)
            {
                return meshAsset;
            }
        }
        Debug.LogError("could not load mesh from " + prefabPath);
        return null;
    }


    public List<T> LoadSubAsset<T>(string uri) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(uri))
            return null;

        var res = uriRegExp.Match(uri);
        if (!res.Success)
            return null;

        var schema = res.Groups[1].Captures[0].Value;
        var path = res.Groups[2].Captures[0].Value;

        var prefabPath = Path.Combine(ModelPath, path);
        if (File.Exists(prefabPath))
        {
            var meshAssets = new List<T>();
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(prefabPath);
            foreach (var asset in allAssets)
            {
                if (typeof(T).IsAssignableFrom(asset.GetType())) meshAssets.Add((T)asset);
            }
            if (meshAssets.Count == 0)
            {
                Debug.LogError("could not load mesh from " + prefabPath);
            }
            return meshAssets;
        }
        return null;
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

        var parentModel = SDFParserBase.FindParentModel(owner.transform);
        string name = owner.name;
        for (var t = owner.transform.parent; t != parentModel; t = t.parent)
        {
            name = t.name + "_" + name;
        }

        var outPath = Path.Combine(Path.GetDirectoryName(FileName), "GeneratedAssets");
        if (!Directory.Exists(outPath))
        {
            Directory.CreateDirectory(outPath);
        }
        var path = AssetDatabase.GenerateUniqueAssetPath($"{outPath}/sdfgen_{name}.{ext}");
        AssetDatabase.CreateAsset(asset, path);
    }

    public void LoadWorld(GameObject rootObject, Camera mainCamera = null)
    {
        if (rootObject == null)
        {
            rootObject = new GameObject("unnamed");
        }

        Models.Clear();

        var worldNode = doc.Element("sdf").Element("world");
        rootObject.name = "Models";

        var cameraNode = worldNode.Element("gui").Element("camera");
        if (cameraNode != null)
        {
            mainCamera.name = cameraNode.Attribute("name")?.Value ?? "unnamed";
            var pose = cameraNode.Element("pose");
            if (pose != null)
            {
                SDFParserBase.HandlePose(pose, mainCamera.gameObject);
            }
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
                    HandleInclude(child, rootObject);
                    break;
                case "gui":
                    break;
                case "model":
                    HandleModel(child, rootObject);
                    break;
                default:
                    Debug.LogWarning($"Unhandled Element: {child}");
                    break;
            }
        }
    }

    public GameObject Load(GameObject parent)
    {
        var sdfNode = doc.Element("sdf");
        foreach (var child in sdfNode.Elements())
        {
            switch (child.Name.ToString())
            {
                case "model":
                    return HandleModel(child, parent);
                case "light":
                    return HandleLight(child, parent);
                default:
                    Debug.LogWarning($"Unhandled Element: {child}");
                    break;
            }
        }
        return null;
    }
    public GameObject HandleModel(XElement modelElement, GameObject parent)
    {
        var model = new SDFModel(this);
        RootElement = model;
        return model.Parse(modelElement, parent);
    }

    public GameObject HandleLight(XElement lightElement, GameObject parent)
    {
        var light = new SDFLight(this);
        RootElement = light;
        return light.Parse(lightElement, parent);

    }

    public GameObject HandleInclude(XElement includeElement, GameObject go)
    {
        var includedObject = LoadURI(includeElement.Element("uri"), go);
        if (includedObject == null) return null;

        foreach (var childElement in includeElement.Elements())
        {
            switch (childElement.Name.ToString())
            {
                case "pose":
                    SDFParserBase.HandlePose(childElement, includedObject);
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


