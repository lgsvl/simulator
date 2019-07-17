using apollo.hdmap;
using Util;

public class ParkingSpaceInfo : BasePolygon
{
    public ParkingSpaceInfo (ParkingSpace parkingSpace)
    {
        _parkingSpace = parkingSpace;
        SetPolygon();
    }

    private ParkingSpace _parkingSpace;

    private void SetPolygon()
    {
        polygon = Tools.Transform(_parkingSpace.polygon.point);
    }

    public override string name => "ParkingSpace";
}

