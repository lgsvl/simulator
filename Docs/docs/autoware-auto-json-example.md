# Example JSON Configuration for an Autoware Auto Vehicle [](#top)

### Bridge Type [[top]] {: #bridge-type data-toc-label='Bridge Type'}

`ROS2`

### Published Topics [[top]] {: #published-topics data-toc-label='Published Topics'}

|Topic|Sensor Name|
|:-:|:-:|
|`/autoware_auto_msgs/VehicleStateReport`|CAN Bus|
|`/nmea_sentence`|GPS|
|`/autoware_auto_msgs/VehicleOdometry`|GPS Odometry|
|`/imu_raw`|IMU|
|`/points_raw`|LidarFront|
|`/points_raw_rear`|LidarRear|
|`/simulator/camera_node/image/compressed`|Main Camera|

### Subscribed Topics [[top]] {: #subscribed-topics data-toc-label='Subscribed Topics'}

|Topic|Sensor Name|
|:-:|:-:|
|`/autoware_auto_msgs/RawControlCommand`|Autoware Car Control|
|`/autoware_auto_msgs/VehicleStateCommand`|Autoware Auto Vehicle State|

### Complete JSON Configuration [[top]] {: #complete-json-configuration data-toc-label='Complete JSON Configuration'}

```JSON
[
  {
    "type": "Transform",
    "name": "base_link",
    "transform": {
      "x": -0.015,
      "y": 0.369,
      "z": -1.37,
      "pitch": 0,
      "yaw": 0,
      "roll": 0
    }
  },
  {
    "type": "CAN-Bus",
    "name": "CAN Bus",
    "params": {
      "Frequency": 10,
      "Topic": "/vehicle_state_report"
    },
    "transform": {
      "x": 0,
      "y": 0,
      "z": 0,
      "pitch": 0,
      "yaw": 0,
      "roll": 0
    }
  },
  {
    "type": "3D Ground Truth",
    "name": "3D Ground Truth",
    "params": {
      "Frequency": 10,
      "Topic": "/simulator/ground_truth/3d_detections",
      "MaxDistance": 300
    },
    "transform": {
      "x": 0,
      "y": 0,
      "z": 0,
      "pitch": 0,
      "yaw": 0,
      "roll": 0
    }
  },
  {
    "type": "GPS Device",
    "name": "GPS",
    "params": {
      "Frequency": 12.5,
      "Topic": "/gps",
      "Frame": "gps",
      "IgnoreMapOrigin": true
    },
    "parent": "base_link",
    "transform": {
      "x": 0,
      "y": 0,
      "z": 0,
      "pitch": 0,
      "yaw": 0,
      "roll": 0
    }
  },
  {
    "type": "GPS Odometry",
    "name": "GPS Odometry",
    "params": {
      "Frequency": 12.5,
      "Topic": "/odom",
      "Frame": "map",
      "ChildFrame": "gps",
      "IgnoreMapOrigin": false
    },
    "parent": "base_link",
    "transform": {
      "x": 0,
      "y": 0,
      "z": 0,
      "pitch": 0,
      "yaw": 0,
      "roll": 0
    }
  },
  {
    "type": "IMU",
    "name": "IMU",
    "params": {
      "Topic": "/imu_raw",
      "Frame": "imu"
    },
    "parent": "base_link",
    "transform": {
      "x": 0,
      "y": 0,
      "z": 0,
      "pitch": 0,
      "yaw": 0,
      "roll": 0
    }
  },
  {
    "type": "Lidar",
    "name": "LidarFront",
    "params": {
      "LaserCount": 16,
      "MinDistance": 2,
      "MaxDistance": 100,
      "RotationFrequency": 10,
      "MeasurementsPerRotation": 360,
      "FieldOfView": 20,
      "CenterAngle": 0,
      "Compensated": true,
      "PointColor": "#ff000000",
      "Topic": "/points_raw",
      "Frame": "velodyne_front"
    },
    "parent": "base_link",
    "transform": {
      "x": 0.022,
      "y": 1.49,
      "z": 1.498,
      "pitch": 0,
      "yaw": 0,
      "roll": 0
    }
  },
  {
    "type": "Lidar",
    "name": "LidarRear",
    "params": {
      "LaserCount": 16,
      "MinDistance": 2,
      "MaxDistance": 100,
      "RotationFrequency": 10,
      "MeasurementsPerRotation": 360,
      "FieldOfView": 20,
      "CenterAngle": 0,
      "Compensated": true,
      "PointColor": "#ff000000",
      "Topic": "/points_raw_rear",
      "Frame": "velodyne_rear"
    },
    "parent": "base_link",
    "transform": {
      "x": 0.022,
      "y": 1.49,
      "z": 0.308,
      "pitch": 0,
      "yaw": 0,
      "roll": 0
    }
  },
  {
    "type": "Manual Control",
    "name": "Manual Car Control"
  },
  {
    "type": "Vehicle Control",
    "name": "Autoware Car Control",
    "params": {
      "Topic": "/vehicle_control_command"
    }
  },
  {
    "type": "Vehicle State",
    "name": "Autoware Auto Vehicle State",
    "params": {
      "Topic": "/vehicle_state_command"
    }
  },
  {
    "type": "Vehicle Odometry",
    "name": "Vehicle Odometry Sensor",
    "params": {
      "Topic": "/vehicle_odometry"
    }
  }
]
```
