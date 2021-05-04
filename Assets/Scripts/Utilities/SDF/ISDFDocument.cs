using System.Xml.Linq;
using UnityEngine;

public interface ISDFDocument
{
    string Version { get; }
    void CreateAsset<T>(T asset, GameObject owner) where T : UnityEngine.Object;
    GameObject HandleInclude(XElement includeElement, GameObject go);
    T LoadAsset<T>(XElement uriElement) where T : UnityEngine.Object;
    GameObject LoadModel(GameObject parent);
    GameObject LoadURI(XElement uriNode, GameObject parent);
    void LoadWorld(GameObject rootObject, Camera mainCamera = null);
    SDFParserBase RootElement { get; }
    string FileName { get; }
    PhysicMaterial DefaultPhysicMaterial { get; }
}
