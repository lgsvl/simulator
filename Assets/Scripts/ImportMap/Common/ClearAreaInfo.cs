using System.Collections.Generic;
using apollo.common;
using apollo.hdmap;
using Util;

public class ClearAreaInfo : BasePolygon
{
    public ClearAreaInfo(ClearArea clearArea)
    {
        _clearArea = clearArea;
        SetPolygon();
    }

    private ClearArea _clearArea;

    public override string name => "ClearArea";

    private void SetPolygon()
    {
        polygon = Tools.Transform(_clearArea.polygon.point);
    }
}

