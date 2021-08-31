/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Simulator.Web;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class SettingsWindow : MonoBehaviour
{
    public Dropdown ResolutionDropdown;
    public Dropdown FullscreenDropdown;
    public Dropdown PresetDropdown;

    public Dropdown   SettingShadows;
    public Dropdown   SettingAO;
    public Dropdown   SettingAntialiasing;
    public Dropdown   SettingMotionBlur;
    public Dropdown   SettingScattering;
    public Dropdown   SettingVolumetrics;
    public GameObject SettingTurnoff;

    private Resolution[] Resolutions;
    private bool RefreshingValues = false;
              
    void OnEnable()
    {
        RefreshingValues = true;

        if ( FullscreenDropdown != null )
        {
            if (Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen || Screen.fullScreenMode == FullScreenMode.FullScreenWindow)
                FullscreenDropdown.value = 0;
            else
                FullscreenDropdown.value = 1;
        }

        if (ResolutionDropdown != null)
        {
            ResolutionDropdown.ClearOptions();
                        
            int currentResolution = -1;

            List<string> resdefines = new List<string>();
            Resolutions = new Resolution[Screen.resolutions.Length];
            int index = 0;
                        
            foreach (var res in Screen.resolutions)
            {
                if (res.width < 1024 || res.height < 768)
                    continue;

                Resolutions[index++] = res;
                resdefines.Add(res.ToString());

                if (currentResolution == -1 && Screen.width == res.width && Screen.height == res.height)
                    currentResolution = resdefines.Count - 1;
            }

            ResolutionDropdown.AddOptions(resdefines);            

            if ( currentResolution != -1 )
            {
                ResolutionDropdown.value = currentResolution;
            }
        }

        if ( PresetDropdown != null )
        {
            PresetDropdown.value = GraphicsSettingsManager.GetCfg().Preset;
            EnableCustomGfxSettings(PresetDropdown.value == (int)GraphicsSettingsManager.Preset.Custom);
        }
                
        RefreshSettingsDropdowns();

        RefreshingValues = false;
    }

    void EnableCustomGfxSettings( bool onOff )
    {
        if (SettingTurnoff != null)
        {
            SettingTurnoff.SetActive(!onOff);
        }
    }

    void RefreshSettingsDropdowns()
    {
        bool wasRefreshingValues = RefreshingValues;
        RefreshingValues = true;
                
        SettingShadows.value = GraphicsSettingsManager.GetCfg().Shadows ? 1 : 0;
        SettingAO.value = GraphicsSettingsManager.GetCfg().AmbientOcclusion ? 1 : 0;
        SettingAntialiasing.value = GraphicsSettingsManager.GetCfg().Antialiasing ? 1 : 0;
        SettingMotionBlur.value = GraphicsSettingsManager.GetCfg().MotionBlur ? 1 : 0;
        SettingScattering.value = GraphicsSettingsManager.GetCfg().Scattering ? 1 : 0;
        SettingVolumetrics.value = GraphicsSettingsManager.GetCfg().Volumetrics ? 1 : 0;

        RefreshingValues = wasRefreshingValues;
    }

    public void OnResolutionChanged( int value )
    {
        if (value < 0 || Resolutions == null || value >= Resolutions.Length || RefreshingValues != false)
            return;

        Resolution r = Resolutions[value];
        Screen.SetResolution(r.width, r.height, GraphicsSettingsManager.Instance.Cfg.Fullscreen);

        GraphicsSettingsManager.GetCfg().ScreenWidth = r.width;
        GraphicsSettingsManager.GetCfg().ScreenHeight = r.height;
        GraphicsSettingsManager.Instance.SaveConfig();        
    }

    public void OnFullscreenChanged( int value )
    {
        if (RefreshingValues != false)
            return;
             
        if ( value == 0 )
        {
            Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;

            GraphicsSettingsManager.GetCfg().Fullscreen = true;
            GraphicsSettingsManager.Instance.SaveConfig();
        }
        else
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;

            GraphicsSettingsManager.GetCfg().Fullscreen = false;
            GraphicsSettingsManager.Instance.SaveConfig();
        }
    }

    public void OnGraphicsPresetChanged( int value )
    {
        if (RefreshingValues != false)
            return;

        GraphicsSettingsManager.Instance.SetQualityPreset(value);
        EnableCustomGfxSettings(value == (int)GraphicsSettingsManager.Preset.Custom);
        RefreshSettingsDropdowns();
    }

    public void OnShadowsSettingChanged( int value )
    {
        if ( RefreshingValues == false )
        {
            GraphicsSettingsManager.GetCfg().Shadows = value != 0;
            GraphicsSettingsManager.Instance.ApplyCustomSettingsChanges(true);
        }
    }

    public void OnAmbientOcclusionSettingChanged( int value )
    {
        if ( RefreshingValues == false )
        {
            GraphicsSettingsManager.GetCfg().AmbientOcclusion = value != 0;
            GraphicsSettingsManager.Instance.ApplyCustomSettingsChanges(true);
        }
    }

    public void OnAntialiasingSettingChanged( int value )
    {
        if ( RefreshingValues == false )
        {
            GraphicsSettingsManager.GetCfg().Antialiasing = value != 0;
            GraphicsSettingsManager.Instance.ApplyCustomSettingsChanges(true);
        }
    }

    public void OnMotionblurSettingChanged( int value )
    {
        if ( RefreshingValues == false )
        {
            GraphicsSettingsManager.GetCfg().MotionBlur = value != 0;
            GraphicsSettingsManager.Instance.ApplyCustomSettingsChanges(true);
        }
    }
    
    public void OnScatteringSettingChanged( int value )
    {
        if (RefreshingValues == false)
        {
            GraphicsSettingsManager.GetCfg().Scattering = value != 0;
            GraphicsSettingsManager.Instance.ApplyCustomSettingsChanges(true);
        }
    }

    public void OnVolumetricsSettingChanged( int value )
    {
        if (RefreshingValues == false)
        {
            GraphicsSettingsManager.GetCfg().Volumetrics = value != 0;
            GraphicsSettingsManager.Instance.ApplyCustomSettingsChanges(true);
        }
    }
}
