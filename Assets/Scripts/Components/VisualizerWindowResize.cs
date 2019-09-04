/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class VisualizerWindowResize : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public Vector2 minSize;
    public Vector2 maxSize;

    public RectTransform ParentRT;
    private Vector3 currentPointerPosition;
    private Vector3 previousPointerPosition;

    private RectTransform rootRT;
    public RectTransform headerRT;

    private void Awake()
    {
        rootRT = SimulatorManager.Instance.UIManager.VisualizerCanvasGO.GetComponent<RectTransform>();
        maxSize = new Vector2(Screen.width, Screen.height - headerRT.rect.max.y * 2);
    }

    public void OnPointerDown(PointerEventData data)
    {
        ParentRT.SetAsLastSibling();
        RectTransformUtility.ScreenPointToWorldPointInRectangle(ParentRT, data.position, data.pressEventCamera, out previousPointerPosition);
    }

    public void OnDrag(PointerEventData data)
    {
        if (ParentRT == null)
            return;
        
        Vector2 sizeDelta = ParentRT.sizeDelta;

        RectTransformUtility.ScreenPointToWorldPointInRectangle(ParentRT, data.position, data.pressEventCamera, out currentPointerPosition);
        Vector3 resizeValue = currentPointerPosition - previousPointerPosition;
        Vector2 resize = new Vector2(resizeValue.x, resizeValue.y);
        sizeDelta += new Vector2(resizeValue.x, -resizeValue.y);
        sizeDelta = new Vector2(Mathf.Clamp(sizeDelta.x, minSize.x, maxSize.x),Mathf.Clamp(sizeDelta.y, minSize.y, maxSize.y));

        ParentRT.sizeDelta = sizeDelta;
        previousPointerPosition = currentPointerPosition;

        var pos = ParentRT.position;
        pos.x = Mathf.Clamp(pos.x, 0f, rootRT.sizeDelta.x - headerRT.rect.max.x * 2);
        pos.y = Mathf.Clamp(pos.y, ParentRT.sizeDelta.y, rootRT.sizeDelta.y - headerRT.rect.max.y * 2);
        ParentRT.position = pos;
    }
}
