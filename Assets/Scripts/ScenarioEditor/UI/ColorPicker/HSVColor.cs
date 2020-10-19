/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.ScenarioEditor.UI.ColorPicker
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Color in the HSV format
    /// </summary>
    public class HSVColor
    {
        /// <summary>
        /// Color's hue
        /// </summary>
        private float h = -1.0f;
        
        /// <summary>
        /// Color's saturation
        /// </summary>
        private float s = -1.0f;
        
        /// <summary>
        /// Color's value
        /// </summary>
        private float v = -1.0f;

        /// <summary>
        /// Event invoked when the hue changes
        /// </summary>
        public event Action<float> HueChanged;

        /// <summary>
        /// Event invoked when the saturation changes
        /// </summary>
        public event Action<float> SatChanged;

        /// <summary>
        /// Event invoked when the value changes
        /// </summary>
        public event Action<float> ValChanged;

        /// <summary>
        /// Color's hue
        /// </summary>
        public float H
        {
            get => h;
            set
            {
                if (Mathf.Approximately(h, value))
                {
                    h = value;
                    return;
                }

                h = value;
                HueChanged?.Invoke(h);
            }
        }

        /// <summary>
        /// Color's saturation
        /// </summary>
        public float S
        {
            get => s;
            set
            {
                if (Mathf.Approximately(s, value))
                {
                    s = value;
                    return;
                }

                s = value;
                SatChanged?.Invoke(s);
            }
        }

        /// <summary>
        /// Color's value
        /// </summary>
        public float V
        {
            get => v;
            set
            {
                if (Mathf.Approximately(v, value))
                {
                    v = value;
                    return;
                }

                v = value;
                ValChanged?.Invoke(v);
            }
        }

        /// <summary>
        /// Sets the hue, saturation, and value basing on the passed RGB color
        /// </summary>
        public void FromRGB(RGBColor rgbColor)
        {
            var min = rgbColor.R < rgbColor.G ? rgbColor.R : rgbColor.G;
            min = min < rgbColor.B ? min : rgbColor.B;

            var max = rgbColor.R > rgbColor.G ? rgbColor.R : rgbColor.G;
            max = max > rgbColor.B ? max : rgbColor.B;

            V = max;
            var delta = max - min;
            if (delta < 0.00001f)
            {
                S = 0.0f;
                H = 0.0f;
                return;
            }

            if (max > 0.0f)
            {
                S = (delta / Mathf.Max(max, Mathf.Epsilon));
            }
            else
            {
                S = 0.0f;
                H = float.NaN;
                return;
            }

            if (rgbColor.R >= max)
                H = (rgbColor.G - rgbColor.B) / delta;
            else if (rgbColor.G >= max)
                H = 2.0f + (rgbColor.B - rgbColor.R) / delta;
            else
                H = 4.0f + (rgbColor.R - rgbColor.G) / delta; // between magenta & cyan

            H *= 60.0f;

            if (H < 0.0f)
                H += 360.0f;
            H /= 360.0f;
        }
    }
}