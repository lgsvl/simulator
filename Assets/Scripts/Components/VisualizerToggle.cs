/**
 * Copyright (c) 2019-2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Simulator.Sensors.UI
{
    public class VisualizerToggle : MonoBehaviour
    {
        public Text VisualizerNameText;
        public GameObject TFVisualPrefab;
        private GameObject TFVisualGO;
        private VisualizerTFLabel TFLabel;

        public Button VSButton;
        public Image VSImage;
        public Sprite VSOnSprite;
        public Sprite VSOffSprite;
        private bool VSState = false;

        public Button TFButton;
        public Image TFImage;
        public Sprite TFOnSprite;
        public Sprite TFOffSprite;
        private bool TFState = false;

        public Visualizer Visualizer { get; set; }
        public SensorBase Sensor { get; set; }

        public void Init(string label, Transform parent = null)
        {
            TFVisualGO = Instantiate(TFVisualPrefab, Sensor.transform);
            TFVisualGO.SetActive(false);
            TFLabel = TFVisualGO.GetComponent<VisualizerTFLabel>();
            TFLabel.Init(label, parent);
            VSImage.sprite = VSOffSprite;
            TFImage.sprite = TFOffSprite;
        }

        public void VSOnClick()
        {
            if (Visualizer == null)
                return;

            VSState = !VSState;
            VSImage.sprite = VSState ? VSOnSprite : VSOffSprite;
            Visualizer.gameObject.SetActive(VSState);
            Visualizer.transform.SetAsLastSibling();
        }

        public void TFOnClick()
        {
            if (TFVisualGO == null)
                return;

            TFState = !TFState;
            TFImage.sprite = TFState ? TFOnSprite : TFOffSprite;
            TFVisualGO.SetActive(TFState);
        }

        public void ExitButtonClicked()
        {
            if (Visualizer == null)
                return;

            VSOnClick();
        }

        public void ResetToggle()
        {
            VSState = false;
            VSImage.sprite = VSOffSprite;

            TFState = false;
            TFImage.sprite = TFOffSprite;
            TFVisualGO.SetActive(TFState);
        }

        private void OnDestroy()
        {
            if (TFVisualGO != null)
            {
                Destroy(TFVisualGO);
            }
        }
    }
}
