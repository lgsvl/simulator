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

    private void Awake()
    {
        thisRenderer = GetComponent<Renderer>();
        thisMaterial = thisRenderer.material;
        initColor = thisMaterial.GetColor("_TintColor");
    }

    private void Start()
    {
        StartCoroutine(FadeMaterialAlpha());
    }

    IEnumerator FadeMaterialAlpha()
    {
        yield return new WaitForSeconds(Random.Range(0f, 25f));

        float elapsedTime = 0f;
        float randomTime = Random.Range(25f, 100f);
        while (elapsedTime < randomTime)
        {
            thisRenderer.material.SetColor("_TintColor", Color.Lerp(initColor, fadeColor, (elapsedTime / randomTime)));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        elapsedTime = 0f;
        randomTime = Random.Range(25f, 100f);
        while (elapsedTime < randomTime)
        {
            thisRenderer.material.SetColor("_TintColor", Color.Lerp(fadeColor, initColor, (elapsedTime / randomTime)));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        StartCoroutine(FadeMaterialAlpha());
    }
}
