# Example JSON Configuration for an Autoware Vehicle [](#top)

### Bridge Type [[top]] {: #bridge-type data-toc-label='Bridge Type'}

`ROS`

### Published Topics [[top]] {: #published-topics data-toc-label='Published Topics'}

|Topic|Sensor Name|
|:-:|:-:|
|`/nmea_sentence`|GPS|
|`/odom`|GPS Odometry|
|`/imu_raw`|IMU|
|`/points_raw`|Lidar|
|`/simulator/camera_node/image/compressed`|Main Camera|

### Subscribed Topics [[top]] {: #subscribed-topics data-toc-label='Subscribed Topics'}

|Topic|Sensor Name|
|:-:|:-:|
|`/vehicle_cmd`|Autoware Car Control|

### Complete JSON Configuration [[top]] {: #complete-json-configuration data-toc-label='Complete JSON Configuration'}

```JSON
[
  {
    "type": "GPS Device",
    "name": "GPS",
    "params": {
      "Frequency": 12.5,
      "Topic": "/nmea_sentence",
      "Frame": "gps",
      "IgnoreMapOrigin": true
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
    "type": "GPS Odometry",
    "name": "GPS Odometry",
    "params": {
      "Frequency": 12.5,
      "Topic": "/odom",
      "Frame": "gps",
      "IgnoreMapOrigin": true
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
    "type": "IMU",
    "name": "IMU",
    "params": {
      "Topic": "/imu_raw",
      "Frame": "imu"
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
    "type": "Lidar",
    "name": "Lidar",
    "params": {
      "LaserCount": 32,
      "MinDistance": 0.5,
      "MaxDistance": 100,
      "RotationFrequency": 10,
      "MeasurementsPerRotation": 360,
      "FieldOfView": 41.33,
      "CenterAngle": 10,
      "Compensated": true,
      "PointColor": "#ff000000",
      "Topic": "/points_raw",
      "Frame": "velodyne"
    },
    "transform": {
      "x": 0,
      "y": 2.312,
      "z": -0.3679201,
      "pitch": 0,
      "yaw": 0,
      "roll": 0
    }
  },
  {
    "type": "Color Camera",
    "name": "Main Camera",
    "params": {
      "Width": 1920,
      "Height": 1080,
      "Frequency": 15,
      "JpegQuality": 75,
      "FieldOfView": 50,
      "MinDistance": 0.1,
      "MaxDistance": 1000,
      "Topic": "/simulator/camera_node/image/compressed",
      "Frame": "camera"
    },
    "transform": {
      "x": 0,
      "y": 1.7,
      "z": -0.2,
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
      "Topic": "/vehicle_cmd"
    }
  }
]
```
