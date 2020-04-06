# <a name="top"></a> Velodyne Lidar Sensor

This sensor plugin is for Velodyne Lidar. VLP-16 and VLP-32C are currently supported. 

<h2> Table of Contents</h2>
[TOC]

## Velodyne Lidar Sensor JSON options [[top]] {: #velodyne-lidar-sensor-json-options}

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

## Velodyne Lidar Sensor Usage [[top]] {: #velodyne-lidar-sensor-usage}

Different to standard [Lidar Sensor](sensor-json-options.md#lidar), which generates point cloud and publishes it via bridge,
Velodyne Lidar sensor generates data packets and position packets and send them out via UDP. Velodyne driver running on the host
machine is reponsible for converting these packets into point cloud. 

### Running with Autoware
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

Here is VLP-32C visualized in the simulator:

[![](images/visualize-VLP-32C.png)](images/full_size_images/visualize-VLP-32C.png)

and here is the point cloud visualized in RViz:

[![](images/rviz-VLP-32c.png)](images/full_size_images/rviz-VLP-32c.png)

Note that the output topic name (`/velodyne_points`) of ROS Velodyne driver is hard-coded and not configurable, while Autoware assumes point cloud published into ROS topic `/points_raw`.
To have ROS Velodyne driver running with Autoware, you have to either use
`<remap>` tag in Autoware launch files to may `/velodyne_points` to `/points_raw`, or modify the topic name in the source code of ROS Velodyne driver and rebuild it.

### Running with Apollo 5.0
Apollo 5.0 is based on CyberRT and comes with its own [Velodyne driver](https://github.com/lgsvl/apollo-5.0/tree/simulator/modules/drivers/velodyne).
You can simply launch Apollo's Velodyne driver in Dreamview (by click `Velodyne` module in `Module Controller` page).
Since Apollo Velodyne driver uses position packet to compensate the point cloud, you also need to launch `GPS`, `localization` and `transform` modules in the same page.


On the Simulator side, you can add Velodyne Lidar sensor into our [sample JSON](apollo5-0-json-example.md).
Since other sensors send messages via bridge, you also need to run `bridge.sh` inside Apollo docker.

To configure the Lidar model, you can edit [velodyne.dag](https://github.com/lgsvl/apollo-5.0/blob/simulator/modules/drivers/velodyne/dag/velodyne.dag) file.
Note that if more than one Lidar is used, each has different data port and position port (configured in their corresponding [.conf files](https://github.com/lgsvl/apollo-5.0/tree/simulator/modules/drivers/velodyne/conf).
You need to set `UDPPortData` and `UDPPortPosition` for each Velodyne Lidar sensor accordingly.

The default launch file and dag file of Apollo 5.0 use VLS-128 and VLP-16 Lidars. If you need to use VLP-32C, in addition to add VLP-32C to dag file,
you may need to modify [static_transform_conf.pb.txt](https://github.com/lgsvl/apollo-5.0/blob/simulator/modules/transform/conf/static_transform_conf.pb.txt) to
include your own VLP-32C extrinsics if you want to get compensated point cloud.

The following screenshot shows the Dreamview interface:

[![](images/dreamview-VLP-32C.png)](images/full_size_images/dreamview-VLP-32C.png)

and next screenshot shows the point cloud visualized in cyber_visualizer:

[![](images/cyber_visualizer-VLP-32C.png)](images/full_size_images/cyber_visualizer-VLP-32C.png)
