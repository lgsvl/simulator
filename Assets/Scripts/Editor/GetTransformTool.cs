/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


using UnityEditor;
using UnityEngine;

public class GetTransformWindow : EditorWindow
{
	static string parentFrameId;
    static string childFrameId;
    static Vector3 translation;
    static Quaternion rotation;
    static Vector3 eulerAngles;
    static bool result;

    public GameObject parent = null;
    public GameObject child = null;

	[MenuItem("Window/Get Transform Tool")]
    static void Init()
    {
        UnityEditor.EditorWindow window = GetWindow(typeof(GetTransformWindow));
        window.Show();
    }

    static bool GetTransform(GameObject parent, GameObject child)
    {
        if (parent == null || child == null)
        {
            return false;
        }
        else
        {
            Debug.Log(parent.name);
            Debug.Log(child.name);

            // var t0 = parent.transform;
            // var t1 = parent.transform;

            Vector3 p0 = parent.transform.localPosition;
            Vector3 p1 = child.transform.localPosition;

            Vector3 e0 = parent.transform.localEulerAngles;
            Vector3 e1 = child.transform.localEulerAngles;

            Vector3 p_diff = p1 - p0;
            Vector3 e_diff = e1 - e0;

            if (child.name == "CaptureCamera")
            {
                e_diff = Quaternion.AngleAxis(90.0f, parent.transform.right) * e_diff;
            }

            if (child.name == "RadarSensor")
            {
                p_diff = Quaternion.AngleAxis(-90.0f, parent.transform.right) * p_diff;
            }

            translation = new Vector3(
                p_diff.x,
                p_diff.z,
                p_diff.y
            );

            e_diff = -e_diff;
            eulerAngles = new Vector3(
                e_diff.x,
                e_diff.z,
                e_diff.y
            );

            // Quaternion r0 = parent.transform.localRotation;
            // Quaternion r1 = child.transform.localRotation;
            // rotation = r1 * Quaternion.Inverse(r0);
            // eulerAngles = rotation.eulerAngles;

            rotation = Quaternion.Euler(eulerAngles);

            return true;
        }
    }

    void OnGUI()
    {
        parent = (GameObject)EditorGUILayout.ObjectField("Parent frame:", parent, typeof(GameObject), true);
        parentFrameId = EditorGUILayout.TextField("Parent frame ID: ", parentFrameId);
        
        child = (GameObject)EditorGUILayout.ObjectField("Child frame:", child, typeof(GameObject), true);
        childFrameId = EditorGUILayout.TextField("Child frame ID: ", childFrameId);
        
        if (GUILayout.Button("Get Transform"))
            result = GetTransform(parent, child);
        
        GUILayout.Label("Result:", EditorStyles.boldLabel);
        if (result)
        {
            GUILayout.Label(string.Format("Euler Angles (degrees): [x: {0}, y: {1}, z: {2}]", eulerAngles.x, eulerAngles.z, eulerAngles.y));
            string format = "header:\n"
                            + "  seq: 0\n"
                            + "  stamp:\n"
                            + "    secs: 0\n"
                            + "    nsecs: 0\n"
                            + "  frame_id: {0}\n"
                            + "child_frame_id: {1}\n"
                            + "transform:\n"
                            + "  translation:\n"
                            + "    x: {2}\n"
                            + "    y: {3}\n"
                            + "    z: {4}\n"
                            + "  rotation:\n"
                            + "    x: {5}\n"
                            + "    y: {6}\n"
                            + "    z: {7}\n"
                            + "    w: {8}";
            string tf_msg = string.Format(format, parentFrameId, childFrameId, translation.x, translation.z, translation.y, rotation.x, rotation.z, rotation.y, rotation.w);
            EditorGUILayout.TextArea(tf_msg);
        } else {
            GUILayout.Label("Please select parent and child objects to get a transform.");
        }
    }
}
