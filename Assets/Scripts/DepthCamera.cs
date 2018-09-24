using UnityEngine;

[RequireComponent(typeof(Camera))]
public class DepthCamera : MonoBehaviour
{
    public Shader Shader;

    void Start()
    {
        var camera = GetComponent<Camera>();
        camera.SetReplacementShader(Shader, "");
        camera.renderingPath = RenderingPath.Forward;
    }
}
