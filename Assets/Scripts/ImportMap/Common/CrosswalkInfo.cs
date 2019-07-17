using apollo.common;
using apollo.hdmap;
using System.Collections.Generic;
using Util;

public class CrosswalkInfo : BasePolygon
{
    public CrosswalkInfo(Crosswalk crosswalk)
    {
        _crosswalk = crosswalk;
        SetPolygon();
    }

    private Crosswalk _crosswalk;

    public override string name => "Crosswalk";

    private void SetPolygon()
    {
        polygon = Tools.Transform(_crosswalk.polygon.point);
    }
}

