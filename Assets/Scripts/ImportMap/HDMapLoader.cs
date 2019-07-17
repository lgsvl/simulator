using UnityEngine;

using System.Collections.Generic;
using HD = global::apollo.hdmap;


using apollo.hdmap;
using apollo.common;
using System.IO;

using Util;

/*
 * Load hdmap form file to proto
 * 
 * 
 */
public class HDMapLoader
{
    public static readonly string FOLDER_NAME = "hd_map";
    public static readonly string FILE_NAME = "base_map";

    public static PointENU origin = new PointENU();

    public Dictionary<Id, LaneInfo> laneTable { get; set; }
    public Dictionary<Id, JunctionInfo> junctionTable { get; set; }
    public Dictionary<Id, SignalInfo> signalTable { get; set; }
    public Dictionary<Id, CrosswalkInfo> crosswalkTable { get; set; }
    public Dictionary<Id, StopSignInfo> stopSignTable { get; set; }
    public Dictionary<Id, YieldSignInfo> yieldSignTable { get; set; }
    public Dictionary<Id, ClearAreaInfo> clearAreaTable { get; set; }
    public Dictionary<Id, SpeedBumpInfo> speedBumpTable { get; set; }
    public Dictionary<Id, ParkingSpaceInfo> parkingSpaceTable { get; set; }
    public Dictionary<Id, PncJunctionInfo> pncJunctionTable { get; set; }
    public Dictionary<Id, OverlapInfo> overlapTable { get; set; }
    public Dictionary<Id, RoadInfo> roadTable { get; set; }

    public HDMapLoader ()
    {
        laneTable = new Dictionary<Id, LaneInfo>();
        junctionTable = new Dictionary<Id, JunctionInfo>();
        roadTable = new Dictionary<Id, RoadInfo>();
        signalTable = new Dictionary<Id, SignalInfo>();
        crosswalkTable = new Dictionary<Id, CrosswalkInfo>();
        stopSignTable = new Dictionary<Id, StopSignInfo>();
        yieldSignTable = new Dictionary<Id, YieldSignInfo>();
        clearAreaTable = new Dictionary<Id, ClearAreaInfo>();
        speedBumpTable = new Dictionary<Id, SpeedBumpInfo>();
        parkingSpaceTable = new Dictionary<Id, ParkingSpaceInfo>();
        pncJunctionTable = new Dictionary<Id, PncJunctionInfo>();
        overlapTable = new Dictionary<Id, OverlapInfo>();
    }

    public string GetFileName()
    {
        var filepath_bin = $"{FOLDER_NAME}{System.IO.Path.DirectorySeparatorChar}{FILE_NAME}.bin";
        return filepath_bin;
    }

    private void PrintHeader(HD.Map hdmap)
    {
        if (hdmap.header != null)
        {
            Debug.Log("hdmap header: " + Tools.ByteToStr(hdmap.header.version));
            Debug.Log("hdmap header: " + Tools.ByteToStr(hdmap.header.date));
            Debug.Log("hdmap header: " + hdmap.header.projection.proj);
            Debug.Log("hdmap header: " + Tools.ByteToStr(hdmap.header.district));
            Debug.Log("hdmap header: " + Tools.ByteToStr(hdmap.header.generation));
            Debug.Log("hdmap header: " + Tools.ByteToStr(hdmap.header.rev_major));
            Debug.Log("hdmap header: " + Tools.ByteToStr(hdmap.header.rev_minor));
            Debug.Log("hdmap header: " + hdmap.header.left);
            Debug.Log("hdmap header: " + hdmap.header.top);
            Debug.Log("hdmap header: " + hdmap.header.right);
            Debug.Log("hdmap header: " + hdmap.header.bottom);
            Debug.Log("hdmap header: " + Tools.ByteToStr(hdmap.header.vendor));
        }
    }

    private void PrintMessage()
    {
        foreach(var lane in laneTable)
        {
            Debug.Log("lane id: " + lane.Key.id);
            foreach (var point in lane.Value.leftBoundary)
            {
                Debug.Log("lane values: " + point.x + "," + point.y);
            }
        }
    }

    private void PrintOrigin()
    {
        Debug.Log("origin: " + origin.x + "," + origin.y + "," + origin.z);
    }

    public void LoadMapFromFile(string filename)
    {
        var file = File.OpenRead(filename);
        if (file == null) {
            Debug.LogError("Open hdmap file error!");
            return;
        }

        HD.Map hdmap = ProtoBuf.Serializer.Deserialize<HD.Map>(file);

        // Set Origin
        SetOrigin(hdmap);
        PrintOrigin();

        // Debug
        PrintHeader(hdmap);

        LoadMapFromProto(hdmap);

        // Debug
        //PrintMessage();

    }

    private void LoadMapFromProto(HD.Map hdmap)
    {
        foreach (var lane in hdmap.lane) {
            laneTable.Add(lane.id, new LaneInfo(lane));
        }

        foreach (var junction in hdmap.junction)
        {
            junctionTable.Add(junction.id, new JunctionInfo(junction));
        }

        foreach (var signal in hdmap.signal)
        {
            signalTable.Add(signal.id, new SignalInfo(signal));
        }

        foreach (var crosswalk in hdmap.crosswalk)
        {
            crosswalkTable.Add(crosswalk.id, new CrosswalkInfo(crosswalk));
        }

        foreach (var stopSign in hdmap.stop_sign)
        {
            stopSignTable.Add(stopSign.id, new StopSignInfo(stopSign));
        }

        foreach (var yield in hdmap.yield)
        {
            yieldSignTable.Add(yield.id, new YieldSignInfo(yield));
        }

        foreach (var clearArea in hdmap.clear_area)
        {
            clearAreaTable.Add(clearArea.id, new ClearAreaInfo(clearArea));
        }

        foreach (var speedBump in hdmap.speed_bump)
        {
            speedBumpTable.Add(speedBump.id, new SpeedBumpInfo(speedBump));
        }

        foreach (var parkingSpace in hdmap.parking_space)
        {
            parkingSpaceTable.Add(parkingSpace.id, new ParkingSpaceInfo(parkingSpace));
        }

        foreach (var pncJunction in hdmap.pnc_junction)
        {
            pncJunctionTable.Add(pncJunction.id, new PncJunctionInfo(pncJunction));
        }

        foreach (var overlap in hdmap.overlap)
        {
            overlapTable.Add(overlap.id, new OverlapInfo(overlap));
        }

        foreach (var road in hdmap.road)
        {
            roadTable.Add(road.id, new RoadInfo(road));
        }
    }

    public PointENU SetOrigin(HD.Map hdmap)
    {
        origin = hdmap.lane[0].left_boundary.curve.segment[0].line_segment.point[0];
        return origin;
    }
}
