/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using UnityEngine.UI;

public class VisualizerTFLabel : MonoBehaviour
{
    private string Label { get; set; }
    private LineRenderer LRenderer;
    private Text[] Labels;
    private Transform ParentTransform;

    private void Awake()
    {
        LRenderer = GetComponent<LineRenderer>();
        Labels = GetComponentsInChildren<Text>();
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

    void LateUpdate()
    {
        var position = Camera.main.WorldToScreenPoint(ParentTransform.position);
        var offset = ParentTransform.position == transform.position ? Labels[0].rectTransform.rect.height : 0f;
        Labels[0].text = ParentTransform.name;
        Labels[0].rectTransform.anchoredPosition = new Vector2(position.x, position.y + offset);

        position = Camera.main.WorldToScreenPoint(gameObject.transform.position);
        Labels[1].text = Label;
        Labels[1].rectTransform.anchoredPosition = new Vector2(position.x, position.y);
    }
}
