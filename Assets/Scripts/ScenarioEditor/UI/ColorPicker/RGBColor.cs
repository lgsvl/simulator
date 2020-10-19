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
    /// Color in the RGB format
    /// </summary>
    public class RGBColor
    {
        /// <summary>
        /// Current color
        /// </summary>
        private Color color = new Color(-1.0f, -1.0f, -1.0f);

        /// <summary>
        /// Event invoked when the red changes
        /// </summary>
        public event Action<float> RedChanged;
        
        /// <summary>
        /// Event invoked when the green changes
        /// </summary>
        public event Action<float> GreenChanged;
        
        /// <summary>
        /// Event invoked when the blue changes
        /// </summary>
        public event Action<float> BlueChanged;

        /// <summary>
        /// Current color
        /// </summary>
        public Color CurrentColor
        {
            get => color;
            set
            {
                R = value.r;
                G = value.g;
                B = value.b;
            }
        }

        /// <summary>
        /// Color's red intensity
        /// </summary>
        public float R
        {
            get => color.r;
            set
            {
                if (Mathf.Approximately(color.r, value))
                {
                    color.r = value;
                    return;
                }

                color.r = value;
                RedChanged?.Invoke(color.r);
            }
        }

        /// <summary>
        /// Color's green intensity
        /// </summary>
        public float G
        {
            get => color.g;
            set
            {
                if (Mathf.Approximately(color.g, value))
                {
                    color.g = value;
                    return;
                }

                color.g = value;
                GreenChanged?.Invoke(color.g);
            }
        }

        /// <summary>
        /// Color's blue intensity
        /// </summary>
        public float B
        {
            get => color.b;
            set
            {
                if (Mathf.Approximately(color.b, value))
                {
                    color.b = value;
                    return;
                }

                color.b = value;
                BlueChanged?.Invoke(color.b);
            }
        }

        /// <summary>
        /// Sets the red, green, and blue intensity basing on the passed HSV color
        /// </summary>
        public void FromHSV(HSVColor hsvColor)
        {
            if (hsvColor.S <= 0.0)
            {
                R = hsvColor.V;
                G = hsvColor.V;
                B = hsvColor.V;
                return;
            }

            var hh = hsvColor.H * 360.0f;
            if (hh >= 360.0) hh = 0.0f;
            hh /= 60.0f;
            var i = (int) hh;
            var ff = hh - i;
            var p = hsvColor.V * (1.0f - hsvColor.S);
            var q = hsvColor.V * (1.0f - (hsvColor.S * ff));
            var t = hsvColor.V * (1.0f - (hsvColor.S * (1.0f - ff)));

            switch (i)
            {
                case 0:
                    R = hsvColor.V;
                    G = t;
                    B = p;
                    break;
                case 1:
                    R = q;
                    G = hsvColor.V;
                    B = p;
                    break;
                case 2:
                    R = p;
                    G = hsvColor.V;
                    B = t;
                    break;

                case 3:
                    R = p;
                    G = q;
                    B = hsvColor.V;
                    break;
                case 4:
                    R = t;
                    G = p;
                    B = hsvColor.V;
                    break;
                case 5:
                default:
                    R = hsvColor.V;
                    G = p;
                    B = q;
                    break;
            }
        }
    }
}