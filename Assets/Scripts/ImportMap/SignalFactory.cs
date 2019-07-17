using UnityEngine;
using apollo.common;
using System.Collections.Generic;
using Vector3 = UnityEngine.Vector3;

public class SignalFactory : MonoBehaviour
{
    public static SignalFactory instance;

    private TransformHelper transformHelper = new TransformHelper();
    /*
     * prefabs
     * 
     */
    public GameObject greenTrafficLight;
    public GameObject redTrafficLight;
    public GameObject trafficCone;

    public List<GameObject> trafficLights;


    private void Awake()
    {
        instance = this;
    }

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private Vector3 GetSignalPosition(PointENU location)
    {
        // 因为右手坐标系，旋转的是模型，所以要用y替代z
        return new Vector3(
            (float)location.x, 
            (float)location.z, 
            (float)location.y);
    }

    private void CreateTrafficLightPolyon(List<PointENU> points)
    {
        GameObject _area = new GameObject("TrafficLightBoundary");

        //Debug.LogError("TrafficLightPolyon: " + points.Count);

        // 1. Mesh
        Mesh mesh = new Mesh
        {
            vertices = new Vector3[] {
                new Vector3((float)points[0].x, (float)points[0].z, (float)points[0].y),
                new Vector3((float)points[1].x, (float)points[1].z, (float)points[1].y),
                new Vector3((float)points[2].x, (float)points[2].z, (float)points[2].y),
                new Vector3((float)points[3].x, (float)points[3].z, (float)points[3].y)
            },
            triangles = new int[] { 0, 1, 2, 0, 2, 3 }
        };

        _area.AddComponent<MeshFilter>().sharedMesh = mesh;
        _area.AddComponent<MeshCollider>().sharedMesh = mesh;

        // 3. Transform
        //_area.transform.Rotate(new Vector3(90, 0, 0));
    }

    public List<GameObject> CreateTrafficLight(SignalInfo signal)
    {
        trafficLights = new List<GameObject>();

        foreach (var location in signal.location)
        {
            GameObject trafficLight = Instantiate(greenTrafficLight);
            trafficLight.transform.localPosition = GetSignalPosition(location);
            trafficLight.transform.Rotate(transformHelper.GetTfLightTransform());
            trafficLight.transform.localScale = new Vector3(50, 50, 50);
            trafficLights.Add(trafficLight);
            // TODO(just one light):
            break;
        }

        CreateTrafficLightPolyon(signal.polygon);

        return trafficLights;
    }

    public GameObject GetTrafficCone()
    {
        GameObject cone = Instantiate(trafficCone);
        return cone;
    }

    public GameObject GetStopSignModel()
    {
        return new GameObject("Signal");
    }

    public GameObject GetYieldModel()
    {
        return new GameObject("Signal");
    }
}
