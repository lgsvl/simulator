using UnityEngine;

[RequireComponent(typeof(Camera))]
public class DepthCamera : MonoBehaviour
{
    public Shader Shader;

    Camera Camera;

    void Start()
    {
        Camera = GetComponent<Camera>();
        Camera.SetReplacementShader(Shader, "");
        Camera.renderingPath = RenderingPath.Forward;
    }
}
