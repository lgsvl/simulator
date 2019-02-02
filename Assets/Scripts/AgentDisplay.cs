/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;
using UnityEngine.UI;

public class AgentDisplay : MonoBehaviour
{
    public RawImage RawImage;
    RfbClient Rfb;

    void Start()
    {
        Rfb = GetComponent<RfbClient>();
        Rfb.OnTextureCreated += texture =>
        {
            RawImage.texture = texture;
        };
    }

    void Update()
    {
        RawImage.gameObject.SetActive(Rfb.IsConnected);
    }
}
