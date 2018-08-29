using System.Collections;
using UnityEngine;

public class SegmentColorer : MonoBehaviour
{
    public GameObject[] Cars;
    public GameObject[] Roads;
    public GameObject[] Trees;
    public GameObject[] Sidewalks;
    public GameObject[] Buildings;
    public GameObject[] Signs;
    public GameObject[] TrafficLights;
    public GameObject[] Obstacles;
    public GameObject[] Shoulders;

    public Color SkyColor = new Color32(0xB5, 0xC2, 0xD9, 255);

    public Camera Camera;
    public Shader Shader;

    void Start()
    {
        if (Camera == null)
        {
            return;
        }

        OverrideMaterials(Cars, "Car");
        OverrideMaterials(Roads, "Road");
        OverrideMaterials(Trees, "Tree");
        OverrideMaterials(Sidewalks, "Sidewalk");
        OverrideMaterials(Obstacles, "Obstacle");
        OverrideMaterials(Buildings, "Building");
        OverrideMaterials(Signs, "Sign");
        OverrideMaterials(TrafficLights, "TrafficLight");
        OverrideMaterials(Shoulders, "Shoulder");

        Camera.SetReplacementShader(Shader, "SegmentColor");
        Camera.backgroundColor = SkyColor;
        Camera.clearFlags = CameraClearFlags.SolidColor;
        Camera.renderingPath = RenderingPath.Forward;

        Camera.GetComponent<PostProcessingListener>().enabled = false;
        Camera.GetComponent<UnityEngine.PostProcessing.PostProcessingBehaviour>().enabled = false;
    }

    void OverrideMaterials(GameObject[] objects, string tag)
    {
        foreach (var obj in objects)
        {
            foreach (var renderer in obj.GetComponentsInChildren<Renderer>())
            {
                foreach (var mat in renderer.sharedMaterials)
                {
                    mat.SetOverrideTag("SegmentColor", tag);
                }
            }
        }
    }
}
