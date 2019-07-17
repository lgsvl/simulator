using System.Collections.Generic;
using apollo.common;
using apollo.hdmap;

public class BasePolygon
{

    public Id id { get; set; }
    public List<PointENU> polygon { set; get; }

    public virtual string name => "BasePolygon";
}
