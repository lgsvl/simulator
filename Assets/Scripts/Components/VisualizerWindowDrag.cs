/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Simulator.Sensors.UI;
using UnityEngine;
using UnityEngine.EventSystems;

public class VisualizerWindowDrag : EventTrigger
{
    private bool dragging;
    private Vector2 offset;
    private Visualizer visualizer;

    private void Awake()
    {
        visualizer = GetComponentInParent<Visualizer>();
    }

    public void Update()
    {
        if (dragging)
        {
            transform.parent.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y) + offset;
        }
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (visualizer == null)
        {
            return;
        }

        if (visualizer.CurrentWindowSizeType == WindowSizeType.Full)
        {
            return;
        }

        offset = new Vector2(transform.parent.position.x, transform.parent.position.y) - RectTransformUtility.WorldToScreenPoint(null, eventData.position);
        dragging = true;
        transform.parent.transform.SetAsLastSibling();
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        dragging = false;
    }
}
