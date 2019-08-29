/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Simulator.Sensors.UI
{
    public enum WindowSizeType
    {
        Small = 0,
        Medium = 1,
        Full = 2
    };

    public class Visualizer : MonoBehaviour
    {
        public Button ExitButton;
        public Button ResizeButton;
        public GameObject ExpandTextGO;
        public GameObject ContractTextGO;
        public Text VisualizerNameText;
        public RectTransform HeaderRT;
        public GameObject CameraVisualGO;
        public GameObject ValuesVisualGO;
        
        public VisualizerToggle VisualizerToggle { get; set; }
        public SensorBase Sensor { get; set; }
        public RawImage CameraRawImage { get; private set; }
        public Text ValuesText { get; private set; }
        
        private RectTransform rt;
        private RectTransform cameraRT;
        private AspectRatioFitter fitter;
        private Vector2 smallSize;
        private Vector2 medSize;
        private Vector2 fullSize;
        private float headerAnchoredYPos = 0f;

        public WindowSizeType CurrentWindowSizeType { get; private set; } = WindowSizeType.Small;

        private void Awake()
        {
            HeaderRT.gameObject.SetActive(false);
            ContractTextGO.SetActive(false);
            ExpandTextGO.SetActive(false);
            smallSize = new Vector2((Screen.width / 8), Screen.height / 8);
            medSize = new Vector2(Screen.width / 4, Screen.height / 4);
            fullSize = new Vector2(Screen.width, Screen.height);
            rt = GetComponent<RectTransform>();
            rt.sizeDelta = smallSize;
            headerAnchoredYPos = HeaderRT.anchoredPosition.y;
            CurrentWindowSizeType = WindowSizeType.Small;
            UpdateWindowSize((int)CurrentWindowSizeType);

            CameraRawImage = CameraVisualGO.GetComponent<RawImage>();
            cameraRT = CameraVisualGO.GetComponent<RectTransform>();
            ValuesText = ValuesVisualGO.GetComponent<Text>();
            fitter = CameraVisualGO.GetComponent<AspectRatioFitter>();
            CameraVisualGO.SetActive(false);
            ValuesVisualGO.SetActive(false);
        }
        
        private void OnEnable()
        {
            ExitButton.onClick.AddListener(ExitButtonOnClick);
            ResizeButton.onClick.AddListener(ResizeOnClick);
            Sensor?.OnVisualizeToggle(true);
        }
        
        private void Update()
        {
            Debug.Assert(Sensor != null);
            Sensor.OnVisualize(this);
        }

        private void OnDisable()
        {
            ExitButton.onClick.RemoveListener(ExitButtonOnClick);
            ResizeButton.onClick.RemoveListener(ResizeOnClick);
            Sensor?.OnVisualizeToggle(false);
        }

        public void UpdateRenderTexture(RenderTexture renderTexture, float aspectRatio)
        {
            Debug.Assert(renderTexture != null);
            if (!HeaderRT.gameObject.activeInHierarchy)
            {
                HeaderRT.gameObject.SetActive(true);
            }
            
            if (!CameraVisualGO.activeInHierarchy)
            {
                CameraVisualGO.SetActive(true);
                fitter.aspectRatio = aspectRatio;
                rt.sizeDelta = new Vector2(smallSize.x, cameraRT.sizeDelta.y);
                smallSize = rt.sizeDelta;
            }
            CameraRawImage.texture = renderTexture;
        }

        public void UpdateValues(string val)
        {
            if (!HeaderRT.gameObject.activeInHierarchy)
            {
                HeaderRT.gameObject.SetActive(true);
            }

            if (!ValuesVisualGO.activeInHierarchy)
            {
                ValuesVisualGO.SetActive(true);
            }
        }

        private void ExitButtonOnClick()
        {
            VisualizerToggle.OnToggleClicked(false);
        }

        private void ResizeOnClick()
        {
            UpdateWindowSize();
        }

        public void UpdateWindowSize(int type = -1)
        {
            CurrentWindowSizeType = type == -1 ? ((int)CurrentWindowSizeType == System.Enum.GetValues(typeof(WindowSizeType)).Length - 1) ? 0 : CurrentWindowSizeType + 1 : (WindowSizeType)type;
            
            switch (CurrentWindowSizeType)
            {
                case WindowSizeType.Small:
                    rt.sizeDelta = smallSize;
                    HeaderRT.anchoredPosition = new Vector2(0f, headerAnchoredYPos);
                    ContractTextGO.SetActive(false);
                    ExpandTextGO.SetActive(true);
                    break;
                case WindowSizeType.Medium:
                    rt.sizeDelta = medSize;
                    HeaderRT.anchoredPosition = new Vector2(0f, headerAnchoredYPos);
                    ContractTextGO.SetActive(false);
                    ExpandTextGO.SetActive(true);
                    break;
                case WindowSizeType.Full:
                    HeaderRT.anchoredPosition = new Vector2(0f, -headerAnchoredYPos);
                    rt.sizeDelta = fullSize;
                    rt.localPosition = Vector2.zero;
                    ContractTextGO.SetActive(true);
                    ExpandTextGO.SetActive(false);
                    break;
                default:
                    Debug.LogError("WindowSizeType out of bounds");
                    break;
            }
        }
    }
}
