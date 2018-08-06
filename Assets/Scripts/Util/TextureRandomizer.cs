using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureRandomizer : MonoBehaviour
{
    public bool ramdomizeOnStart;
    enum TexType { Albedo, Emission, }
    TexType texType = TexType.Albedo;

    public Renderer editRenderer;
    public int materialIndex;
    public List<Texture2D> textures;

    void Start()
    {
        if (ramdomizeOnStart)
        {
            RandomSelectTexture();
        }
    }

    public void RandomSelectTexture()
    {
        int index = Random.Range(0, textures.Count);
        if (texType == TexType.Albedo)
        {
            editRenderer.materials[0].SetColor("_Color", Color.white);
            editRenderer.materials[0].SetTexture("_MainTex", textures[index]);
        }
    }
}
