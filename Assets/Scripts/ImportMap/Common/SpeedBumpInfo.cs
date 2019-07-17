using System;
using System.Collections.Generic;
using apollo.common;
using apollo.hdmap;

public class SpeedBumpInfo
{
    public SpeedBumpInfo(SpeedBump speedBump)
    {
        _speedBump = speedBump;
    }

    public string name => "SpeedBump";

    private SpeedBump _speedBump;

    public List<List<PointENU>> line;

}

