using UnityEngine;

public class DepthCameraEnabler : MonoBehaviour
{
    public DepthCamera Camera;

    public RenderTextureDisplayer TextureDisplay;

    Camera PreviousCamera;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            bool active = Camera.gameObject.activeSelf;
            Camera.gameObject.SetActive(!active);

            if (active)
            {
                TextureDisplay.renderCamera = PreviousCamera;
            }
            else
            {
                PreviousCamera = TextureDisplay.renderCamera;
                TextureDisplay.renderCamera = Camera.GetComponent<Camera>();
            }
        }
    }
}
