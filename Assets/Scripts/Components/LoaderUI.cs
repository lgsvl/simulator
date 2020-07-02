/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using UnityEngine;
using Simulator;
using Simulator.Web;
using System.Linq;

public class LoaderUI : MonoBehaviour
{
    public CanvasScaler BGCanvasScaler;
    public RectTransform bgCanvasRT;
    public Image BackgroundImage;
    public List<Sprite> BGSprites { get; set; } = new List<Sprite>();
    public Button StartButton;
    public Text StartButtonText;

    public GameObject SettingsPanel;
    public GameObject SettingsButton;
    public Dropdown FullscreenDropdown;
    public Dropdown ResolutionDropdown;
    public Dropdown QualityDropdown;
    private Resolution[] Resolutions;

    private string origStartButtonText;
    private float fadeTime = 3f;
    private bool fading = true;
    private int currentBGImageIndex = -1;

    public enum LoaderUIStateType { START, PROGRESS, READY };
    public LoaderUIStateType LoaderUIState = LoaderUIStateType.START;

    // TODO set BackgroundImage.overrideSprite to sprite after Unity update https://issuetracker.unity3d.com/issues/image-color-cannot-be-changed-via-script-when-image-type-is-set-to-simple

    private void Start()
    {
        if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Linux)
        {
            SettingsPanel.SetActive(false);
            SettingsButton.SetActive(false);
        }
        else
        {
            SetDropdowns();
        }

        origStartButtonText = StartButtonText.text;
        bgCanvasRT = BGCanvasScaler.GetComponent<RectTransform>();
        fading = true;
        currentBGImageIndex = -1;
        StopAllCoroutines();

        // TODO get bgSprites from DB
        if (BGSprites.Count > 0)
            StartCoroutine(BGFadeSwitch());

        SetLoaderUIState(LoaderUIStateType.START);
    }

    private void Update()
    {
        var currentRatio = bgCanvasRT.rect.width / bgCanvasRT.rect.height;
        var desiredRatio = BackgroundImage.sprite.bounds.size.x / BackgroundImage.sprite.bounds.size.y;

        if (currentRatio > desiredRatio)
            BGCanvasScaler.matchWidthOrHeight = 0f;
        else if (currentRatio < desiredRatio)
            BGCanvasScaler.matchWidthOrHeight = 1f;
        else
            BGCanvasScaler.matchWidthOrHeight = 0.5f;
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    private IEnumerator BGFadeSwitch()
    {
        while (true)
        {
            if (fading)
                yield return new WaitForSeconds(Random.Range(5f, 10f));

            var elapsedTime = 0f;
            var colorStart = BackgroundImage.color;
            while (elapsedTime < fadeTime)
            {
                BackgroundImage.color = Color.Lerp(colorStart, fading ? Color.black : Color.white, elapsedTime / fadeTime);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            BackgroundImage.color = fading ? Color.black : Color.white;
            BackgroundImage.overrideSprite = fading ? GetBGImageSprite() : BackgroundImage.overrideSprite;
            fading = !fading;
        }
    }
    
    public void SetLoaderUIState(LoaderUIStateType state)
    {
        LoaderUIState = state;
        switch (LoaderUIState)
        {
            case LoaderUIStateType.START:
                if (Config.RunAsMaster)
                    StartButton.interactable = true;
                else
                {
                    StartButton.interactable = false;
                    StartButtonText.text = "Client ready";
                }
                break;
            case LoaderUIStateType.PROGRESS:
                StartButton.interactable = false;
                StartButtonText.text = "Loading...";
                break;
            case LoaderUIStateType.READY:
                StartButton.interactable = false;
                StartButtonText.text = "API ready!";
                break;
        }
    }
    
    private Sprite GetBGImageSprite()
    {
        currentBGImageIndex = currentBGImageIndex < BGSprites.Count - 1 ? currentBGImageIndex + 1 : 0;
        return BGSprites[currentBGImageIndex];
    }

    public void EnableUI()
    {
        gameObject.SetActive(true);
    }

    public void DisableUI()
    {
        gameObject.SetActive(false);
    }

    public void SetResolution(int index)
    {
        Resolution resolution = Resolutions[index];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

    public void SetGraphics(int index)
    {
        QualitySettings.SetQualityLevel(index, true);
    }

    public void SetFullscreen(int index)
    {
        if (index == 0)
        {
            Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.FullScreenWindow);
        }
        else
        {
            Screen.SetResolution(Screen.currentResolution.width / 2, Screen.currentResolution.height / 2, FullScreenMode.Windowed);
        }
    }

    private void SetDropdowns()
    {
        SettingsPanel.SetActive(false);

        FullscreenDropdown.value = Screen.fullScreen ? 0 : 1;
        FullscreenDropdown.RefreshShownValue();

        Resolutions = Screen.resolutions.Select(resolution => new Resolution { width = resolution.width, height = resolution.height }).Distinct().ToArray();
        Resolutions = Screen.resolutions;
        ResolutionDropdown.ClearOptions();
        var options = new List<string>();
        for (int i = 0; i < Resolutions.Length; i++)
        {
            var option = $"{Resolutions[i].width} x {Resolutions[i].height} {Resolutions[i].refreshRate}Hz";
            options.Add(option);
        }
        ResolutionDropdown.AddOptions(options);

        QualityDropdown.ClearOptions();
        QualityDropdown.AddOptions(QualitySettings.names.ToList());
        QualityDropdown.value = QualitySettings.GetQualityLevel();
        QualityDropdown.RefreshShownValue();
    }

    public void EnterScenarioEditor()
    {
        Loader.EnterScenarioEditor();
    }
}
