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
            // To get the difference C between A and B,
            // C = A * Quaternion.Inverse(B);
            // To add the difference to D,
            // D = C * D;

            Vector3 pos0 = parent.transform.localPosition;
            Vector3 pos1 = child.transform.localPosition;

            Quaternion rot0 = parent.transform.localRotation;
            Quaternion rot1 = child.transform.localRotation;

            Vector3 pos_diff = pos1 - pos0;

            if (parent.name == "LidarSensor" && child.name == "CaptureCamera")
            {
                rot0 = Quaternion.AngleAxis(-90.0f, Vector3.right) * rot0;
            }
            else if (parent.name == "CaptureCamera" && child.name == "RadarSensor")
            {
                rot0 = Quaternion.AngleAxis(90.0f, Vector3.right) * rot0;
                rot0 = Quaternion.AngleAxis(90.0f, Vector3.up) * rot0;
                pos_diff = Quaternion.AngleAxis(-90.0f, Vector3.right) * pos_diff;
            } 
            else if (parent.name == "LidarSensor" && child.name == "RadarSensor")
            {
                rot0 = Quaternion.AngleAxis(90.0f, Vector3.up) * rot0;
            } 
            else if (parent.name == "ImuSensor" && child.name == "RadarSensor")
            {
                pos_diff = Quaternion.AngleAxis(90.0f, Vector3.up) * pos_diff;
            }
            else if (parent.name == "GpsSensor" && child.name == "CaptureCamera")
            {
                rot0 = Quaternion.AngleAxis(-90.0f, Vector3.right) * rot0;
                rot0 = Quaternion.AngleAxis(90.0f, Vector3.forward) * rot0;
                pos_diff = Quaternion.AngleAxis(90.0f, Vector3.up) * pos_diff;
            }
            
            Vector3 rot_diff = (rot0 * Quaternion.Inverse(rot1)).eulerAngles;

            translation = new Vector3(
                pos_diff.x,
                pos_diff.z,
                pos_diff.y
            );

            eulerAngles = new Vector3(
                rot_diff.x,
                rot_diff.z,
                rot_diff.y
            );

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
            GUILayout.Label(string.Format("Euler Angles (degrees): [x: {0}, y: {1}, z: {2}]", eulerAngles.x, eulerAngles.y, eulerAngles.z));
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
            string tf_msg = string.Format(format, parentFrameId, childFrameId, translation.x, translation.y, translation.z, rotation.x, rotation.y, rotation.z, rotation.w);
            EditorGUILayout.TextArea(tf_msg);
        } else {
            GUILayout.Label("Please select parent and child objects to get a transform.");
        }
    }
}
