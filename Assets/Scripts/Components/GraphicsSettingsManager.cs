/**
 * Copyright (c) 2020-2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using UnityEngine;
using YamlDotNet.Serialization;
using System.IO;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class GraphicsSettingsManager : MonoBehaviour
{
    public static GraphicsSettingsManager Instance;

    public enum Preset
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Custom = 3
    };

    public class Config
    {
        public int ScreenWidth = 0;
        public int ScreenHeight = 0;
        public bool Fullscreen = false;
        public int Preset = 2;
        public bool Shadows = true;
        public bool AmbientOcclusion = true;
        public bool Antialiasing = true;
        public bool MotionBlur = true;
        public bool Scattering = true;
        public bool Volumetrics = true;
    }

    public Config Cfg = new Config();
    public static ref Config GetCfg() => ref Instance.Cfg;

    void Start()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(this);
        Instance = this;

        LoadPrefsConfig();

        if (Cfg.ScreenWidth > 0 && Cfg.ScreenHeight > 0)
        {
            Screen.SetResolution(Cfg.ScreenWidth, Cfg.ScreenHeight, Cfg.Fullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed);
        }

        RenderPipelineManager.beginFrameRendering += RenderBeginFrame;        
    }

    public void RenderBeginFrame( ScriptableRenderContext ctx, Camera[] cams )
    {
        HDRenderPipeline hdrp = RenderPipelineManager.currentPipeline as HDRenderPipeline;
        if (hdrp != null)
        {
            ApplyCustomSettingsChanges(false);
            RenderPipelineManager.beginFrameRendering -= RenderBeginFrame;
        }
    }

    public void SetQualityPreset( int value )
    {
        if (value < 0 || value > (int)Preset.Custom)
            return;

        switch ((Preset)value)
        {
            case Preset.Low:
                {
                    Cfg.Shadows = false;
                    Cfg.AmbientOcclusion = false;
                    Cfg.Antialiasing = false;
                    Cfg.MotionBlur = false;
                    Cfg.Scattering = false;
                    Cfg.Volumetrics = false;
                }
                break;
            case Preset.Medium:
                {
                    Cfg.Shadows = true;
                    Cfg.AmbientOcclusion = false;
                    Cfg.Antialiasing = false;
                    Cfg.MotionBlur = true;
                    Cfg.Scattering = true;
                    Cfg.Volumetrics = false;
                }
                break;
            case Preset.High:
                {
                    Cfg.Shadows = true;
                    Cfg.AmbientOcclusion = true;
                    Cfg.Antialiasing = true;
                    Cfg.MotionBlur = true;
                    Cfg.Scattering = true;
                    Cfg.Volumetrics = true;
                }
                break;
        }

        Cfg.Preset = value;

        if ((Preset)value != Preset.Custom)
        {
            ApplyCustomSettingsChanges(true);
        }
        else
        {
            SaveConfig();
        }
    }

    public void ApplyCustomSettingsChanges( bool saveConfig )
    {
        HDRenderPipeline hdrp = RenderPipelineManager.currentPipeline as HDRenderPipeline;
        if (hdrp != null)
        {
            ref FrameSettings fs = ref hdrp.GetDefaultCameraFrameSettings();

            fs.SetEnabled(FrameSettingsField.ShadowMaps, Cfg.Shadows);
            fs.SetEnabled(FrameSettingsField.Antialiasing, Cfg.Antialiasing);
            fs.SetEnabled(FrameSettingsField.SSAO, Cfg.AmbientOcclusion);
            fs.SetEnabled(FrameSettingsField.SubsurfaceScattering, Cfg.Scattering);
            fs.SetEnabled(FrameSettingsField.Volumetrics, Cfg.Volumetrics);
            fs.SetEnabled(FrameSettingsField.MotionVectors, Cfg.MotionBlur);
        }

        if (saveConfig != false)
        {
            SaveConfig();
        }
    }

    private struct PrefsKey
    {
        public const string ScreenWidth      = "Gfx_ScreenWidth";
        public const string ScreenHeight     = "Gfx_ScreenHeight";
        public const string Fullscreen       = "Gfx_Fullscreen";
        public const string Preset           = "Gfx_Preset";
        public const string Shadows          = "Gfx_Shadows";
        public const string AmbientOcclusion = "Gfx_AO";
        public const string Antialiasing     = "Gfx_AA";
        public const string Motionblur       = "Gfx_Motionblur";
        public const string Scattering       = "Gfx_Scattering";
        public const string Volumetrics      = "Gfx_Volumetrics";
    };

    private void LoadPrefsConfig()
    {
        Cfg.ScreenWidth  = PlayerPrefs.GetInt(PrefsKey.ScreenWidth, 0);
        Cfg.ScreenHeight = PlayerPrefs.GetInt(PrefsKey.ScreenHeight, 0);
        Cfg.Fullscreen   = PlayerPrefs.GetInt(PrefsKey.Fullscreen, 0) != 0;
        Cfg.Preset       = PlayerPrefs.GetInt(PrefsKey.Preset,3);
        
        Cfg.Shadows          = PlayerPrefs.GetInt(PrefsKey.Shadows,1) != 0;
        Cfg.AmbientOcclusion = PlayerPrefs.GetInt(PrefsKey.AmbientOcclusion,1) != 0;
        Cfg.Antialiasing     = PlayerPrefs.GetInt(PrefsKey.Antialiasing,1) != 0;
        Cfg.MotionBlur       = PlayerPrefs.GetInt(PrefsKey.Motionblur,1) != 0;
        Cfg.Scattering       = PlayerPrefs.GetInt(PrefsKey.Scattering,1) != 0;
        Cfg.Volumetrics      = PlayerPrefs.GetInt(PrefsKey.Volumetrics,1) != 0;
    }

    public void SaveConfig()
    {
        PlayerPrefs.SetInt(PrefsKey.ScreenWidth, Cfg.ScreenWidth);
        PlayerPrefs.SetInt(PrefsKey.ScreenHeight, Cfg.ScreenHeight);
        PlayerPrefs.SetInt(PrefsKey.Fullscreen, Cfg.Fullscreen ? 1 : 0);
        PlayerPrefs.SetInt(PrefsKey.Preset, Cfg.Preset);

        PlayerPrefs.SetInt(PrefsKey.Shadows, Cfg.Shadows ? 1 : 0);
        PlayerPrefs.SetInt(PrefsKey.AmbientOcclusion, Cfg.AmbientOcclusion ? 1 : 0);
        PlayerPrefs.SetInt(PrefsKey.Antialiasing, Cfg.Antialiasing ? 1 : 0);
        PlayerPrefs.SetInt(PrefsKey.Motionblur, Cfg.MotionBlur ? 1 : 0);
        PlayerPrefs.SetInt(PrefsKey.Scattering, Cfg.Scattering ? 1 : 0);
        PlayerPrefs.SetInt(PrefsKey.Volumetrics, Cfg.Volumetrics ? 1 : 0);

        PlayerPrefs.Save();
    }
}
