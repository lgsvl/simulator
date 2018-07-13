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
}
