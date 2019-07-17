using apollo.common;
using System.Collections.Generic;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class RoadTool : MonoBehaviour
{
    public HDMapLoader hdMapLoader;

    public void Start()
    {
        hdMapLoader = new HDMapLoader();
        CreateRoad();
    }

    public void Update()
    {
        
    }

    /*
     * @Input: HDMap 
     * @Output: plane array (roads)
     * 
     */
    public void CreateRoad()
    {
        var filename = hdMapLoader.GetFileName();
        hdMapLoader.LoadMapFromFile(filename);
        
        Pipeline();
    }

    private void Pipeline()
    {        
        foreach (var row in hdMapLoader.laneTable)
        {
            LaneFactory.instance.GetRoad(row.Value);
        }

        foreach (var row in hdMapLoader.crosswalkTable)
        {
            LaneFactory.instance.GetCrosswalk(row.Value);
        }

        foreach (var row in hdMapLoader.signalTable)
        {
            SignalFactory.instance.CreateTrafficLight(row.Value);
            foreach (var p in row.Value.polygon)
            {
                //Debug.LogError(p.x + "," + p.y + "," + p.z);
            }
        }

        /*
         * Below all draw a polygon, No need to be regular
         * 
         */
        foreach (var row in hdMapLoader.junctionTable)
        {
            LaneFactory.instance.GetPolygonObject(row.Value);
        }

        foreach (var row in hdMapLoader.clearAreaTable)
        {
            LaneFactory.instance.GetPolygonObject(row.Value);
        }

        foreach (var row in hdMapLoader.parkingSpaceTable)
        {
            LaneFactory.instance.GetPolygonObject(row.Value);
        }
    }

}