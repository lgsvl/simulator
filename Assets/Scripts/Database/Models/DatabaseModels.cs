namespace Database
{
    public class DatabaseModel
    {
        public string Name { get; set; }
        public int Id { get; set; }
    }

    public class Map : DatabaseModel
    {
        public string Url { get; set; }
        public string PreviewUrl { get; set; }
        public string LocalPath { get; set; }
        public string Status { get; set; }
        public string Error { get; set; }
    }
    
    public class Vehicle : DatabaseModel
    {
        public string Url { get; set; }
        public string PreviewUrl { get; set; }
        public string LocalPath { get; set; }
        public string Sensors { get; set; }
        public string Status { get; set; }
        public string Error { get; set; }
    }
    
    public class Cluster : DatabaseModel
    {
        public string Ips { get; set; }
    }

    public class Simulation : DatabaseModel
    {
        public int? Cluster { get; set; }
        public int? Map { get; set; }
        public string Vehicles { get; set; }
        public bool? ApiOnly { get; set; }
        public bool? Interactive { get; set; }
        public bool? OffScreen { get; set; }
        public System.DateTime? TimeOfDay { get; set; }
        public float? Rain { get; set; }
        public float? Fog { get; set; }
        public float? Wetness { get; set; }
        public float? Cloudiness { get; set; }
    }
}
