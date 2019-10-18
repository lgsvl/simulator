/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Simulator.Sensors.UI;
using UnityEngine;
using UnityEngine.EventSystems;

public class VisualizerWindowDrag : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    private Vector2 offset;
    private Visualizer visualizer;
    private RectTransform rootRT;
    private RectTransform visRT;

    private void Awake()
    {
        rootRT = SimulatorManager.Instance.UIManager.VisualizerCanvasGO.GetComponent<RectTransform>();
        visualizer = GetComponentInParent<Visualizer>();
        visRT = visualizer.GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (visualizer.CurrentWindowSizeType == WindowSizeType.Full)
        {
            return;
        }

        offset = new Vector2(transform.parent.position.x, transform.parent.position.y) - RectTransformUtility.WorldToScreenPoint(null, eventData.position);
        transform.parent.transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData data)
    {
        if (visualizer.CurrentWindowSizeType == WindowSizeType.Full)
        {
            return;
        }

        var pos = new Vector2(Input.mousePosition.x, Input.mousePosition.y) + offset;
        pos.x = Mathf.Clamp(pos.x, 0f, rootRT.sizeDelta.x - visRT.sizeDelta.x);
        pos.y = Mathf.Clamp(pos.y, visRT.sizeDelta.y, rootRT.sizeDelta.y);
        transform.parent.position = pos;
    }
}
