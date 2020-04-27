# <a name="top"></a> Lidar Sensor Plugin

This page introduces the Velodyne Lidar Sensor plugin, as well as how to build your own Lidar sensor plugin.

<h2> Table of Contents</h2>
[TOC]

## Velodyne Lidar Sensor Plugin [[top]] {: #velodyne-lidar-sensor-plugins data-toc-label='Velodyne Lidar Sensor Plugin'}

This sensor plugin is for [Velodyne Lidar](https://velodynelidar.com/). VLP-16, VLP-32C and VLS-128 are currently supported. 
The built asset bundle of this plugin (named `sensor_VelodyneLidarSensor`) can be found in `AssetBundles/Sensors` folder
when you unzip the downloaded LGSVL Simulator (i.e. in the same level of the Simulator executable). 

The Velodyne Lidar Sensor is implemented following exact intrinsics of real Velodyne Lidar,
such as elevation angles and azimuth offsets. Particularly, each laser beam in Velodyne Lidar sensor
has azimuth offset same as the real Lidar, while the normal [Lidar Sensor](sensor-json-options.md#lidar)
assumes all laser beams are on same vertical line (i.e. no azimuth offset).

In contrast to the standard [Lidar Sensor](sensor-json-options.md#lidar), 
which generates point cloud and publishes it via bridge,
Velodyne Lidar sensor generates data packets and position packets and sends them out via UDP socket. 
Velodyne driver running on the host machine (the machine which receives the packets) is responsible for converting these packets 
into point cloud and publish it out. This will greatly alleviate the burden on bridge bandwidth, so that the simulation can support
more sensors (e.g. camera sensors) simultaneously. See [this issue](https://github.com/lgsvl/simulator/issues/687) for an example 
of exhausted bridge bandwidth. 

### Velodyne Lidar Sensor JSON options [[top]] {: #velodyne-lidar-sensor-json-options data-toc-label='Velodyne Lidar Sensor JSON options'}

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

- Value of `VelodyneLidarType` can only be "VLP_16", "VLP_32C" or "VLS_128". Note that it uses underscore ('_') not dash ('-').
- `HostName` is the IP address of the machine which receives the UDP packets (a.k.a. host machine).
- `UDPPortData` and `UDPPortPosition` are UPD ports for data packets and position packets. If more than one Velodyne Lidar plugin is used, each one should have a unique port.
- `VerticalRayAngles`, `LaserCount`, `FieldOfView`, and `CenterAngle` will be ignored for Velodyne Lidar since they will be set internally following the corresponding model spec.

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
      "VelodyneLidarType": "VLP_32C",
      "HostName": "127.0.0.1",
      "UdpPortData": 2368,
      "UdpPortPosition": 8308
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

### Velodyne Lidar Sensor Usage [[top]] {: #velodyne-lidar-sensor-usage data-toc-label='Velodyne Lidar Sensor Usage'}

#### Running with Autoware
Autoware is based on ROS. For ROS-based systems, [ROS Velodyne driver](https://github.com/ros-drivers/velodyne)
can be used. 

Detailed steps of running ROS Velodyne driver are as follows:

<span>1.</span> Create a workspace folder and enter it
```bash 
mkdir velodyne_ws && cd velodyne_ws
```

<span>2.</span> Clone ROS Velodyne driver into `src` folder
```bash
git clone https://github.com/ros-drivers/velodyne.git src
```

<span>3.</span> Build the Velodyne drive as a ROS node
```bash
catkin_make
```

<span>4.</span> Setup running environment
```bash
source /opt/$ROS_DISTRO/setup.bash
source devel/setup.bash
```

<span>5.</span> Configuration of device IP
Before running the Velodyne driver, you need to modify the launch files to setup device IP (i.e. the IP of the machine where the LGSVL Simulator is running).

* For VLP-16, edit velodyne_ws/src/velodyne/velodyne_pointcloud/launch/VLP16_points.launch and put the device IP after `device_ip`.
* For VLP-32, edit velodyne_ws/src/velodyne/velodyne_pointcloud/launch/VLP-32C_points.launch and put the device IP after `device_ip`.
* ROS Velodyne driver does not support VLS-128 for now. For more details please refer to the [official page](http://wiki.ros.org/velodyne_driver#Supported_Devices).

<span>6.</span> Launch Velodyne driver

For VLP-16:
```bash
roslaunch velodyne_pointcloud VLP16_points.launch
```

For VLP-32C:
```bash
roslaunch velodyne_pointcloud VLP-32C_points.launch
```

If you have LGSVL Simulator running on client machine, you should be able to see UDP packets received on both data port and position port, 
and ROS topic `/velodyne_points` is published by the driver. You can also use RViz to visualize the point cloud in that topic.

Fig. 1 shows point cloud of VLP-32C visualized in the simulator,

|[![](images/visualize-VLP-32C.png)](images/full_size_images/visualize-VLP-32C.png)|
|:--:|
| Fig. 1: Visualized point clouds of VLP-32C Lidar in LGSVL Simulator. |

and Fig. 2 shows the same point cloud visualized in RViz  (click to see in full resolution):

|[![](images/rviz-VLP-32c.png)](images/full_size_images/rviz-VLP-32c.png)|
|:--:|
| Fig. 2: Visualized point clouds of VLP-32C Lidar in RViz. |

Note that the output topic name (`/velodyne_points`) of ROS Velodyne driver is hard-coded and not configurable, while Autoware assumes point cloud published into ROS topic `/points_raw`.
To have ROS Velodyne driver running with Autoware, you have to either use
`<remap>` tag in Autoware launch files to may `/velodyne_points` to `/points_raw`, or modify the topic name in the source code of ROS Velodyne driver and rebuild it.

#### Running with Apollo 5.0
Apollo 5.0 is based on CyberRT and comes with its own [Velodyne driver](https://github.com/lgsvl/apollo-5.0/tree/simulator/modules/drivers/velodyne).

Detailed steps of running ROS Velodyne driver are as follows:

<span>1.</span> Follow [these instructions](apollo5-0-instructions.md) to start Apollo 5.0 and launch bridge.

<span>2.</span> (optional) Configure the Lidar model if your Lidar setting is different to the default setting of Apollo 5.0.

To configure the Lidar model, you can edit [velodyne.dag](https://github.com/lgsvl/apollo-5.0/blob/simulator/modules/drivers/velodyne/dag/velodyne.dag) file.
Note that if more than one Lidar is used, each has different data port and position port (configured in their corresponding [.conf files](https://github.com/lgsvl/apollo-5.0/tree/simulator/modules/drivers/velodyne/conf).
You need to set `UDPPortData` and `UDPPortPosition` for each Velodyne Lidar sensor accordingly.

The default launch file and dag file of Apollo 5.0 use VLS-128 and VLP-16 Lidars. If you need to use VLP-32C, in addition to add VLP-32C to dag file,
you may need to modify [static_transform_conf.pb.txt](https://github.com/lgsvl/apollo-5.0/blob/simulator/modules/transform/conf/static_transform_conf.pb.txt) to
include your own VLP-32C extrinsics if you want to get compensated point cloud.

<span>3.</span> Launch `GPS`, `Localization`, `Transform`, and `Velodyne` modules in `Module Controller` page of Dreamview.
Fig. 3 shows the Dreamview web interface:

|[![](images/dreamview-VLP-32C.png)](images/full_size_images/dreamview-VLP-32C.png)|
|:--:|
| Fig. 3: Dreamview web interface. |

On the Simulator side, you can add Velodyne Lidar sensor into our [sample JSON](apollo5-0-json-example.md).

If you have LGSVL Simulator running on client machine, you should be able to see UDP packets received on both data port and position port, 
and `cyber_monitor` should shows point clouds published into corresponding Cyber channels. 
You can also use `cyber_visualizer` to visualize the point cloud in those channels. 
Fig. 4 shows the point cloud of VLP-32C Lidar visualized in Cyber Visualizer (click to see in full resolution):

|[![](images/cyber_visualizer-VLP-32C.png)](images/full_size_images/cyber_visualizer-VLP-32C.png)|
|:--:|
| Fig. 4: Visualized point clouds of VLP-32C Lidar in Cyber Visualizer. |

## Build Your Own Lidar Sensor Plugin [[top]] {: #build-your-own-lidar-sensor-plugin data-toc-label='Build Your Own Lidar Sensor Plugin'}

If you want to build your own Lidar sensor plugin to support other Lidar models, you can follow the 
[general instructions](sensor-plugins.md) on building sensor plugins.

Instead of deriving your plugin class from [`SensorBase`](https://github.com/lgsvl/simulator/blob/master/Assets/Scripts/Sensors/SensorBase.cs), 
you can derive your class from [`LidarSensorBase`](https://github.com/lgsvl/simulator/blob/master/Assets/Scripts/Sensors/LidarSensorBase.cs), 
so that you can reuse most of the code there, focusing only on raw data generation and sending.