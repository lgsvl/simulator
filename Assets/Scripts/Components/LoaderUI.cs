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

public class LoaderUI : MonoBehaviour
{
    public CanvasScaler BGCanvasScaler;
    public RectTransform bgCanvasRT;
    public Image BackgroundImage;
    public List<Sprite> BGSprites { get; set; } = new List<Sprite>();
    public Button StartButton;
    public Text StartButtonText;

    private string origStartButtonText;
    private float fadeTime = 3f;
    private bool fading = true;
    private int currentBGImageIndex = -1;

    public enum LoaderUIStateType { START, PROGRESS, READY };
    public LoaderUIStateType LoaderUIState = LoaderUIStateType.START;

    // TODO set BackgroundImage.overrideSprite to sprite after Unity update https://issuetracker.unity3d.com/issues/image-color-cannot-be-changed-via-script-when-image-type-is-set-to-simple

    private void Start()
    {
        origStartButtonText = StartButtonText.text;
        bgCanvasRT = BGCanvasScaler.GetComponent<RectTransform>();
        fading = true;
        currentBGImageIndex = -1;
        StopAllCoroutines();

        // TODO get bgSprites from DB
        if (BGSprites.Count > 0)
            StartCoroutine(BGFadeSwitch());

        StartButton.onClick.AddListener(OnStartButtonClick);
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
        StartButton.onClick.RemoveListener(OnStartButtonClick);
    }
    
    private void OnStartButtonClick()
    {
        Application.OpenURL(Loader.Instance.Address + "/");
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
                StartButton.interactable = true;
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
}
