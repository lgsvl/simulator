namespace Ros
{
    [MessageType("pb_msgs/Header")]
    public struct ApolloHeader
    {
        public double timestamp_sec;
        public string module_name;
        public int sequence_num;
        public int lidar_timestamp;
        public int camera_timestamp;
        public int radar_timestamp;
        public int version;
    }

    [MessageType("pb_msgs/Point3D")]
    public struct Point3D
    {
        public double x;
        public double y;
	public double z;
    }

    [MessageType("pb_msgs/Imu")]
    public struct Imu
    {
        public ApolloHeader header;
        public double measurement_time;
        public float measurement_span;
        public Point3D linear_acceleration;
        public Point3D angular_velocity;
    }

    [MessageType("pb_msgs/GnssBestPose")]
    public struct GnssBestPose
    {
        public ApolloHeader header;
        public double measurement_time;
        public int sol_status;
        public int sol_type;

        public double latitude;  // in degrees
        public double longitude;  // in degrees
        public double height_msl;  // height above mean sea level in meters
        public float undulation;  // undulation = height_wgs84 - height_msl
        public int datum_id;  // datum id number
        public float latitude_std_dev;  // latitude standard deviation (m)
        public float longitude_std_dev;  // longitude standard deviation (m)
        public float height_std_dev;  // height standard deviation (m)
        public string base_station_id;  // base station id
        public float differential_age;  // differential position age (sec)
        public float solution_age;  // solution age (sec)
        public int num_sats_tracked;  // number of satellites tracked
        public int num_sats_in_solution;  // number of satellites used in solution
        public int num_sats_l1;  // number of L1/E1/B1 satellites used in solution
        public int num_sats_multi;  // number of multi-frequency satellites used in solution
        public int reserved;  // reserved
        public int extended_solution_status;  // extended solution status - OEMV and
                                                    // greater only
        public int galileo_beidou_used_mask;
        public int gps_glonass_used_mask;
    }
}
