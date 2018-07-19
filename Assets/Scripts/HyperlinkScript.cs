using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HyperlinkScript : MonoBehaviour
{
    public string hyperlink;
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            Application.OpenURL(hyperlink);
        });
    }
}
