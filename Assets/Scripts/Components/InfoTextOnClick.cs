/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */
 
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class InfoTextOnClick : MonoBehaviour
{
    public string SubString { get; set; }
    public bool BuildInfo { get; set; }
    private Text InfoText;
    private Button TextButton;
    private bool SubTextActive = false;

    private void Awake()
    {
        InfoText = GetComponent<Text>();
        TextButton = GetComponent<Button>();
        SubTextActive = false;
    }

    private void OnEnable()
    {
        TextButton.onClick.AddListener(TextButtonOnClick);
    }

    private void OnDisable()
    {
        TextButton.onClick.RemoveListener(TextButtonOnClick);
    }

    private void TextButtonOnClick()
    {
        if (!string.IsNullOrEmpty(SubString))
        {
            var sb = new StringBuilder();
            sb.Append(InfoText.text);
            if (SubTextActive)
            {
                InfoText.text = sb.Replace("\n\n" + SubString, "").ToString();
            }
            else
            {
                InfoText.text = sb.Append("\n\n" + SubString).ToString();
            }
            SubTextActive = !SubTextActive;
        }
    }
}
