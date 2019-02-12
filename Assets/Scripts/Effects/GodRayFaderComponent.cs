using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GodRayFaderComponent : MonoBehaviour
{
    public Color fadeColor;
    private Color initColor;
    private Color tempColor;
    private Renderer thisRenderer;
    private Material thisMaterial;
    private float fadeTime;

    bool isNight = false;

    private void Awake()
    {
        thisRenderer = GetComponent<Renderer>();
        thisMaterial = thisRenderer.material;
        initColor = thisMaterial.GetColor("_TintColor");
    }

    private void OnEnable()
    {
        Missive.AddListener<DayNightMissive>(OnDayNightChange);
    }

    private void OnDisable()
    {
        Missive.RemoveListener<DayNightMissive>(OnDayNightChange);
        thisRenderer.material.SetColor("_TintColor", initColor);
    }

    private void OnDayNightChange(DayNightMissive missive)
    {
        switch (missive.state)
        {
            case DayNightStateTypes.Day:
                OnDay();
                break;
            case DayNightStateTypes.Night:
                OnNight();
                break;
            case DayNightStateTypes.Sunrise:
                OnDay();
                break;
            case DayNightStateTypes.Sunset:
                OnNight();
                break;
            default:
                break;
        }
    }

    private void Start()
    {
        StartCoroutine(FadeMaterialAlpha());
    }

    void OnNight()
    {
        isNight = true;
        thisRenderer.material.SetColor("_TintColor", Color.black);
    }

    void OnDay()
    {
        isNight = false;
        thisRenderer.material.SetColor("_TintColor", initColor);
    }

    IEnumerator FadeMaterialAlpha()
    {
        yield return new WaitForSeconds(Random.Range(0f, 25f));

        float elapsedTime = 0f;
        float randomTime = Random.Range(25f, 100f);
        while (elapsedTime < randomTime)
        {
            var color = isNight ? Color.black : Color.Lerp(initColor, fadeColor, (elapsedTime / randomTime));
            thisRenderer.material.SetColor("_TintColor", color);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        elapsedTime = 0f;
        randomTime = Random.Range(25f, 100f);
        while (elapsedTime < randomTime)
        {
            var color = isNight ? Color.black : Color.Lerp(fadeColor, initColor, (elapsedTime / randomTime));
            thisRenderer.material.SetColor("_TintColor", color);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        StartCoroutine(FadeMaterialAlpha());
    }
}
