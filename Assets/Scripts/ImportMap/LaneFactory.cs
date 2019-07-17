using UnityEngine;
using Util;

public class LaneFactory : MonoBehaviour
{
    public static LaneFactory instance;

    private MeshHelper meshHelper = new MeshHelper();
    private MaterialHelper materialHelper = new MaterialHelper();
    private MiscHelper miscHelper = new MiscHelper();
    private TransformHelper transformHelper = new TransformHelper();

    private void Awake()
    {
        instance = this;
    }

    private void PrintBoundary(Boundary boundary)
    {
        foreach (var l in boundary.left) {
            Debug.LogError("left: " + l.x + ", " + l.y);
        }

        foreach (var r in boundary.right)
        {
            Debug.LogError("right: " + r.x + ", " + r.y);
        }
    }

    public GameObject GetRoad(LaneInfo lane)
    {
        GameObject _lane = new GameObject("Lane");

        // 1. Mesh
        Boundary boundary = new Boundary(lane.leftBoundary, lane.rightBoundary);

        // Debug
        //PrintBoundary(boundary);

        Mesh mesh = meshHelper.GetMesh(boundary);
        _lane.AddComponent<MeshFilter>().sharedMesh = mesh;
        _lane.AddComponent<MeshCollider>().sharedMesh = mesh;

        // 2. Material
        _lane.AddComponent<MeshRenderer>().sharedMaterial = 
            materialHelper.GetMaterial(lane.leftBoundaryType, lane.rightBoundaryType);

        // 3. Transform (90, 0, 0)
        //_lane.transform.Rotate(transformHelper.GetTransform());

        // 4. Layer and Tag
        _lane.layer = miscHelper.GetLayer(_lane.name);
        _lane.tag = miscHelper.GetTag(_lane.name);

        return _lane;
    }


    /*
     * TODO(): I want the crosswalk will repeated but not stretch to rendering 
     * (just horizontal, the vertical direction still need stretch)
     * 
     */
    public GameObject GetCrosswalk(CrosswalkInfo crosswalk)
    {
        GameObject _crosswalk = new GameObject("Crosswalk");

        // 1. Mesh
        Polygon polygon = new Polygon(crosswalk.polygon);

        // Stopping overlapping textures from flickering,
        // set the layer down than another 0.0001f


        Mesh mesh = meshHelper.GetMesh(polygon);
        _crosswalk.AddComponent<MeshFilter>().sharedMesh = mesh;
        _crosswalk.AddComponent<MeshCollider>().sharedMesh = mesh;

        // 2. Material
        _crosswalk.AddComponent<MeshRenderer>().sharedMaterial =
            materialHelper.GetMaterial("Crosswalk");

        // 3. Transform
        //_crosswalk.transform.Rotate(transformHelper.GetTransform());

        // 4. Layer and Tag
        _crosswalk.layer = miscHelper.GetLayer(_crosswalk.name);
        _crosswalk.tag = miscHelper.GetTag(_crosswalk.name);
        return _crosswalk;
    }


    public GameObject GetPolygonObject(BasePolygon area)
    {
        GameObject _area = new GameObject(area.name);

        // 1. Mesh
        Polygon polygon = new Polygon(area.polygon);

        Mesh mesh = meshHelper.GetMesh(polygon);
        _area.AddComponent<MeshFilter>().sharedMesh = mesh;
        _area.AddComponent<MeshCollider>().sharedMesh = mesh;

        // 2. Material
        _area.AddComponent<MeshRenderer>().sharedMaterial =
            materialHelper.GetMaterial(area.name);

        // 3. Transform
        //_area.transform.Rotate(transformHelper.GetTransform());

        // 4. Layer and Tag
        _area.layer = miscHelper.GetLayer(_area.name);
        _area.tag = miscHelper.GetTag(_area.name);

        return _area;
    }

}
