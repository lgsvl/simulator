using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

public class RaycastProjector : EditorWindow
{
    public List<GameObject> projectingGOs = new List<GameObject>();
    public string[] axes = { "+X", "-X", "+Y", "-Y", "+Z", "-Z" };
    public int axis = 0;
    public float raycastDist = 1.0f;
    public float offset = 0.005f;
    public enum Axis
    {
        None, X, Y, Z,
    }
    Axis objAxis;

    private void Awake()
    {
        axis = 3;
        objAxis = Axis.Y;
    }

    [MenuItem("Window/RaycastProjector")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(RaycastProjector));
    }

	void OnGUI () {
        GUIStyle leftAlignment = new GUIStyle();
        leftAlignment.alignment = TextAnchor.LowerLeft;

        GUILayout.Label("Objects To Project", EditorStyles.boldLabel);
        if (GUILayout.Button("Load Objects"))
        {
            projectingGOs = Selection.gameObjects.ToList();
            Debug.Log(projectingGOs.Count + " Objects have been loaded for projection");
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label("Projection Direction", EditorStyles.boldLabel);
        axis = EditorGUILayout.Popup(axis, axes, leftAlignment);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Raycast Distance", EditorStyles.boldLabel);
        raycastDist = EditorGUILayout.FloatField(raycastDist);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        objAxis = (Axis)EditorGUILayout.EnumPopup("Object axis to match hit normal with", objAxis);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Offset", EditorStyles.boldLabel);
        offset = EditorGUILayout.FloatField(offset);
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        if (GUILayout.Button("Project"))
        {
            foreach (var go in projectingGOs)
            {
                Debug.Log("Projecting " + go.name);

                RaycastHit hitInfo;
                Ray ray;
                if (axes[axis].Contains("-"))
                    ray = new Ray(go.transform.position, Vector3.down);
                else
                    ray = new Ray(go.transform.position, Vector3.up);

                if (Physics.Raycast(ray, out hitInfo, raycastDist))
                {
                    Quaternion rot = Quaternion.identity;
                    switch (objAxis)
                    {
                        case Axis.X:
                            rot = Quaternion.FromToRotation(go.transform.right, hitInfo.normal);
                            break;
                        case Axis.Y:
                            rot = Quaternion.FromToRotation(go.transform.up, hitInfo.normal);
                            break;
                        case Axis.Z:
                            rot = Quaternion.FromToRotation(go.transform.forward, hitInfo.normal);
                            break;
                        case Axis.None:
                            break;
                    }

                    if (axes[axis].Contains("X"))
                    {                        
                        Undo.RecordObject(go.transform, "undo " + go.name);
                        go.transform.position = new Vector3(hitInfo.point.x + offset, go.transform.position.y, go.transform.position.z);
                    }
                    else if (axes[axis].Contains("Y"))
                    {
                        Undo.RecordObject(go.transform, "undo " + go.name);
                        go.transform.position = new Vector3(go.transform.position.x, hitInfo.point.y + offset, go.transform.position.z);
                    }
                    else if (axes[axis].Contains("Z"))
                    {
                        Undo.RecordObject(go.transform, "undo " + go.name);
                        go.transform.position = new Vector3(go.transform.position.x, go.transform.position.y, hitInfo.point.z + offset);
                    }

                    go.transform.rotation = rot * go.transform.rotation;
                }
            }
        }
    }
}
