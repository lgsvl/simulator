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
        Window = 0,
        Full = 1
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
        private Image bgImage;

        private AspectRatioFitter fitter;
        private Vector2 windowSize;
        private Vector2 fullSize;
        private Vector3 windowPosition;
        private float headerAnchoredYPos = 0f;

        private RectTransform rootRT;

        public WindowSizeType CurrentWindowSizeType { get; private set; } = WindowSizeType.Window;

        private void Awake()
        {
            bgImage = GetComponent<Image>();
            bgImage.enabled = false;
            rootRT = SimulatorManager.Instance.UIManager.VisualizerCanvasGO.GetComponent<RectTransform>();
            HeaderRT.gameObject.SetActive(false);
            ContractTextGO.SetActive(false);
            ExpandTextGO.SetActive(false);
            windowSize = new Vector2(240f, 135f);
            fullSize = new Vector2(Screen.width, Screen.height);
            rt = GetComponent<RectTransform>();
            rt.sizeDelta = windowSize;
            headerAnchoredYPos = HeaderRT.anchoredPosition.y;
            CurrentWindowSizeType = WindowSizeType.Window;
            UpdateWindowSize((int)CurrentWindowSizeType);
            
            CameraRawImage = CameraVisualGO.GetComponentInChildren<RawImage>();
            cameraRT = CameraVisualGO.GetComponent<RectTransform>();
            ValuesText = ValuesVisualGO.GetComponent<Text>();
            fitter = CameraVisualGO.GetComponentInChildren<AspectRatioFitter>();
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

            // save rt size/position for full to window
            if (CurrentWindowSizeType == WindowSizeType.Window && rt != null)
            {
                windowSize = rt.sizeDelta;
                windowPosition = rt.localPosition;
            }
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
            }

            if (!bgImage.enabled == false)
            {
                bgImage.enabled = true;
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

            if (!bgImage.enabled == false)
            {
                bgImage.enabled = true;
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
                case WindowSizeType.Window:
                    rt.sizeDelta = windowSize;
                    rt.localPosition = windowPosition;
                    HeaderRT.anchoredPosition = new Vector2(0f, headerAnchoredYPos);
                    ContractTextGO.SetActive(false);
                    ExpandTextGO.SetActive(true);
                    break;
                case WindowSizeType.Full:
                    rt.sizeDelta = fullSize;
                    rt.localPosition = new Vector3(-fullSize.x / 2, fullSize.y / 2, 0f);
                    HeaderRT.anchoredPosition = new Vector2(0f, -headerAnchoredYPos);
                    ContractTextGO.SetActive(true);
                    ExpandTextGO.SetActive(false);
                    break;
            }
        }
    }
}
