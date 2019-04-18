using System;

namespace Web
{
    public class WebResponse
    {
        public int Id { get; set; }
    }

    public class MapResponse : WebResponse
    {
        public string Name;
        public string Url;
        public string PreviewUrl;
        public string Status;
    }

    public class VehicleResponse : WebResponse
    {
        public string Name;
        public string Url;
        public string PreviewUrl;
        public string Status;
        public string[] Sensors;
    }

    public class ClusterResponse : WebResponse
    {
        public string Name;
        public string[] Ips;
    }

    public class SimulationResponse : WebResponse
    {
        public string Name;
        public int Map;
        public int[] Vehicles;
        public bool? ApiOnly;
        public bool? Interactive;
        public bool? OffScreen;
        public int? Cluster;
        public DateTime? TimeOfDay;
        public Weather Weather;
    }
}
