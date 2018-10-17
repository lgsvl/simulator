using UnityEngine;

public class DepthCameraEnabler : MonoBehaviour
{
    public DepthCamera Camera;

    public RenderTextureDisplayer TextureDisplay;

    Camera PreviousCamera;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            bool active = Camera.gameObject.activeSelf;
            Camera.gameObject.SetActive(!active);

            if (active)
            {
                int w = TextureDisplay.renderCamera.targetTexture.width;
                int h = TextureDisplay.renderCamera.targetTexture.height;
                int d = TextureDisplay.renderCamera.targetTexture.depth;
                var f = TextureDisplay.renderCamera.targetTexture.format;
                TextureDisplay.renderCamera = PreviousCamera;
                TextureDisplay.renderCamera.targetTexture = new RenderTexture(w, h, d, f);
            }
            else
            {
                PreviousCamera = TextureDisplay.renderCamera;
                TextureDisplay.renderCamera = Camera.GetComponent<Camera>();
                TextureDisplay.renderCamera.targetTexture = new RenderTexture(PreviousCamera.targetTexture.width, PreviousCamera.targetTexture.height, PreviousCamera.targetTexture.depth, PreviousCamera.targetTexture.format);
            }
        }
    }
}
