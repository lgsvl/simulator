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
    public Shader Shader;

    void Start()
    {
        OverrideMaterials(Cars, "Car");
        OverrideMaterials(Roads, "Road");
        OverrideMaterials(Trees, "Tree");
        OverrideMaterials(Sidewalks, "Sidewalk");
        OverrideMaterials(Obstacles, "Obstacle");
        OverrideMaterials(Buildings, "Building");
        OverrideMaterials(Signs, "Sign");
        OverrideMaterials(TrafficLights, "TrafficLight");
        OverrideMaterials(Shoulders, "Shoulder");
    }

    private void OnDisable()
    {
        RemoveOverrideMaterials(Cars);
        RemoveOverrideMaterials(Roads);
        RemoveOverrideMaterials(Trees);
        RemoveOverrideMaterials(Sidewalks);
        RemoveOverrideMaterials(Obstacles);
        RemoveOverrideMaterials(Buildings);
        RemoveOverrideMaterials(Signs);
        RemoveOverrideMaterials(TrafficLights);
        RemoveOverrideMaterials(Shoulders);
    }

    void OverrideMaterials(GameObject[] objects, string tag)
    {
        foreach (var obj in objects)
        {
            if (obj == null) return;

            foreach (var renderer in obj.GetComponentsInChildren<Renderer>())
            {
                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat != null)
                    {
                        mat.SetOverrideTag("SegmentColor", tag);
                    }
                }
            }
        }
    }

    private void RemoveOverrideMaterials(GameObject[] objects)
    {
        foreach (var obj in objects)
        {
            if (obj == null) return;

            foreach (var renderer in obj.GetComponentsInChildren<Renderer>())
            {
                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat != null)
                    {
                        mat.SetOverrideTag("SegmentColor", "");
                    }
                }
            }
        }
    }

    public void ApplyToCamera(Camera camera)
    {
        camera.SetReplacementShader(Shader, "SegmentColor");
        camera.backgroundColor = SkyColor;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.renderingPath = RenderingPath.Forward;
    }
}
