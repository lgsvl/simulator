# Example JSON Configuration for an Apollo 5.0 Vehicle [](#top)

### Bridge Type [[top]] {: #bridge-type data-toc-label='Bridge Type'}

`CyberRT`

### Published Topics [[top]] {: #published-topics data-toc-label='Published Topics'}

|Topic|Sensor Name|
|:-:|:-:|
|`/apollo/canbus/chassis`|CAN Bus|
|`/apollo/sensor/gnss/best_pose`|GPS|
|`/apollo/sensor/gnss/odometry`|GPS Odometry|
|`/apollo/sensor/gnss/ins_stat`|GPS INS Status|
|`/apollo/sensor/gnss/imu`|IMU|
|`/apollo/sensor/gnss/corrected_imu`|IMU|
|`/apollo/sensor/conti_radar`|Radar|
|`/apollo/sensor/lidar128/compensator/PointCloud2`|Lidar|
|`/apollo/sensor/camera/front_6mm/image/compressed`|Main Camera|
|`/apollo/sensor/camera/front_12mm/image/compressed`|Telephoto Camera|

### Subscribed Topics [[top]] {: #subscribed-topics data-toc-label='Subscribed Topics'}

|Topic|Sensor Name|
|:-:|:-:|
|`/apollo/control`|Apollo Car Control|

### Complete JSON Configuration [[top]] {: #complete-json-configuration data-toc-label='Complete JSON Configuration'}

```JSON
[
  {
    "type": "CAN-Bus",
    "name": "CAN Bus",
    "params": {
      "Frequency": 10,
      "Topic": "/apollo/canbus/chassis"
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
      "Topic": "/apollo/sensor/gnss/best_pose",
      "Frame": "gps"
    },
    "transform": {
      "x": 0,
      "y": 0,
      "z": -1.348649,
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
      "Topic": "/apollo/sensor/gnss/odometry",
      "Frame": "gps"
    },
    "transform": {
      "x": 0,
      "y": 0,
      "z": -1.348649,
      "pitch": 0,
      "yaw": 0,
      "roll": 0
    }
  },
  {
    "type": "GPS-INS Status",
    "name": "GPS INS Status",
    "params": {
      "Frequency": 12.5,
      "Topic": "/apollo/sensor/gnss/ins_stat",
      "Frame": "gps"
    },
    "transform": {
      "x": 0,
      "y": 0,
      "z": -1.348649,
      "pitch": 0,
      "yaw": 0,
      "roll": 0
    }
  },
  {
    "type": "IMU",
    "name": "IMU",
    "params": {
      "Topic": "/apollo/sensor/gnss/imu",
      "Frame": "imu",
      "CorrectedTopic": "/apollo/sensor/gnss/corrected_imu",
      "CorrectedFrame": "imu"
    },
    "transform": {
      "x": 0,
      "y": 0,
      "z": -1.348649,
      "pitch": 0,
      "yaw": 0,
      "roll": 0
    }
  },
  {
	"type": "Radar",
	"name": "Radar",
	"params": {
	  "Frequency": 13.4,
	  "Topic": "/apollo/sensor/conti_radar"
	},
	"transform": {
	  "x": 0,
	  "y": 0.689,
	  "z": 2.272,
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
      "Topic": "/apollo/sensor/lidar128/compensator/PointCloud2",
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
      "Topic": "/apollo/sensor/camera/front_6mm/image/compressed"
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
    "type": "Color Camera",
    "name": "Telephoto Camera",
    "params": {
      "Width": 1920,
      "Height": 1080,
      "Frequency": 15,
      "JpegQuality": 75,
      "FieldOfView": 10,
      "MinDistance": 0.1,
      "MaxDistance": 1000,
      "Topic": "/apollo/sensor/camera/front_12mm/image/compressed"
    },
    "transform": {
      "x": 0,
      "y": 1.7,
      "z": -0.2,
      "pitch": -4,
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
    "name": "Apollo Car Control",
    "params": {
      "Topic": "/apollo/control"
    }
  }
]
```
