# <a name="top"></a> Sensor Plugin JSON Options

This page details the different available sensor plugins and the configuration options possible.
More details on how to use these plugins can be found at [Sensor Plugins](sensor-plugins.md).

<h2> Table of Contents</h2>
[TOC]


### Velodyne Lidar [[top]] {: #velodyne-lidar data-toc-label='Velodyne Lidar'}

This sensor plugin is for Velodyne Lidar. VLP-16 and VLP-32C are currently supported. 

Different to standard [Lidar Sensor](sensor-json-options.md#lidar), which generates point cloud and publishes via bridge,
this sensor plugin generates data packets and position packets and send them out via UDP. Velodyne driver running on the host
machine is reponsible for converting these packets into point cloud. For ROS-based systems, [ROS Velodyne driver](https://github.com/ros-drivers/velodyne)
can be used. Apollo also comes with its own Velodyne driver (e.g. [this](https://github.com/lgsvl/apollo-3.0/tree/simulator/modules/drivers/velodyne) 
for Apollo 3.0 and [this](https://github.com/lgsvl/apollo-5.0/tree/simulator/modules/drivers/velodyne) for Apollo 5.0).

For more details, please refer to the Velodyne product manuals for VLP-16 and VLP-32C.

|Parameter|Description|Unit|Type|Default Value|Minimum|Maximum|
|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
|`VerticalRayAngles`|defines vertical angle for each laser beam*||List of Float|empty list|||
|`LaserCount`|defines how many vertically stacked laser beams there are||Int|32|1|128|
|`FieldOfView`|defines the vertical angle between bottom and top ray|degrees|Float|41.33|1|45|
|`CenterAngle`|defines the center of the FieldOfView cone to the horizon (+ means below horizon)|degrees|Float|10|-45|45|
|`MinDistance`|defines how far an object must be from the sensor for it to be detected|meters|Float|0.5|0.01|1000|
|`MaxDistance`|defines how close an object must be to the sensor for it to be detected|meters|Float|100|0.01|2000|
|`RotationFrequency`|defines how fast the sensor rotates|Hertz|Float|10|1|30|
|`MeasurementsPerRotation`|defines how many measurements each beam takes per rotation||Int|1500|18|6000|
|`Compensated`|defines whether or not the point cloud is compensated for the movement of the vehicle||Bool|`true`|||
|`PointSize`|defines how large of points are visualized|pixels|Float|2|1|10|
|`PointColor`|defines the color of visualized points|rgba in hex|Color|#FF0000FF|||
|`VelodyneLidarType`|defines type of Velodyne Lidar||String|VLP_16|||
|`HostName`|IP address of host||String||||
|`UDPPortData`|UDP port for data packets||Int|2368|||
|`UDPPortPosition`|UDP port for position packets||Int|8308|||

\* Most of parameters except the last four are same as [Lidar Sensor](sensor-json-options.md#lidar). 

Details of last four parameters are as follows:

- Value of `VelodyneLidarType` can only be "VLP_16" or "VLP_32C". Note that it uses underscore ('_') not dash ('-').
- `HostName` is the IP address of the machine which receives the UDP packets (a.k.a. host machine).
- `UDPPortData` and `UDPPortPosition` are UPD ports for data packets and position packets. If more than one
Velodyne Lidar plugin is used, each one should have a unique port.

VLP-16 configuration sample:
```JSON
{
    "type": "VelodyneLidar",
    "name": "Velodyne VLP-16",
    "params": {
      "LaserCount": 16,
      "FieldOfView": 30,
      "CenterAngle": 0,
      "MinDistance": 0.5,
      "MaxDistance": 100,
      "RotationFrequency": 10,
      "MeasurementsPerRotation": 1800,
      "Compensated": true,
      "PointColor": "#ff000000",
      "Topic": "/point_cloud",
      "Frame": "velodyne",
 	  "VelodyneLidarType": "VLP_16",
      "HostName": "10.195.248.155",
      "UdpPortData": 2378,
      "UdpPortPosition": 8318
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

VLP-32C configuration sample:
```JSON
{
    "type": "VelodyneLidar",
    "name": "Velodyne VLP-32C",
    "params": {
      "MinDistance": 0.5,
      "MaxDistance": 100,
      "RotationFrequency": 10,
      "MeasurementsPerRotation": 1800,
      "Compensated": true,
      "PointColor": "#ff000000",
      "Topic": "/point_cloud",
      "Frame": "velodyne",
      "VerticalRayAngles": [
        -25.0,   -1.0,    -1.667,  -15.639,
        -11.31,   0.0,    -0.667,   -8.843,
        -7.254,  0.333,  -0.333,   -6.148,
        -5.333,  1.333,   0.667,   -4.0,
        -4.667,  1.667,   1.0,     -3.667,
        -3.333,  3.333,   2.333,   -2.667,
        -3.0,    7.0,     4.667,   -2.333,
        -2.0,   15.0,    10.333,   -1.333
        ],
      "VelodyneLidarType": "VLP_32C",
      "HostName": "10.195.248.155",
      "UdpPortData": 2378,
      "UdpPortPosition": 8318
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
