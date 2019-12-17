# <a name="top"></a> Sensor JSON Options

This page details the different available sensors and the configuration options possible. 

<h2> Table of Contents</h2>
[TOC]


### Examples [[top]] {: #examples data-toc-label='Examples'}

Example JSON configurations are available here:

- [Apollo 3.0 JSON](apollo-json-example.md)
- [Apollo 5.0 JSON](apollo5-0-json-example.md)
- [Autoware JSON](autoware-json-example.md)
- [Data Collection JSON](ground-truth-json-example.md)

### How to Specify a Sensor [[top]] {: #how-to-specify-a-sensor data-toc-label='How to Specify a Sensor'}
A vehicle configuration is in the following format:

```output
[
    SENSOR,
    SENSOR,
    SENSOR
]
```

A `SENSOR` is defined in the JSON configuration in the following format:

```output
{
	"type": STRING,
	"name": STRING,
	"params": {PARAMS},
	"transform": {
	"x": FLOAT,
	"y": FLOAT,
	"z": FLOAT,
	"pitch": FLOAT,
	"yaw": FLOAT,
	"roll": FLOAT,
	}
}
```
- `type` is the type of sensor.
- `name` is the name of the sensor. This is how the sensor will be identified.
- `params` are the explicitly specified parameters. If a parameter is not set, the Default Value in the sensor definition will be used.
	- ex. `{"Width": 1920, "Height": 1080}`
	- There are 2 parameters that all sensors have
		
		|Parameter|Description|Default Value|
		|:-:|:-:|:-:|
		|`Topic`|defines the topic that the sensor will subscribe/publish to|`null`|
		|`Frame`|defines the frame_id if the sensor publishes a ROS message. See [ROS Header Message](http://docs.ros.org/melodic/api/std_msgs/html/msg/Header.html) for more information|`null`|

- `transform` is the location and rotation of the sensor relative to the local position of the vehicle. 
The Unity left-hand coordinate system is used (+x right, +y up, +z forward, +pitch tilts the front down, +yaw rotates clockwise when viewed from above, +roll tilts the left side down).
	- `x` is the position of the sensor along the x-axis
	- `y` is the position of the sensor along the y-axis
	- `z` is the position of the sensor along the z-axis
	- `pitch` is the rotation around the x-axis
	- `yaw` is the rotation around the y-axis
	- `roll` is the rotation around the z-axis


### Color Camera [[top]] {: #color-camera data-toc-label='Color Camera'}
This is the type of sensor that would be used for the `Main Camera` in Apollo.

|Parameter|Description|Unit|Type|Default Value|Minimum|Maximum|
|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
|`Width`|defines the width of the image output|pixels|Int|1920|1|1920|
|`Height`|defines the height of the image output|pixels|Int|1080|1|1080|
|`Frequency`|defines the maximum rate that messages will be published|Hertz|Int|15|1|100|
|`JpegQuality`|defines the quality if the image output|%|Int|75|0|100|
|`FieldOfView`|defines the vertical angle that the camera sees|degrees|Float|60|1|90
|`MinDistance`|defines how far an object must be from the sensor for it to be detected|meters|Float|0.1|0.01|1000|
|`MaxDistance`|defines how close an object must be to the sensor for it to be detected|meters|Float|1000|0.01|2000|

```JSON
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
      "Topic": "/simulator/main_camera",
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
}
```

### Depth Camera [[top]] {: #depth-camera data-toc-label='Depth Camera'}
This sensor returns an image where the shades on the grey-scale correspond to the depth of objects.

|Parameter|Description|Unit|Type|Default Value|Minimum|Maximum|
|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
|`Width`|defines the width of the image output|pixels||Int|1920|1|1920|
|`Height`|defines the height of the image output|pixels|Int|1080|1|1080|
|`Frequency`|defines the maximum rate that messages will be published|Hertz|Int|5|1|100|
|`JpegQuality`|defines the quality if the image output|%|Int|100|0|100|
|`FieldOfView`|defines the vertical angle that the camera sees|degrees|Float|60|1|90
|`MinDistance`|defines how far an object must be from the sensor for it to be detected|meters|Float|0.1|0.01|1000|
|`MaxDistance`|defines how close an object must be to the sensor for it to be detected|meters|Float|1000|0.01|2000|

```JSON
{
    "type": "Depth Camera",
    "name": "Depth Camera",
    "params": {
      "Width": 1920,
      "Height": 1080,
      "Frequency": 15,
      "JpegQuality": 75,
      "FieldOfView": 50,
      "MinDistance": 0.1,
      "MaxDistance": 1000,
      "Topic": "/simulator/depth_camera"
    },
    "transform": {
      "x": 0,
      "y": 1.7,
      "z": -0.2,
      "pitch": 0,
      "yaw": 0,
      "roll": 0
    }
}
```

### Semantic Camera [[top]] {: #semantic-camera data-toc-label='Semantic Camera'}
This sensor returns an image where objects are colored corresponding to their tag:

|Tag|Color|Hex Value|
|:-:|:-:|:-:|
|Car|Blue|#120E97|
|Road|Purple|#7A3F83|
|Sidewalk|Orange|#BA8350|
|Vegetation|Green|#71C02F|
|Obstacle|White|#FFFFFF|
|TrafficLight|Yellow|#FFFF00|
|Building|Turquoise|#238688|
|Sign|Dark Yellow|#C0C000|
|Shoulder|Pink|#FF00FF|
|Pedestrian|Red|#FF0000|
|Curb|Dark Purple|#4A254F|

|Parameter|Description|Unit|Type|Default Value|Minimum|Maximum|
|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
|`Width`|defines the width of the image output|pixels|Int|1920|1|1920|
|`Height`|defines the height of the image output|pixels|Int|1080|1|1080|
|`Frequency`|defines the maximum rate that messages will be published|Hertz|Int|15|1|100|
|`FieldOfView`|defines the vertical angle that the camera sees|degrees|Float|60|1|90
|`MinDistance`|defines how far an object must be from the sensor for it to be detected|meters|Float|0.1|0.01|1000|
|`MaxDistance`|defines how close an object must be to the sensor for it to be detected|meters|Float|1000|0.01|2000|

```JSON
{
    "type": "Semantic Camera",
    "name": "Semantic Camera",
    "params": {
      "Width": 1920,
      "Height": 1080,
      "Frequency": 15,
      "FieldOfView": 50,
      "MinDistance": 0.1,
      "MaxDistance": 1000,
      "Topic": "/simulator/semantic_camera"
    },
    "transform": {
      "x": 0,
      "y": 1.7,
      "z": -0.2,
      "pitch": 0,
      "yaw": 0,
      "roll": 0
    }
}
```

### Lidar [[top]] {: #lidar data-toc-label='Lidar'}
This sensor returns a point cloud after 1 revolution.

|Parameter|Description|Unit|Type|Default Value|Minimum|Maximum|
|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
|`LaserCount`|defines how many vertically stacked laser beams there are||Int|32|1|128|
|`MinDistance`|defines how far an object must be from the sensor for it to be detected|meters|Float|0.5|0.01|1000|
|`MaxDistance`|defines how close an object must be to the sensor for it to be detected|meters|Float|100|0.01|2000|
|`RotationFrequency`|defines how fast the sensor rotates|Hertz|Float|10|1|30|
|`MeasurementsPerRotation`|defines how many measurements each beam takes per rotation||Int|1500|18|6000|
|`FieldOfView`|defines the vertical angle between bottom and top ray|degrees|Float|41.33|1|45|
|`CenterAngle`|defines the center of the FieldOfView cone to the horizon (+ means below horizon)|degrees|Float|10|-45|45|
|`Compensated`|defines whether or not the point cloud is compensated for the movement of the vehicle||Bool|`true`|||
|`PointSize`|defines how large of points are visualized|pixels|Float|2|1|10|
|`PointColor`|defines the color of visualized points|rgba in hex|Color|#FF0000FF|||

```JSON
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
      "Topic": "/point_cloud",
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
}
```

### 3D Ground Truth [[top]] {: #3d-ground-truth data-toc-label='3D Ground Truth'}
This sensor returns 3D ground truth data for training and creates bounding boxes around the detected objects. The color of the object corresponds to the object's type:

|Object|Color|
|:-:|:-:|
|Car|Green|
|Pedestrian|Yellow|
|Bicycle|Cyan|
|Unknown|Magenta|

|Parameter|Description|Unit|Type|Default Value|Minimum|Maximum|
|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
|`Frequency`|defines the maximum rate that messages will be published|Hertz|Float|10|1|100|
|`MaxDistance`|defines the how close an object must be to the sensor to be detected|meters|Float|100|1|1000|

```JSON
{
    "type": "3D Ground Truth",
    "name": "3D Ground Truth",
    "params": {
      "Frequency": 10,
      "Topic": "/simulator/ground_truth/3d_detections"
    },
    "transform": {
      "x": 0,
      "y": 1.975314,
      "z": -0.3679201,
      "pitch": 0,
      "yaw": 0,
      "roll": 0
    }
}
```

### 3D Ground Truth Visualizer [[top]] {: #3d-ground-truth-visualizer data-toc-label='3D Ground Truth Visualizer'}
This sensor will visualize bounding boxes on objects as detected by the AD Stack. It does not publish any data and instead subscribes to a topic from the AD Stack. The color of the boxes are:

|Object|Color|
|:-:|:-:|
|Car|Green|
|Pedestrian|Yellow|
|Bicycle|Cyan|
|Unknown|Magenta|

```JSON
{
    "type": "3D Ground Truth Visualizer",
    "name": "3D Ground Truth Visualizer",
    "params": {
      "Topic": "/simulator/ground_truth/3d_visualize"
    },
    "transform": {
      "x": 0,
      "y": 1.975314,
      "z": -0.3679201,
      "pitch": 0,
      "yaw": 0,
      "roll": 0
    }
}
```

### CAN-Bus [[top]] {: #can-bus data-toc-label='CAN-Bus'}
This sensor sends data about the vehicle chassis. The data includes:
- Speed [m/s]
- Throttle [%]
- Braking [%]
- Steering [+/- %]
- Parking Brake Status [bool]
- High Beam Status [bool]
- Low Beam Status [bool]
- Hazard Light Status [bool]
- Fog Light Status [bool]
- Left Turn Signal Status [bool]
- Right Turn Signal Status [bool]
- Wiper Status [bool]
- Reverse Gear Status [bool]
- Selected Gear [Int]
- Engine Status [bool]
- Engine RPM [RPM]
- GPS Latitude [Latitude]
- GPS Longitude [Longitude]
- Altitude [m]
- Orientation [3D Vector of Euler angles]
- Velocity [3D Vector of m/s]


|Parameter|Description|Unit|Type|Default Value|Minimum|Maximum|
|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
|`Frequency`|defines the maximum rate that messages will be published|Hertz|Float|10|1|100|

```JSON
{
    "type": "CAN-Bus",
    "name": "CAN Bus",
    "params": {
      "Frequency": 10,
      "Topic": "/canbus"
    },
    "transform": {
      "x": 0,
      "y": 0,
      "z": 0,
      "pitch": 0,
      "yaw": 0,
      "roll": 0
    }
}
```

### GPS Device [[top]] {: #gps-device data-toc-label='GPS Device'}
This sensor outputs the GPS location of the vehicle in Longitude/Latitude and Northing/Easting coordintates. 

|Parameter|Description|Unit|Type|Default Value|Minimum|Maximum|
|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
|`Frequency`|defines the maximum rate that messages will be published|Hertz|Float|12.5|1|100|
|`IgnoreMapOrigin`|defines whether or not the actual GPS position is returned. If `true`, then the Unity world position is returned (as if the MapOrigin were (0,0))||Bool|`false`|||

```JSON
{
    "type": "GPS Device",
    "name": "GPS",
    "params": {
      "Frequency": 12.5,
      "Topic": "/gps",
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
}
```

### GPS Odometry [[top]] {: #gps-odometry data-toc-label='GPS Odometry'}
This sensor outputs the GPS location of the vehicle in Longitude/Latitude and Northing/Easting coordintates and the vehicle velocity.

|Parameter|Description|Unit|Type|Default Value|Minimum|Maximum|
|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
|`Frequency`|defines the maximum rate that messages will be published|Hertz|Float|12.5|1|100|
|`ChildFrame`|used by Autoware|||||
|`IgnoreMapOrigin`|defines whether or not the actual GPS position is returned. If `true`, then the Unity world position is returned (as if the MapOrigin were (0,0))||Bool|`false`|||

```JSON
{
    "type": "GPS Odometry",
    "name": "GPS Odometry",
    "params": {
      "Frequency": 12.5,
      "Topic": "/gps_odometry",
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
  }
```

### GPS-INS Status [[top]] {: #gps-ins-status data-toc-label='GPS-INS Status'}
This sensor outputs the status of the GPS correction due to INS. The Simulator is an ideal environment in which GPS is always corrected.

|Parameter|Description|Unit|Type|Default Value|Minimum|Maximum|
|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
|`Frequency`|defines the maximum rate that messages will be published [Hertz]|Float|12.5|1|100|

```JSON
{
    "type": "GPS-INS Status",
    "name": "GPS INS Status",
    "params": {
      "Frequency": 12.5,
      "Topic": "/gps_ins_stat",
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
  }
```

### Vehicle Control [[top]] {: #vehicle-control data-toc-label='Vehicle Control'}
This sensor is required for a vehicle to subscribe to the control topic of an AD Stack. 

```JSON
{
    "type": "Vehicle Control",
    "name": "AD Car Control",
    "params": {
      "Topic": "/vehicle_cmd"
    }
}
```

### Keyboard Control [[top]] {: #keyboard-control data-toc-label='Keyboard Control'}
This sensor is required for a vehicle to accept keyboard control commands. Parameters are not required.

```JSON
{
    "type": "Keyboard Control",
    "name": "Keyboard Car Control"
}
```

### Wheel Control [[top]] {: #wheel-control data-toc-label='Wheel Control'}
This sensor is required for a vehicle to accept Logitech G920 wheel control commands. Parameters are not required.

```JSON
{
    "type": "Wheel Control",
    "name": "Wheel Car Control"
}
```

### Manual Control [[top]] {: #manual-control data-toc-label='Manual Control'}
This sensor is required for a vehicle to accept keyboard control commands. Parameters are not required.
`{Deprecated}` Will be removed next release.  Use Keyboard Control

```JSON
{
    "type": "Manual Control",
    "name": "Manual Car Control"
}
```

### Cruise Control [[top]] {: #cruise-control data-toc-label='Cruise Control'}
This sensor causes the vehicle to accelerate to the desired speed and then maintain the desired speed. 

|Parameter|Description|Unit|Type|Default Value|Minimum|Maximum|
|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
|`CruiseSpeed`|defines the desired speed|meters/second|Float|0|0|200|

```JSON
{
    "type": "Cruise Control",
    "name": "AD Car Control",
    "params": {
      "CruiseSpeed": 10
    }
}
```

### IMU [[top]] {: #imu data-toc-label='IMU'}
This sensor output at a fixed rate of 100 Hz. IMU publishes data on topics where the 2nd topic has corrected IMU data.

|Parameter|Description|
|:-:|:-:|
|`CorectedTopic`|defines the 2nd topic that the data is published to|
|`CorrectedFrame`|defines the 2nd frame for the ROS header|

```JSON
{
    "type": "IMU",
    "name": "IMU",
    "params": {
      "Topic": "/imu",
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
}
```

### 2D Ground Truth [[top]] {: #2d-ground-truth data-toc-label='2D Ground Truth'}
This sensor outputs an image where objects are encased in a box. The color of the box depends on the type of object.

|Object|Color|
|:-:|:-:|
|Car|Green|
|Pedestrian|Yellow|
|Bicycle|Cyan|
|Unknown|Magenta|

|Parameter|Description|Unit|Type|Default Value|Minimum|Maximum|
|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
|`Frequency`|defines the maximum rate that messages will be published|Hertz|Float|10|1|100|
|`Width`|defines the width of the image|pixels|Int|1920|1|1920|
|`Height`|defines the height of the iamge|pixels|Int|1080|1|1080|
|`FieldOfView`|defines the vertical angle that the camera sees|degrees|Float|60|1|90|
|`MinDistance`|defines how far an object must be from the sensor to be in the image|meters|Float|0.1|0.01|1000|
|`MaxDistance`|defines how close an object must be to the sensor to be in the image|meters|Float|1000|0.01|2000|
|`DetectionRange`|defines how close an object must be to be given a bounding box|meters|Float|100|0.01|2000|

```JSON
{
    "type": "2D Ground Truth",
    "name": "2D Ground Truth",
    "params": {
      "Frequency": 10,
      "Topic": "/simulator/ground_truth/2d_detections"
    },
    "transform": {
      "x": 0,
      "y": 1.7,
      "z": -0.2,
      "pitch": 0,
      "yaw": 0,
      "roll": 0
    }
}
```

### 2D Ground Truth Visualizer [[top]] {: #2d-ground-truth-visualizer data-toc-label='2D Ground Truth Visualizer'}
This sensor will visualize bounding boxes on objects as detected by the AD Stack, it does not publish any data. The color of the boxes are:

|Object|Color|
|:-:|:-:|
|Car|Green|
|Pedestrian|Yellow|
|Bicycle|Cyan|
|Unknown|Magenta|

In order for bounding boxes to align properly, parameters should match the same camera that the AD Stack is using for detection (i.e. if running Apollo, the parameters should match the sensor named "Main Camera"). 

|Parameter|Description|Unit|Type|Default Value|Minimum|Maximum|
|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
|`Width`|defines the width of the image|pixels|Int|1920|1|1920|
|`Height`|defines the height of the iamge|pixels|Int|1080|1|1080|
|`FieldOfView`|defines the vertical angle that the camera sees|degrees|Float|60|1|90|
|`MinDistance`|defines how far an object must be from the sensor to be in the image|meters|Float|0.1|0.01|1000|
|`MaxDistance`|defines how close an object must be to the sensor to be in the image|meters|Float|1000|0.01|2000|

```JSON
{
    "type": "2D Ground Truth Visualizer",
    "name": "2D Ground Truth Visualizer",
    "params": {
      "Width": 1920,
      "Height": 1080,
      "FieldOfView": 50,
      "MinDistance": 0.1,
      "MaxDistance": 1000,
      "Topic": "/simulator/ground_truth/2d_visualize"
    },
    "transform": {
      "x": 0,
      "y": 1.7,
      "z": -0.2,
      "pitch": 0,
      "yaw": 0,
      "roll": 0
    }
}
```

### Radar [[top]] {: #radar data-toc-label='Radar'}
This sensor outputs the objects detected by the radar. Detected objects are visualized with a box colored by their type:

|Type|Color|
|:-:|:-:|
|Car|Green|
|Agent|Magenta|
|Bicycle|Cyan|

|Parameter|Description|Unit|Type|Default Value|Minimum|Maximum|
|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
|`Frequency`|defines the maximum rate that messages will be published|Hertz|Float|13.4|1|100|

```JSON
{
    "type": "Radar",
    "name": "Radar",
    "params": {
      "Frequency": 13.4,
      "Topic": "/radar"
    },
    "transform": {
      "x": 0,
      "y": 0.689,
      "z": 2.272,
      "pitch": 0,
      "yaw": 0,
      "roll": 0
    }
}
```

### Clock [[top]] {: #clock data-toc-label='Clock'}
This sensor outputs simulated time to ROS as [rosgraph_msgs/Clock](http://docs.ros.org/api/rosgraph_msgs/html/msg/Clock.html) message.
Only parameter to use is topic name.

```JSON
{
    "type": "Clock",
    "name": "ROS Clock",
    "params": {
      "Topic": "/clock"
    }
}
```

### Control Calibration [[top]] {: #control-calibration data-toc-label='Control Calibration'}
This sensor outputs control calibration criteria collected by AD Stacks (Apollo, Autoware). It generates steering, throttle or brakes with gear commands between minimum and maximum of velocity during duration.

|Parameter|Description|Unit|Type|Minimum|Maximum|
|:-:|:-:|:-:|:-:|:-:|:-:|
|`min_velocity`|defines the minimum velocity when criterion is executed|meters/second|Float|0|50.0|
|`max_velocity`|defines the maximum velocity when criterion is executed|meters/second|Float|0|50.0|
|`throttle`|defines the throttle which makes acceleration|Percent|Float|0|100.0|
|`brakes`|defines the brakes which make deceleration|Percent|Float|0|100.0|
|`steering`|defines ego vehicle's steering|Percent|Float|-100.0|100.0|
|`gear`|defines ego vehicle's direction (forward or reverse)||String|||
|`duration`|defines criterion's execution time|second|Float|0||

```JSON
{
    "type": "Control Calibration",
    "name": "Control Calibration",
    "params": {
        "states": [{
                "min_velocity": 0.2,
                "max_velocity": 10.0,
                "throttle": 23,
                "brakes": 0,
                "steering": 0,
                "gear": "forward",
                "duration": 4
            },
            {
                "min_velocity": 0.2,
                "max_velocity": 2.0,
                "throttle": 22,
                "brakes": 0,
                "steering": 0,
                "gear": "reverse",
                "duration": 4
            }
        ]
    }
}
```

[Total Control Calibration Criteria:](./total-control-calibration-criteria.md)
