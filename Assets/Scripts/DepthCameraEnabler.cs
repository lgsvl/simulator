using UnityEngine;

public class DepthCameraEnabler : MonoBehaviour
{
    public DepthCamera Camera;

    public UnityEngine.UI.RawImage TextureView;

    Texture PreviousTexture;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            bool active = Camera.gameObject.activeSelf;
            TextureView.gameObject.GetComponent<RenderTextureDisplayer>().enabled = active;
            if (active)
            {
                TextureView.texture = PreviousTexture;
            }
            else
            {
                PreviousTexture = TextureView.texture;
                TextureView.texture = Camera.gameObject.GetComponent<Camera>().targetTexture;
            }
            Camera.gameObject.SetActive(!active);
            TextureView.gameObject.SetActive(!active);
        }
    }
}
