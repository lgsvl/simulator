/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ReturnScript : MonoBehaviour
{
    public GameObject menuParent;
    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            menuParent.SetActive(false);
        });
    }
}
