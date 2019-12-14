/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Simulator.Editor.Tools
{
    public class EditorTools : EditorWindow
    {
        [MenuItem("Simulator/Editor Tools", false, 141)]
        public static void Open()
        {
            var window = GetWindow(typeof(EditorTools), false, "Editor Tools");
            window.Show();
        }

        public enum EditorToolsType
        {
            Prefab,
            Component,
        }
        public static EditorToolsType ToolType { get; set; } = EditorToolsType.Prefab;

        public enum EditorPrefabTools
        {
            Add,
            Replace,
            ZeroParent
        }
        public static EditorPrefabTools PrefabTool { get; set; } = EditorPrefabTools.Add;

        public enum EditorComponentTools
        {
            Add,
            RandomRotation,
            Remove
        }
        public static EditorComponentTools ComponentTool { get; set; } = EditorComponentTools.Add;

        public static bool UseMeshCenter = false;

        // style
        private GUIStyle titleLabelStyle;
        private GUIStyle subtitleLabelStyle;
        private GUIStyle buttonStyle;
        private Color nonProColor;
        private GUIContent[] editorToolsTypeContent;
        private GUIContent[] editorPrefabToolsContent;
        private GUIContent[] editorComponentToolsContent;

        // prefab
        private GameObject PrefabToAdd = null;
        private GameObject PrefabToReplace = null;

        // component
        private MonoScript ScriptToAdd = null;

        private void Awake()
        {
            editorToolsTypeContent = new GUIContent[] {
            new GUIContent { text = "Prefab", tooltip = "Prefab mode"},
            new GUIContent { text = "Component", tooltip = "Component mode"},
            };

            editorPrefabToolsContent = new GUIContent[] {
            new GUIContent { text = "Add", tooltip = "Add prefab to prefab"},
            new GUIContent { text = "Replace", tooltip = "Replace object with new prefab"},
            new GUIContent { text = "ZeroParent", tooltip = "Zero parent transform to child objects"},
            };

            editorComponentToolsContent = new GUIContent[] {
            new GUIContent { text = "Add", tooltip = "Add component to object"},
            new GUIContent { text = "RandomRotation", tooltip = "Apply random rotation to object"},
            new GUIContent { text = "Remove", tooltip = "Remove component type"},
            };

            ToolType = EditorToolsType.Prefab;
            PrefabTool = EditorPrefabTools.ZeroParent;
            ComponentTool = EditorComponentTools.RandomRotation;
        }

        private void OnGUI()
        {
            titleLabelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14 };
            subtitleLabelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 10 };
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.onNormal.textColor = Color.white;
            nonProColor = new Color(0.75f, 0.75f, 0.75f);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Type", titleLabelStyle, GUILayout.ExpandWidth(true));
            GUILayout.Space(5);
            if (!EditorGUIUtility.isProSkin)
                GUI.backgroundColor = nonProColor;
            ToolType = (EditorToolsType)GUILayout.SelectionGrid((int)ToolType, editorToolsTypeContent, 2, buttonStyle);
            if (!EditorGUIUtility.isProSkin)
                GUI.backgroundColor = Color.white;
            GUILayout.Space(5);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Space(10);

            switch (ToolType)
            {
                case EditorToolsType.Prefab:
                    EditorGUILayout.LabelField("Prefab Tools", titleLabelStyle, GUILayout.ExpandWidth(true));
                    GUILayout.Space(5);
                    if (!EditorGUIUtility.isProSkin)
                        GUI.backgroundColor = nonProColor;
                    PrefabTool = (EditorPrefabTools)GUILayout.SelectionGrid((int)PrefabTool, editorPrefabToolsContent, 2, buttonStyle);
                    if (!EditorGUIUtility.isProSkin)
                        GUI.backgroundColor = Color.white;
                    GUILayout.Space(5);
                    EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                    GUILayout.Space(10);

                    switch (PrefabTool)
                    {
                        case EditorPrefabTools.Add:
                            EditorGUILayout.LabelField("Add Tool", subtitleLabelStyle, GUILayout.ExpandWidth(true));
                            GUILayout.Space(10);
                            EditorGUILayout.HelpBox("Add prefab and select the object to attach\nUseMeshCenter true = renderer.bounds.center\nUseMeshCenter false = pivot center\nPress Run", MessageType.None, true);
                            PrefabToAdd = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Prefab to add", "This is the prefab to add"), PrefabToAdd, typeof(GameObject), true);
                            GUILayout.Space(5);
                            UseMeshCenter = EditorGUILayout.Toggle("Use mesh center", UseMeshCenter);
                            GUILayout.Space(5);
                            if (GUILayout.Button(new GUIContent("Run", "Run add prefab to prefab root")))
                                AddPrefabTool();
                            break;
                        case EditorPrefabTools.Replace:
                            EditorGUILayout.LabelField("Replace Tool", subtitleLabelStyle, GUILayout.ExpandWidth(true));
                            GUILayout.Space(10);
                            EditorGUILayout.HelpBox("Replace selection with prefab\nPress Run", MessageType.None, true);
                            PrefabToReplace = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Prefab to replace", "This is the prefab to replace selection with"), PrefabToReplace, typeof(GameObject), true);
                            GUILayout.Space(5);
                            if (GUILayout.Button(new GUIContent("Run", "Run replace prefab")))
                                ReplacePrefabTool();
                            break;
                        case EditorPrefabTools.ZeroParent:
                            EditorGUILayout.LabelField("Zero Parent Tool", subtitleLabelStyle, GUILayout.ExpandWidth(true));
                            GUILayout.Space(10);
                            EditorGUILayout.HelpBox("Select child of prefab root to center prefab to\nPress Run", MessageType.None, true);
                            if (GUILayout.Button(new GUIContent("Run", "Run zero parent to child object")))
                                ZeroParentTool();
                            break;
                    }

                    break;
                case EditorToolsType.Component:
                    EditorGUILayout.LabelField("Component Tools", titleLabelStyle, GUILayout.ExpandWidth(true));
                    GUILayout.Space(5);
                    if (!EditorGUIUtility.isProSkin)
                        GUI.backgroundColor = nonProColor;
                    ComponentTool = (EditorComponentTools)GUILayout.SelectionGrid((int)ComponentTool, editorComponentToolsContent, 2, buttonStyle);
                    if (!EditorGUIUtility.isProSkin)
                        GUI.backgroundColor = Color.white;
                    GUILayout.Space(5);
                    EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                    GUILayout.Space(10);

                    switch (ComponentTool)
                    {
                        case EditorComponentTools.Add:
                            EditorGUILayout.LabelField("Add Tool", subtitleLabelStyle, GUILayout.ExpandWidth(true));
                            GUILayout.Space(10);
                            EditorGUILayout.HelpBox("Assign script to add and select child to add to\nPress Run", MessageType.None, true);
                            ScriptToAdd = (MonoScript)EditorGUILayout.ObjectField(new GUIContent("Script to add", "This is the script to add"), ScriptToAdd, typeof(MonoScript), true);
                            GUILayout.Space(5);
                            if (GUILayout.Button(new GUIContent("Run", "Run add component to prefab")))
                                AddComponentTool();
                            break;
                        case EditorComponentTools.RandomRotation:
                            EditorGUILayout.LabelField("Random Rotation Tool", subtitleLabelStyle, GUILayout.ExpandWidth(true));
                            GUILayout.Space(10);

                            break;
                        case EditorComponentTools.Remove:
                            EditorGUILayout.LabelField("Remove Tool", subtitleLabelStyle, GUILayout.ExpandWidth(true));
                            GUILayout.Space(10);

                            break;
                    }
                    break;
            }
        }

        private void ZeroParentTool()
        {
            var selection = Selection.activeTransform;
            if (selection == null)
            {
                Debug.LogError("Must have an object selected in hierarchy");
                return;
            }

            var childName = selection.name;
            var root = PrefabUtility.GetOutermostPrefabInstanceRoot(selection);
            var asset = PrefabUtility.GetCorrespondingObjectFromOriginalSource(root);
            var assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
            if (assetPath == null)
            {
                Debug.LogError("Can't find prefab asset to change");
                return;
            }

            List<Transform> orig = new List<Transform>();
            List<Vector3> origPos = new List<Vector3>();
            List<Quaternion> origRot = new List<Quaternion>();

            GameObject[] allObjects = (GameObject[])FindObjectsOfType(typeof(GameObject));
            foreach (GameObject go in allObjects)
            {
                if (PrefabUtility.GetCorrespondingObjectFromOriginalSource(go) == asset)
                {
                    foreach (Transform child in go.transform)
                    {
                        if (child.name == childName)
                        {
                            origPos.Add(child.position);
                            origRot.Add(go.transform.rotation);
                            orig.Add(go.transform);
                        }
                    }
                }
            }

            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.OutermostRoot, InteractionMode.UserAction);
            List<Transform> childs = new List<Transform>(root.transform.childCount);
            List<Vector3> childPos = new List<Vector3>();
            List<Quaternion> childRot = new List<Quaternion>();
            List<Vector3> childScl = new List<Vector3>();
            foreach (Transform child in root.transform)
            {
                childs.Add(child);
            }
            foreach (Transform child in childs)
            {
                child.SetParent(root.transform.parent, true);
            }
            root.transform.position = selection.position;
            foreach (var child in childs)
            {
                child.SetParent(root.transform, true);
            }
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, assetPath, InteractionMode.UserAction);

            foreach (Transform child in root.transform)
            {
                childPos.Add(child.localPosition);
                childRot.Add(child.localRotation);
                childScl.Add(child.localScale);
            }

            for (int i = 0; i < orig.Count; i++)
            {
                orig[i].transform.position = origPos[i];
                orig[i].transform.rotation = origRot[i];
                childs.Clear();
                foreach (Transform child in orig[i])
                {
                    childs.Add(child);
                }
                for (int j = 0; j < childs.Count; j++)
                {
                    childs[j].localPosition = childPos[j];
                    childs[j].localRotation = childRot[j];
                    childs[j].localScale = childScl[j];
                }
                Debug.Log(orig[i].name, orig[i]);
            }

            SceneView.RepaintAll();
            // TODO needs better undo
        }

        private void AddPrefabTool()
        {
            var selection = Selection.activeTransform;
            if (selection == null)
            {
                Debug.LogError("Must have an object selected in hierarchy");
                return;
            }

            var root = PrefabUtility.GetOutermostPrefabInstanceRoot(selection);
            var assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);

            if (PrefabToAdd == null)
            {
                Debug.LogError("Need prefab to add");
                return;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(PrefabToAdd);
            go.transform.position = UseMeshCenter ? selection.GetComponent<Renderer>().bounds.center : selection.position;
            go.transform.SetParent(selection);
            PrefabUtility.ApplyAddedGameObject(go, assetPath, InteractionMode.AutomatedAction);

            PrefabToAdd = null;
            Debug.Log("Added prefab to root prefab");
            // TODO needs undo, prefab apply won't revert
        }

        private void ReplacePrefabTool()
        {
            var selection = Selection.gameObjects;
            if (selection == null)
            {
                Debug.LogError("Must have an object selected in hierarchy");
                return;
            }

            if (PrefabToReplace == null)
            {
                Debug.LogError("Need prefab to replace");
                return;
            }

            for (int i = 0; i < selection.Length; i++)
            {
                var root = selection[i].transform.parent;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(PrefabToReplace);
                go.transform.position = selection[i].transform.position;
                go.transform.rotation = selection[i].transform.rotation;
                go.transform.SetParent(root);
                DestroyImmediate(selection[i]);
            }

            PrefabToReplace = null;
            Debug.Log("Replaced selection with prefab");
            // TODO needs undo, prefab apply won't revert
        }

        private void AddComponentTool()
        {
            var selection = Selection.activeTransform;
            if (selection == null)
            {
                Debug.LogError("Must have an object selected in hierarchy");
                return;
            }
            var root = PrefabUtility.GetOutermostPrefabInstanceRoot(selection);

            var assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
            if (assetPath == null)
            {
                Debug.LogError("Can't find prefab asset to change");
                return;
            }

            if (ScriptToAdd == null)
            {
                Debug.LogError("Need script to add");
                return;
            }
            var scriptType = ScriptToAdd.GetClass();

            Component added = selection.gameObject.AddComponent(scriptType);
            PrefabUtility.ApplyAddedComponent(added, assetPath, InteractionMode.AutomatedAction);

            ScriptToAdd = null;
            Debug.Log($"Added component {scriptType}");
            // TODO needs undo, prefab apply won't revert
        }
    }
}
