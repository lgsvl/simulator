/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.ColorPicker
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Elements;
    using Inspector;
    using Managers;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    /// <summary>
    /// Panel which allows picking color by editing RGB or HSV parameters
    /// </summary>
    public class ColorPicker : MonoBehaviour, IDragHandler
    {
        /// <summary>
        /// Cached shader property id for the _Hue
        /// </summary>
        private static readonly int HueShaderProperty = Shader.PropertyToID("_Hue");
        
        /// <summary>
        /// Cached shader property id for the _Sat
        /// </summary>
        private static readonly int SatShaderProperty = Shader.PropertyToID("_Sat");
        
        /// <summary>
        /// Cached shader property id for the _Val
        /// </summary>
        private static readonly int ValShaderProperty = Shader.PropertyToID("_Val");
        
        /// <summary>
        /// Cached shader property id for the _Red
        /// </summary>
        private static readonly int RedShaderProperty = Shader.PropertyToID("_Red");
        
        /// <summary>
        /// Cached shader property id for the _Green
        /// </summary>
        private static readonly int GreenShaderProperty = Shader.PropertyToID("_Green");
        
        /// <summary>
        /// Cached shader property id for the _Blue
        /// </summary>
        private static readonly int BlueShaderProperty = Shader.PropertyToID("_Blue");

        //Ignoring Roslyn compiler warning for unassigned private field with SerializeField attribute
#pragma warning disable 0649
        /// <summary>
        /// Image that displayed currently selected color
        /// </summary>
        [SerializeField]
        private Image colorSample;

        /// <summary>
        /// Input field that allows entering the color in hex format
        /// </summary>
        [SerializeField]
        private InputField colorHex;

        /// <summary>
        /// Slider that allows editing the color's hue
        /// </summary>
        [SerializeField]
        private Slider hueSlider;

        /// <summary>
        /// Slider that allows editing the color's saturation
        /// </summary>
        [SerializeField]
        private Slider satSlider;

        /// <summary>
        /// Slider that allows editing the color's value
        /// </summary>
        [SerializeField]
        private Slider valSlider;

        /// <summary>
        /// Slider that allows editing the color's red intensity
        /// </summary>
        [SerializeField]
        private Slider redSlider;

        /// <summary>
        /// Slider that allows editing the color's green intensity
        /// </summary>
        [SerializeField]
        private Slider greenSlider;

        /// <summary>
        /// Slider that allows editing the color's blue intensity
        /// </summary>
        [SerializeField]
        private Slider blueSlider;
#pragma warning restore 0649

        /// <summary>
        /// Cached materials used by children that require _Hue parameter updates
        /// </summary>
        private readonly List<Material> hueDependentMaterials = new List<Material>();
        
        /// <summary>
        /// Cached materials used by children that require _Sat parameter updates
        /// </summary>
        private readonly List<Material> satDependentMaterials = new List<Material>();
        
        /// <summary>
        /// Cached materials used by children that require _Val parameter updates
        /// </summary>
        private readonly List<Material> valDependentMaterials = new List<Material>();
        
        /// <summary>
        /// Cached materials used by children that require _Red parameter updates
        /// </summary>
        private readonly List<Material> redDependentMaterials = new List<Material>();
        
        /// <summary>
        /// Cached materials used by children that require _Green parameter updates
        /// </summary>
        private readonly List<Material> greenDependentMaterials = new List<Material>();
        
        /// <summary>
        /// Cached materials used by children that require _Blue parameter updates
        /// </summary>
        private readonly List<Material> blueDependentMaterials = new List<Material>();

        /// <summary>
        /// Cached currently set color in the HSV format
        /// </summary>
        private readonly HSVColor hsvColor = new HSVColor();
        
        /// <summary>
        /// Cached currently set color in the RGB format
        /// </summary>
        private readonly RGBColor rgbColor = new RGBColor();

        /// <summary>
        /// Is this color picker initialized
        /// </summary>
        private bool isInitialized;

        /// <summary>
        /// Callback invoked everytime when color changes
        /// </summary>
        private event Action<Color> ColorChangeCallback;
        
        /// <summary>
        /// Callback invoked when the color picker panel is hidden
        /// </summary>
        private event Action HiddenCallback;

        /// <summary>
        /// Cached currently set color in the HSV format
        /// </summary>
        public HSVColor CurrentHSVColor => hsvColor;

        /// <summary>
        /// Unity Awake method
        /// </summary>
        private void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// Unity OnDestroy method
        /// </summary>
        private void OnDestroy()
        {
            Deinitialize();
        }

        /// <summary>
        /// Initialization method
        /// </summary>
        private void Initialize()
        {
            if (isInitialized)
                return;
            var images = GetComponentsInChildren<Image>();
            var matSubs = new Dictionary<Material, Material>();
            foreach (var image in images)
            {
                var material = image.material;
                if (material == null)
                    continue;
                if (matSubs.TryGetValue(material, out var materialClone))
                {
                    image.material = materialClone;
                    continue;
                }
                //Clone the material, so editing parameters won't change the resource
                materialClone = new Material(material);
                if (materialClone.HasProperty(HueShaderProperty))
                    hueDependentMaterials.Add(materialClone);
                if (materialClone.HasProperty(SatShaderProperty))
                    satDependentMaterials.Add(materialClone);
                if (materialClone.HasProperty(ValShaderProperty))
                    valDependentMaterials.Add(materialClone);
                if (materialClone.HasProperty(RedShaderProperty))
                    redDependentMaterials.Add(materialClone);
                if (materialClone.HasProperty(GreenShaderProperty))
                    greenDependentMaterials.Add(materialClone);
                if (materialClone.HasProperty(BlueShaderProperty))
                    blueDependentMaterials.Add(materialClone);
                matSubs.Add(material, materialClone);
                image.material = materialClone;
            }

            hsvColor.HueChanged += OnHueChanged;
            hsvColor.SatChanged += OnSatChanged;
            hsvColor.ValChanged += OnValChanged;

            rgbColor.RedChanged += OnRedChanged;
            rgbColor.GreenChanged += OnGreenChanged;
            rgbColor.BlueChanged += OnBlueChanged;
            isInitialized = true;
        }

        /// <summary>
        /// Deinitialization method
        /// </summary>
        private void Deinitialize()
        {
            if (!isInitialized)
                return;
            hsvColor.HueChanged -= OnHueChanged;
            hsvColor.SatChanged -= OnSatChanged;
            hsvColor.ValChanged -= OnValChanged;

            rgbColor.RedChanged -= OnRedChanged;
            rgbColor.GreenChanged -= OnGreenChanged;
            rgbColor.BlueChanged -= OnBlueChanged;
            isInitialized = false;
        }

        /// <summary>
        /// Shows the color picker and setups it with the given parameters
        /// </summary>
        /// <param name="startColor">Color that is set when this panel shows</param>
        /// <param name="onColorChange">Callback invoked everytime when color changes</param>
        /// <param name="onHidden">Callback invoked when the color picker panel is hidden</param>
        public void Show(Color startColor, Action<Color> onColorChange, Action onHidden)
        {
            Initialize();
            ScenarioManager.Instance.SelectedOtherElement += OnSelectedOtherElement;
            ScenarioManager.Instance.inspector.MenuItemChanged += InspectorOnMenuItemChanged;
            ColorChangeCallback = onColorChange;
            HiddenCallback = onHidden;
            SetColor(startColor);
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Hides this panel and clear its setup 
        /// </summary>
        public void Hide()
        {
            ScenarioManager.Instance.SelectedOtherElement -= OnSelectedOtherElement;
            ScenarioManager.Instance.inspector.MenuItemChanged -= InspectorOnMenuItemChanged;
            ColorChangeCallback = null;
            gameObject.SetActive(false);
            HiddenCallback?.Invoke();
            HiddenCallback = null;
        }

        /// <summary>
        /// Method invoked when other scenario element is selected
        /// </summary>
        /// <param name="previousElement">Deselected scenario element</param>
        /// <param name="scenarioElement">Selected scenario element</param>
        private void OnSelectedOtherElement(ScenarioElement previousElement, ScenarioElement scenarioElement)
        {
            Hide();
        }

        /// <summary>
        /// Method invoked when other menu item is selected in the inspector
        /// </summary>
        /// <param name="menuItem">Selected menu item</param>
        private void InspectorOnMenuItemChanged(InspectorMenuItem menuItem)
        {
            Hide();
        }

        /// <summary>
        /// Changes the currently selected color
        /// </summary>
        /// <param name="newColor">New color that will be set in the picker</param>
        public void SetColor(Color newColor)
        {
            rgbColor.CurrentColor = newColor;
            hsvColor.FromRGB(rgbColor);
            OnColorChanged();
        }

        /// <summary>
        /// Method called when the color changes
        /// </summary>
        private void OnColorChanged()
        {
            var c = rgbColor.CurrentColor;
            if (colorSample != null)
                colorSample.color = c;
            if (colorHex != null)
                colorHex.SetTextWithoutNotify(
                    $"#{Mathf.RoundToInt(c.r * 255):X2}{Mathf.RoundToInt(c.g * 255):X2}{Mathf.RoundToInt(c.b * 255):X2}");
            ColorChangeCallback?.Invoke(c);
        }

        /// <summary>
        /// Method called when the hue parameter changes
        /// </summary>
        /// <param name="hue">New hue value</param>
        private void OnHueChanged(float hue)
        {
            foreach (var material in hueDependentMaterials)
                material.SetFloat(HueShaderProperty, hue);
            if (hueSlider!=null)
                hueSlider.SetValueWithoutNotify(hue);
        }

        /// <summary>
        /// Method called when the saturation parameter changes
        /// </summary>
        /// <param name="saturation">New saturation value</param>
        private void OnSatChanged(float saturation)
        {
            foreach (var material in satDependentMaterials)
                material.SetFloat(SatShaderProperty, saturation);
            if (satSlider!=null)
                satSlider.SetValueWithoutNotify(saturation);
        }

        /// <summary>
        /// Method called when the value parameter changes
        /// </summary>
        /// <param name="value">New value</param>
        private void OnValChanged(float value)
        {
            foreach (var material in valDependentMaterials)
                material.SetFloat(ValShaderProperty, value);
            if (valSlider!=null)
                valSlider.SetValueWithoutNotify(value);
        }

        /// <summary>
        /// Method called when the red parameter changes
        /// </summary>
        /// <param name="red">New red value</param>
        private void OnRedChanged(float red)
        {
            foreach (var material in redDependentMaterials)
                material.SetFloat(RedShaderProperty, red);
            if (redSlider!=null)
                redSlider.SetValueWithoutNotify(red);
        }

        /// <summary>
        /// Method called when the green parameter changes
        /// </summary>
        /// <param name="green">New green value</param>
        private void OnGreenChanged(float green)
        {
            foreach (var material in greenDependentMaterials)
                material.SetFloat(GreenShaderProperty, green);
            if (greenSlider!=null)
                greenSlider.SetValueWithoutNotify(green);
        }

        /// <summary>
        /// Method called when the blue parameter changes
        /// </summary>
        /// <param name="blue">New blue value</param>
        private void OnBlueChanged(float blue)
        {
            foreach (var material in blueDependentMaterials)
                material.SetFloat(BlueShaderProperty, blue);
            if (blueSlider!=null)
                blueSlider.SetValueWithoutNotify(blue);
        }

        /// <summary>
        /// Sets the new hue value
        /// </summary>
        /// <param name="hue">New hue value</param>
        public void SetHue(float hue)
        {
            hsvColor.H = hue;
            rgbColor.FromHSV(hsvColor);
            OnColorChanged();
        }

        /// <summary>
        /// Sets the new saturation value
        /// </summary>
        /// <param name="saturation">New saturation value</param>
        public void SetSat(float saturation)
        {
            hsvColor.S = saturation;
            rgbColor.FromHSV(hsvColor);
            OnColorChanged();
        }

        /// <summary>
        /// Sets the new value
        /// </summary>
        /// <param name="value">New value</param>
        public void SetVal(float value)
        {
            hsvColor.V = value;
            rgbColor.FromHSV(hsvColor);
            OnColorChanged();
        }

        /// <summary>
        /// Sets the new saturation and value
        /// </summary>
        /// <param name="saturation">New saturation value</param>
        /// <param name="value">New value</param>
        public void SetSatAndVal(float saturation, float value)
        {
            hsvColor.S = saturation;
            hsvColor.V = value;
            rgbColor.FromHSV(hsvColor);
            OnColorChanged();
        }

        /// <summary>
        /// Sets the new red value
        /// </summary>
        /// <param name="red">New red value</param>
        public void SetRed(float red)
        {
            rgbColor.R = red;
            hsvColor.FromRGB(rgbColor);
            OnColorChanged();
        }

        /// <summary>
        /// Sets the new green value
        /// </summary>
        /// <param name="green">New green value</param>
        public void SetGreen(float green)
        {
            rgbColor.G = green;
            hsvColor.FromRGB(rgbColor);
            OnColorChanged();
        }

        /// <summary>
        /// Sets the new blue value
        /// </summary>
        /// <param name="blue">New blue value</param>
        public void SetBlue(float blue)
        {
            rgbColor.B = blue;
            hsvColor.FromRGB(rgbColor);
            OnColorChanged();
        }

        /// <summary>
        /// Sets current color to parsed color value from the hex formatted string
        /// </summary>
        /// <param name="hexString">Color in the hex formatted string</param>
        public void SetColorHex(string hexString)
        {
            if (hexString.IndexOf('#') != -1)
                hexString = hexString.Replace("#", "");

            int r, g, b;

            try
            {
                r = int.Parse(hexString.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                g = int.Parse(hexString.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                b = int.Parse(hexString.Substring(4, 2), NumberStyles.AllowHexSpecifier);
            }
            catch (ArgumentException)
            {
                return;
            }

            rgbColor.CurrentColor = new Color(r / 255.0f, g / 255.0f, b / 255.0f, 1.0f);
            hsvColor.FromRGB(rgbColor);
            OnColorChanged();
        }

        /// <summary>
        /// Method invoked when the color picker is being dragged
        /// </summary>
        /// <param name="eventData">Pointer event data</param>
        public void OnDrag(PointerEventData eventData)
        {
            transform.position += new Vector3(eventData.delta.x, eventData.delta.y);
        }
    }
}