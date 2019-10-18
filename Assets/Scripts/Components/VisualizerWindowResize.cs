/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using UnityEngine.EventSystems;
using Simulator.Sensors.UI;

public class VisualizerWindowResize : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public Visualizer Visualizer;
    private RectTransform visualizerRT;
    private RectTransform rootRT;
    private Vector3 currentPointerPosition;
    private Vector3 previousPointerPosition;

    private void Awake()
    {
        Visualizer = GetComponentInParent<Visualizer>();
        visualizerRT = Visualizer.GetComponent<RectTransform>();
        rootRT = SimulatorManager.Instance.UIManager.VisualizerCanvasGO.GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData data)
    {
        visualizerRT.SetAsLastSibling();
        RectTransformUtility.ScreenPointToWorldPointInRectangle(visualizerRT, data.position, data.pressEventCamera, out previousPointerPosition);
    }

    public void OnDrag(PointerEventData data)
    {
        Visualizer.SetWindowType();
        Vector2 sizeDelta = visualizerRT.sizeDelta;
        if (RectTransformUtility.RectangleContainsScreenPoint(rootRT, data.position, null))
        {
            RectTransformUtility.ScreenPointToWorldPointInRectangle(visualizerRT, data.position, data.pressEventCamera, out currentPointerPosition);
            Vector3 resizeValue = currentPointerPosition - previousPointerPosition;
            Vector2 resize = new Vector2(resizeValue.x, resizeValue.y);
            sizeDelta += new Vector2(resizeValue.x, -resizeValue.y);
            sizeDelta = new Vector2(Mathf.Clamp(sizeDelta.x, (Screen.width / 8f), Screen.width), Mathf.Clamp(sizeDelta.y, (Screen.height / 8f), Screen.height));

            visualizerRT.sizeDelta = sizeDelta;
            previousPointerPosition = currentPointerPosition;

            var pos = visualizerRT.position;
            pos.x = Mathf.Clamp(pos.x, 0f, rootRT.sizeDelta.x - visualizerRT.sizeDelta.x);
            pos.y = Mathf.Clamp(pos.y, visualizerRT.sizeDelta.y, rootRT.sizeDelta.y);
            visualizerRT.position = pos;
        }
        else
        {
            visualizerRT.sizeDelta = sizeDelta;
        }
    }
}
