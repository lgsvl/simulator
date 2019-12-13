# Autoware.Auto with LGSVL Simulator [](#top)

<h2> Table of Contents</h2>
[TOC]

## Overview [[top]] {: #general data-toc-label='Overview'}

This guide describes setting up and using Autoware.Auto with the LGSVL simulator. As Autoware.Auto is still under-development, full self-driving is not yet possible. This guide will focus on running individual modules which have been implemented.

## Setup [[top]] {: #setup data-toc-label='Setup'}

### Requirements [[top]] {: #requirements data-toc-label='Requirements'}

- Linux operating system
- Nvidia graphics card

#### Installing Docker CE [[top]] {: #installing-docker-ce data-toc-label='Installing Docker CE'}

To install Docker CE please refer to the [official documentation](https://docs.docker.com/install/linux/docker-ce/ubuntu/). We also suggest following through with the [post installation steps](https://docs.docker.com/install/linux/linux-postinstall/).

#### Installing Nvidia Docker [[top]] {: #installing-nvidia-docker data-toc-label='Installing Nvidia Docker'}

- Before installing nvidia-docker make sure that you have an appropriate Nvidia driver installed. To test if nvidia drivers are properly installed enter `nvidia-smi` in a terminal. If the drivers are installed properly an output similar to the following should appear.

```
    +-----------------------------------------------------------------------------+
    | NVIDIA-SMI 390.87                 Driver Version: 390.87                    |
    |-------------------------------+----------------------+----------------------+
    | GPU  Name        Persistence-M| Bus-Id        Disp.A | Volatile Uncorr. ECC |
    | Fan  Temp  Perf  Pwr:Usage/Cap|         Memory-Usage | GPU-Util  Compute M. |
    |===============================+======================+======================|
    |   0  GeForce GTX 108...  Off  | 00000000:65:00.0  On |                  N/A |
    |  0%   59C    P5    22W / 250W |   1490MiB / 11175MiB |      4%      Default |
    +-------------------------------+----------------------+----------------------+
                                                                                
    +-----------------------------------------------------------------------------+
    | Processes:                                                       GPU Memory |
    |  GPU       PID   Type   Process name                             Usage      |
    |=============================================================================|
    |    0      1187      G   /usr/lib/xorg/Xorg                           863MiB |
    |    0      3816      G   /usr/bin/gnome-shell                         305MiB |
    |    0      4161      G   ...-token=7171B24E50C2F2C595566F55F1E4D257    68MiB |
    |    0      4480      G   ...quest-channel-token=3330599186510203656   147MiB |
    |    0     17936      G   ...-token=5299D28BAAD9F3087B25687A764851BB   103MiB |
    +-----------------------------------------------------------------------------+
```

- Install [nvidia docker](https://github.com/NVIDIA/nvidia-docker).

    *Note:* For docker 19.03 and newer nvidia GPUs are natively supported as devices in docker runtime, and nvidia-docker2 is deprecated, however, because Autoware.auto uses the `--runtime nvidia` argument nvidia-docker2 will need to be installed even for newer docker versions.

### Installing Autoware.auto [[top]] {: #installing-autoware-auto data-toc-label='Installing Autoware.auto'}

- Follow the [installation and development setup](https://autowarefoundation.gitlab.io/autoware.auto/AutowareAuto/installation-and-development.html) guide for Autoware.auto.

### Simulator installation [[top]] {: #simulator-installation data-toc-label='Simulator Installation'}

- Download and extract the latest [simulator release](https://github.com/lgsvl/simulator/releases/latest)
- (Optional) Download the latest [PythonAPI release](https://github.com/lgsvl/PythonAPI/releases/latest) (make sure the release version matches the simulator) and install it using pip:

```bash
cd PythonAPI
pip3 install --user .
```

### Install ROS2 dashing [[top]] {: #install-ros2-dashing data-toc-label='Install Ros2 dashing'}

- Follow [these steps](https://index.ros.org/doc/ros2/Installation/Dashing/Linux-Install-Debians).

### Install the ROS2 Web Bridge [[top]] {: #install-ros2-web-bridge data-toc-label='Install Ros2 Web Bridge'}

- Clone the [ROS2 web bridge](https://github.com/RobotWebTools/ros2-web-bridge):

```bash
cd adehome
git clone -b 0.2.7 https://github.com/RobotWebTools/ros2-web-bridge.git
```

- Install nodejs v10:

```bash
curl -sL https://deb.nodesource.com/setup_10.x | sudo -E bash -
sudo apt-get install -y nodejs
cd ros2-web-bridge
npm install
```

## Run Simulator alongside Autoware.Auto
The ROS2 web bridge allows the simulator and Autoware.auto to communicate. To test this connection we can visualize sensor data from the simulator in rviz2 (running in the Autoware.auto container).
rviz2 -d /home/"${USER}"/AutowareAuto/install/share/autoware_auto_examples/rviz2/autoware.rviz
- Start the Autoware.Auto containers:

```bash
cd adehome/Autoware.Auto
ade start -- --net=host --privileged # to allow connect to rosbridge
```

- Enter the container and start rviz2:

```bash
ade enter
export LD_LIBRARY_PATH=${LD_LIBRARY_PATH}:/usr/local/nvidia/lib64/
rviz2 -d /home/"${USER}"/AutowareAuto/install/autoware_auto_examples/share/autoware_auto_examples/rviz2/autoware.rviz
```

- Start the LGSVL Simulator by launching the executable and click on the button to open the web UI.
    
- In the Vehicles tab look for `Lexus2016RXHybrid`. If not available download it from [here](https://content.lgsvlsimulator.com/vehicles/lexusrx2016/) and follow [these instructions](https://www.lgsvlsimulator.com/docs/vehicles-tab/#how-to-add-a-vehicle) to add it.
    - Click on the wrench icon for the Lexus vehicle:
    - Change the bridge type to `ROS2`
    - Use the following JSON configuration:
```
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
      "Topic": "/test_velodyne_node_cloud_front",
      "Frame": "base_link"
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
- Switch to the Simulations tab and click the `Add new` button:
    - Enter a name and switch to the `Map & Vehicles` tab
    - Select a map from the drop down menu. If none are available follow [this guide](https://www.lgsvlsimulator.com/docs/maps-tab/#where-to-find-maps) to get a map.
    - Select the `Lexus2016RXHybrid` from the drop down menu. In the bridge connection box to the right enter the bridge address (default: `localhost:9090`)
    - Click submit
    Select the simulation and press the play button in the bottom right corner of the screen

- Launch ROS2 web bridge in a new terminal:

```bash
source /opt/ros/dashing/setup.bash
cd adehome/ros2-web-bridge
node bin/rosbridge.js
```

You should now be able to see the lidar point cloud in rviz (see image below).

If the pointcloud is not visible make sure the fixed frame is set to `base_link` and that a PointCloud2 message is added which listens on the `/test_velodyne_node_cloud_front` topic.

![](images/autoware-auto-rviz.png)



## Copyright and License [[top]] {: #copyright-and-license data-toc-label='Copyright and License'}

Copyright (c) 2019 LG Electronics, Inc.

This software contains code licensed as described in LICENSE.
