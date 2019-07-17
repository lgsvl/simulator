using System;
using System.Collections.Generic;
using apollo.common;
using apollo.hdmap;
using Util;

public class SignalInfo : BasePolygon
{
    public SignalInfo (Signal signal)
    {
        _signal = signal;
        SetLocation();
        SetPolygon();
    }

    private Signal _signal;

    public Signal.Type type { get; set; }

    // location is the trafficlight's position and boundary is the trafficlight's bor
    public List<PointENU> location { get; set; }

    private void SetLocation()
    {
        location = new List<PointENU>();

        foreach (var subsignal in _signal.subsignal)
        {
            location.Add(Tools.Transform(subsignal.location));
        }
    }

    private void SetPolygon()
    {
        polygon = Tools.Transform(_signal.boundary.point);
    }

    public override string name => "Signal";

}

