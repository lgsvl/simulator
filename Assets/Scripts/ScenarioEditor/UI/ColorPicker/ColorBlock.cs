/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.ColorPicker
{
    using UnityEngine;
    using UnityEngine.EventSystems;

    /// <summary>
    /// The UI color block that allows changing the color's saturation and value with the pointer
    /// </summary>
    public class ColorBlock : MonoBehaviour, IDragHandler, IPointerDownHandler
    {
        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Handle that points current saturation and value on this color block
        /// </summary>
        [SerializeField]
        private RectTransform handle;
#pragma warning restore 0649
        
        /// <summary>
        /// Cached <see cref="RectTransform"/> of this game object
        /// </summary>
        private RectTransform rectTransform;
        
        /// <summary>
        /// Parent color picker that includes this color block
        /// </summary>
        private ColorPicker colorPicker;
        
        /// <summary>
        /// Preallocated vectors array for fetching corners of this rect transform
        /// </summary>
        private readonly Vector3[] corners = new Vector3[4];

        /// <summary>
        /// Unity Awake method
        /// </summary>
        private void Awake()
        {
            rectTransform = transform as RectTransform;
            colorPicker = GetComponentInParent<ColorPicker>();
            colorPicker.CurrentHSVColor.SatChanged += OnSatChanged;
            colorPicker.CurrentHSVColor.ValChanged += OnValChanged;
            OnSatChanged(colorPicker.CurrentHSVColor.S);
            OnValChanged(colorPicker.CurrentHSVColor.V);
        }

        /// <summary>
        /// Unity OnDestroy method
        /// </summary>
        private void OnDestroy()
        {
            if (colorPicker != null)
            {
                colorPicker.CurrentHSVColor.SatChanged -= OnSatChanged;
                colorPicker.CurrentHSVColor.ValChanged -= OnValChanged;
            }
        }

        /// <summary>
        /// Method invoked when the color's saturation changes
        /// </summary>
        /// <param name="saturation">New saturation of the edited color</param>
        private void OnSatChanged(float saturation)
        {
            var pos = handle.anchoredPosition;
            pos.x = rectTransform.sizeDelta.x * saturation;
            handle.anchoredPosition = pos;
        }

        /// <summary>
        /// Method invoked when the color's value changes
        /// </summary>
        /// <param name="value">New value of the edited color</param>
        private void OnValChanged(float value)
        {
            var pos = handle.anchoredPosition;
            pos.y = rectTransform.sizeDelta.y * value;
            handle.anchoredPosition = pos;
        }

        /// <summary>
        /// Updates either color's saturation and value basing on the pointer position
        /// </summary>
        /// <param name="eventData">Pointer event data</param>
        private void UpdateSatAndVal(PointerEventData eventData)
        {
            var x = (eventData.position.x - corners[0].x) / rectTransform.sizeDelta.x;
            var y = (eventData.position.y - corners[0].y) / rectTransform.sizeDelta.y;
            var sat = Mathf.Clamp(x, 0.0f, 1.0f);
            var val = Mathf.Clamp(y, 0.0f, 1.0f);
            colorPicker.SetSatAndVal(sat, val);
        }

        /// <summary>
        /// Method invoked when the handler is being dragged
        /// </summary>
        /// <param name="eventData">Pointer event data</param>
        public void OnDrag(PointerEventData eventData)
        {
            UpdateSatAndVal(eventData);
        }

        /// <summary>
        /// Method invoked when pointer is pressed on the color block
        /// </summary>
        /// <param name="eventData">Pointer event data</param>
        public void OnPointerDown(PointerEventData eventData)
        {
            rectTransform.GetWorldCorners(corners);
            UpdateSatAndVal(eventData);
        }
    }
}