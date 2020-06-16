/**
 * Copyright (c) 2019-2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Simulator.Sensors.UI
{
    public enum WindowSizeType
    {
        Window = 0,
        Full = 1
    };

    public class Visualizer : MonoBehaviour, IPointerDownHandler
    {
        public Button ExitButton;
        public Button ResizeButton;
        public GameObject ExpandTextGO;
        public GameObject ContractTextGO;
        public Text VisualizerNameText;
        public GameObject HeaderGO;
        public GameObject CameraVisualGO;
        public GameObject ValuesVisualGO;

        public VisualizerToggle VisualizerToggle { get; set; }
        public SensorBase Sensor { get; set; }
        public RawImage CameraRawImage { get; private set; }

        public Text ValuesText { get; private set; }

        private StringBuilder sb = new StringBuilder();
        private float elapsedTime = 1f;
        private RectTransform rt;
        private Image bgImage;
        private AspectRatioFitter fitter;
        private Vector2 windowSize = new Vector2(320f, 180f);
        private Vector3 windowPosition = Vector3.zero;

        public WindowSizeType CurrentWindowSizeType { get; private set; } = WindowSizeType.Window;

        private void Awake()
        {
            bgImage = GetComponent<Image>();
            rt = GetComponent<RectTransform>();
            ValuesText = ValuesVisualGO.GetComponent<Text>();
            CameraRawImage = CameraVisualGO.GetComponentInChildren<RawImage>(true);
            fitter = CameraVisualGO.GetComponentInChildren<AspectRatioFitter>(true);

            bgImage.enabled = false;
            HeaderGO.SetActive(false);
            CameraVisualGO.SetActive(false);
            ValuesVisualGO.SetActive(false);
            ContractTextGO.SetActive(false);
            ExpandTextGO.SetActive(false);

            ExitButton.onClick.AddListener(ExitButtonOnClick);
            ResizeButton.onClick.AddListener(ResizeOnClick);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            transform.SetAsLastSibling();
        }

        public void Init(string name)
        {
            this.name = name;
            VisualizerNameText.text = name;

            if (rt == null)
            {
                return;
            }

            if (PlayerPrefs.HasKey($"Visualizer/{name}/position/x"))
            {
                var posX = PlayerPrefs.GetFloat($"Visualizer/{name}/position/x");
                var posY = PlayerPrefs.GetFloat($"Visualizer/{name}/position/y");
                if (posX != 0 && posY != 0)
                {
                    rt.localPosition = new Vector3(posX * Screen.width, posY * Screen.height, 0);
                }
            }

            if (PlayerPrefs.HasKey($"Visualizer/{name}/size/x"))
            {
                var sizeX = PlayerPrefs.GetFloat($"Visualizer/{name}/size/x");
                var sizeY = PlayerPrefs.GetFloat($"Visualizer/{name}/size/y");
                if (sizeX != 0 && sizeY != 0)
                {
                    rt.sizeDelta = new Vector2(sizeX * Screen.width, sizeY * Screen.height);
                }
            }

            if (PlayerPrefs.HasKey($"Visualizer/{name}/widowsizetype"))
            {
                CurrentWindowSizeType = (WindowSizeType)System.Enum.Parse(typeof(WindowSizeType), PlayerPrefs.GetString($"Visualizer/{name}/widowsizetype"));
            }

            UpdateWindowSize((int)CurrentWindowSizeType, true);
        }

        private void OnEnable()
        {
            Sensor?.OnVisualizeToggle(true);
        }
        
        private void Update()
        {
            // save rt size/position for full to window
            if (CurrentWindowSizeType == WindowSizeType.Window && rt != null)
            {
                windowSize = rt.sizeDelta;
                windowPosition = rt.localPosition;
            }
        }

        private void LateUpdate()
        {
            Debug.Assert(Sensor != null);
            Sensor.OnVisualize(this);
        }

        private void OnDisable()
        {
            Sensor?.OnVisualizeToggle(false);

            if (rt != null)
            {
                var pos = rt.localPosition / new Vector2(Screen.width, Screen.height);
                if (pos.x != 0 && pos.y != 0)
                {
                    PlayerPrefs.SetFloat($"Visualizer/{name}/position/x", pos.x);
                    PlayerPrefs.SetFloat($"Visualizer/{name}/position/y", pos.y);
                }

                var size = rt.sizeDelta / new Vector2(Screen.width, Screen.height);
                PlayerPrefs.SetFloat($"Visualizer/{name}/size/x", size.x);
                PlayerPrefs.SetFloat($"Visualizer/{name}/size/y", size.y);

                PlayerPrefs.SetString($"Visualizer/{name}/widowsizetype", CurrentWindowSizeType.ToString());

                PlayerPrefs.Save();
            }
        }

        private void OnDestroy()
        {
            ExitButton.onClick.RemoveListener(ExitButtonOnClick);
            ResizeButton.onClick.RemoveListener(ResizeOnClick);
        }

        public void UpdateRenderTexture(RenderTexture renderTexture, float aspectRatio)
        {
            Debug.Assert(renderTexture != null);

            ToggleVisualizerElements(true);

            fitter.aspectRatio = aspectRatio;
            CameraRawImage.texture = renderTexture;
        }

        public void UpdateGraphValues(Dictionary<string, object> datas)
        {
            Debug.Assert(ValuesText != null);

            ToggleVisualizerElements(false);

            if (elapsedTime >= 1)
            {
                sb.Clear();
                foreach (var data in datas)
                {
                    sb.AppendLine($"{data.Key}: {data.Value}");
                }
                ValuesText.text = sb.ToString();
                elapsedTime = 0f;
            }
            else
            {
                elapsedTime += Time.unscaledDeltaTime;
            }
        }

        private void ToggleVisualizerElements(bool isCamera)
        {
            if (!HeaderGO.activeInHierarchy)
            {
                HeaderGO.SetActive(true);
            }
            if (!bgImage.enabled)
            {
                bgImage.enabled = true;
            }

            if (isCamera)
            {
                if (!CameraVisualGO.activeInHierarchy)
                {
                    CameraVisualGO.SetActive(true);
                }
            }
            else
            {
                if (!ValuesVisualGO.activeInHierarchy)
                {
                    ValuesVisualGO.SetActive(true);
                }
            }
        }

        private void ExitButtonOnClick()
        {
            VisualizerToggle.ExitButtonClicked();
        }

        private void ResizeOnClick()
        {
            UpdateWindowSize();
        }

        public void UpdateWindowSize(int type = -1, bool isSaved = false)
        {
            CurrentWindowSizeType = type == -1 ? ((int)CurrentWindowSizeType == System.Enum.GetValues(typeof(WindowSizeType)).Length - 1) ? 0 : CurrentWindowSizeType + 1 : (WindowSizeType)type;

            switch (CurrentWindowSizeType)
            {
                case WindowSizeType.Window:
                    if (!isSaved)
                    {
                        rt.sizeDelta = windowSize;
                        rt.localPosition = windowPosition;
                    }
                    ContractTextGO.SetActive(false);
                    ExpandTextGO.SetActive(true);
                    break;
                case WindowSizeType.Full:
                    if (!isSaved)
                    {
                        rt.sizeDelta = new Vector2(Screen.width, Screen.height);
                        rt.localPosition = new Vector3(-Screen.width / 2, Screen.height / 2, 0f);
                    }
                    ContractTextGO.SetActive(true);
                    ExpandTextGO.SetActive(false);
                    break;
            }
        }

        public void ResetWindow()
        {
            if (rt == null)
            {
                return;
            }

            rt.sizeDelta = new Vector2(320f, 180f);
            SetWindowType();
            transform.SetAsLastSibling();
        }

        public void SetWindowType()
        {
            CurrentWindowSizeType = WindowSizeType.Window;
            ContractTextGO.SetActive(false);
            ExpandTextGO.SetActive(true);
        }
    }
}
