using System;

namespace Web
{
    public class MapRequest
    {
        public string name;
        public string url;
    }

    public class VehicleRequest
    {
        public string name;
        public string url;
        public string[] sensors;
    }

    public class ClusterRequest
    {
        public string name;
        public string[] ips;
    }

    public class SimulationRequest
    {
        public string name;
        public int map;
        public int[] vehicles;
        public bool apiOnly;
        public bool interactive;
        public bool offScreen;
        public int cluster;
        public DateTime timeOfDay;
        public Weather weather;
    }

    public class Weather
    {
        public float rain;
        public float fog;
        public float wetness;
        public float cloudiness;
    }
}
