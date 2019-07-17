using System.Collections.Generic;
using apollo.hdmap;
using apollo.common;

using Util;

public class JunctionInfo : BasePolygon
{
    public JunctionInfo(Junction junction)
    {
        _junction = junction;
        SetPolygon();
    }

    private Junction _junction;

    public override string name => "Junction";

    private void SetPolygon()
    {
        polygon = Tools.Transform(_junction.polygon.point);
    }
}

