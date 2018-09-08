/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */


using UnityEditor;
using UnityEngine;

public class GetIntrinsicsWindow : EditorWindow
{
	public GameObject obj = null;

    static private double fx;
    static private double fy;
    static private double cx;
    static private double cy;
    static private float width;
    static private float height;
    static bool result = false;

	[MenuItem("Window/Get Intrinsics Tool")]
    static void Init()
    {
        UnityEditor.EditorWindow window = GetWindow(typeof(GetIntrinsicsWindow));
        window.Show();
    }

    static bool GetIntrinsics(GameObject obj)
    {
        if (obj == null) {
            return false;
        } else {
            Camera cam = obj.GetComponent<Camera>();
            
            var radAngle = cam.fieldOfView * Mathf.Deg2Rad;
            var radHFOV = 2 * Mathf.Atan(Mathf.Tan(radAngle / 2) * cam.aspect);
            var hFOV = Mathf.Rad2Deg * radHFOV;
            var vFOV = cam.fieldOfView;

            fx = (double)((float)cam.pixelWidth / (2.0f * Mathf.Tan(0.5f * hFOV * Mathf.Deg2Rad)));
            fy = (double)((float)cam.pixelHeight / (2.0f * Mathf.Tan(0.5f * vFOV * Mathf.Deg2Rad)));
            cx = cam.pixelWidth / 2.0;
            cy = cam.pixelHeight / 2.0;

            width = cam.pixelWidth;
            height = cam.pixelHeight;

            return true;
        }
    }

    void OnGUI()
    {
        obj = (GameObject)EditorGUILayout.ObjectField("Camera:", obj, typeof(GameObject), true);
        
        if (GUILayout.Button("Get Intrinsics"))
            result = GetIntrinsics(obj);
        
        GUILayout.Label("Result:", EditorStyles.boldLabel);
        if (result)
        {
            string format = "header:\n"
                            + "  seq: 0\n"
                            + "  stamp:\n"
                            + "    secs: 0\n"
                            + "    nsecs: 0\n"
                            + "  frame_id: ''\n"
                            + "width: {0}\n"
                            + "height: {1}\n"
                            + "distortion_model: plumb_bob\n"
                            + "D: [0.0, 0.0, 0.0, 0.0, 0.0]\n"
                            + "K: [{2}, 0.0, {3}, 0.0, {4}, {5}, 0.0, 0.0, 1.0]\n"
                            + "R: [1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0]\n"
                            + "P: [{6}, 0.0, {7}, 0.0, 0.0, {8}, {9}, 0.0, 0.0, 0.0, 1.0, 0.0]\n"
                            + "binning_x: 0\n"
                            + "binning_y: 0\n"
                            + "roi:\n"
                            + "  x_offset: 0\n"
                            + "  y_offset: 0\n"
                            + "  height: 0\n"
                            + "  width: 0\n"
                            + "  do_rectify: False";
            string output = string.Format(format, width, height, fx, cx, fy, cy, fx, cx, fy, cy);
            EditorGUILayout.TextArea(output);
        } else {
            GUILayout.Label("Please select a camera object to get intrinsics.");
        }
    }
}
