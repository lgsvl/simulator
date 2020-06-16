/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

public class VisualizerTFLabel : MonoBehaviour
{
    private string Label { get; set; }
    private LineRenderer LRenderer;
    private Transform ParentTransform;

    private void Awake()
    {
        LRenderer = GetComponent<LineRenderer>();
    }

    public void Init(string label, Transform parent = null)
    {
        Label = label;
        ParentTransform = parent;
        if (ParentTransform != null)
        {
            LRenderer.SetPositions(new Vector3[] { transform.localPosition, transform.InverseTransformPoint(ParentTransform.transform.position) });
        }
    }

    void OnGUI()
    {
        var position = Camera.main.WorldToScreenPoint(ParentTransform.position);
        var textSize = GUI.skin.label.CalcSize(new GUIContent(ParentTransform.name));
        var offset = ParentTransform.position == transform.position ? textSize.y : 0f;
        GUI.Label(new Rect(position.x, Screen.height - position.y + offset, textSize.x, textSize.y), ParentTransform.name);

        position = Camera.main.WorldToScreenPoint(gameObject.transform.position);
        textSize = GUI.skin.label.CalcSize(new GUIContent(Label));
        GUI.Label(new Rect(position.x, Screen.height - position.y, textSize.x, textSize.y), Label);
    }
}
