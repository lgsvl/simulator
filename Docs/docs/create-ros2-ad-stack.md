# How to create a ROS2-based AD stack with LGSVL Simulator

This tutorial works with Simulator Release [2019.05](https://github.com/lgsvl/simulator/releases/tag/2019.05)

This documentation describes how to develop ROS2 nodes to receive sensor data from LGSVL Simulator and send control commands to drive a car.

[The Lane Following model](https://github.com/lgsvl/lanefollowing) is a [ROS2](https://index.ros.org/doc/ros2/)-based Autonomous Driving stack developed with [LGSVL Simulator](https://www.lgsvlsimulator.com/). In high-level overview, the model is composed of three modules: a sensor module, a perception module, and a control module. The sensor module receives raw sensor data such as camera images from the simulator and preprocess the data before feeding into the perception module. Then, the perception module takes in the preprocessed data, extracts lane information, and predicts steering wheel commands. Finally, the control module sends a predicted control command back to the simulator, which would drive a car autonomously. 



<h2> Table of Contents</h2>
[TOC]

## Requirements

- Docker
- Python3
- ROS2
- TensorFlow, Keras
- LGSVL Simulator

## Setup

Our AD stack implementation is based on ROS2 and uses rosbridge to communicate with LGSVL Simulator. To do that, we need to prepare Ubuntu machine with ROS2 installed. We provide a Docker image containing Ubuntu 18.04 and ROS2 installed so you can just pull the Docker image and start writing code right away.

### Installing Docker CE

To install Docker CE please refer to the [official documentation](https://docs.docker.com/install/linux/docker-ce/ubuntu/). We also suggest following through with the [post installation steps](https://docs.docker.com/install/linux/linux-postinstall/).

### Installing NVIDIA Docker

Before installing nvidia-docker make sure that you have an appropriate NVIDIA driver installed.
To test if NVIDIA drivers are properly installed enter `nvidia-smi` in a terminal. If the drivers are installed properly an output similar to the following should appear.
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

The installation steps for nvidia-docker are available at the [official repo](https://github.com/NVIDIA/nvidia-docker).

### Pulling Docker Image

```
docker pull lgsvl/lanefollowing:latest
```

### What's inside Docker Image

- Ubuntu 18.04
- CUDA 9.2
- cuDNN 7.1.4.18
- Python 3.6
- TensorFlow 1.8
- Keras 2.2.4
- ROS1 melodic + rosbridge
- ROS2 Crystal + rosbridge
- Jupyter Notebook

## Creating a ROS2 Package

A ROS2 package is simply a directory and should contain files named `package.xml` and `setup.py`. Create folders as below and create `setup.py` and `package.xml`. Please note that the package name must match with the folder name of your ROS package.

```
ros2_ws/
    src/
        lane_following/
            setup.py
            package.xml
```

### `setup.py`

```
from setuptools import setup

package_name = 'lane_following'

setup(
    name=package_name,
    version='0.0.1',
    packages=[
        'train',
    ],
    py_modules=[
        'collect',
        'drive',
    ],
    install_requires=['setuptools'],
    zip_safe=True,
    author='David Uhm',
    author_email='david.uhm@lge.com',
    maintainer='David Uhm',
    maintainer_email='david.uhm@lge.com',
    keywords=[
        'ROS', 
        'ROS2', 
        'deep learning', 
        'lane following', 
        'end to end', 
        'LGSVL Simulator', 
        'Autonomous Driving'
    ],
    classifiers=[
        'Intended Audience :: Developers',
        'Programming Language :: Python',
        'Topic :: Software Development',
    ],
    description='ROS2-based End-to-End Lane Following model',
    license='BSD',
    tests_require=['pytest'],
    entry_points={
        'console_scripts': [
            'collect = collect:main',
            'drive = drive:main',
        ],
    },
)
```

### `package.xml`

```
<?xml version="1.0"?>
<package format="2">
  <name>lane_following</name>
  <version>0.0.1</version>
  <description>ROS2-based End-to-End Lane Following model</description>
  <maintainer email="david.uhm@lge.com">David Uhm</maintainer>
  <license>BSD</license>

  <exec_depend>rclpy</exec_depend>
  <exec_depend>std_msgs</exec_depend>
  <exec_depend>sensor_msgs</exec_depend>

  <test_depend>ament_copyright</test_depend>
  <test_depend>ament_flake8</test_depend>
  <test_depend>ament_pep257</test_depend>
  <test_depend>python3-pytest</test_depend>

  <export>
    <build_type>ament_python</build_type>
  </export>
</package>
```

## Building a ROS2 Package

Now, you can build your package as below:

```
source /opt/ros/crystal/setup.bash
cd ~/ros2_ws
colcon build --symlink-install
```

## Running Rosbridge

Rosbridge provides a JSON API to ROS functionality for non-ROS programs such as LGSVL Simulator. You can run rosbridge to connect your ROS node with LGSVL Simulator as below:

```
source /opt/ros/crystal/setup.bash
rosbridge
```

## Writing ROS2 Subscriber Node

ROS nodes communicate with each other by passing messages. Messages are routed via a topic with publish/subscribe concepts. A node sends out a message by publishing it to a given topic. Then, a node that is interested in a certain kind of data will subscribe to the appropriate topic. In our cases, LGSVL Simulator publishes sensor data such as camera images or LiDAR point clouds via rosbridge, and then the Lane Following model subscribes to that topic to receive sensor data, preprocesses the data, feeds them into the pretrained model, and finally computes a control command based on perceived sensor data. Below is an example of how to subscribe to sensor data topics from a ROS node. You can subscribe to a single topic only or multiple topics simultaneously.

### Subscribe to a single topic

```
import rclpy
from rclpy.node import Node
from sensor_msgs.msg import CompressedImage


class Drive(Node):
    def __init__(self):
        super().__init__('drive')
        
        self.sub_image = self.create_subscription(CompressedImage, '/simulator/sensor/camera/center/compressed', self.callback)
    
    def callback(self, msg):
        self.get_logger().info('Subscribed: {}'.format(msg.data))


def main(args=None):
    rclpy.init(args=args)
    drive = Drive()
    rclpy.spin(drive)


if __name__ == '__main__':
    main()
```

### Subscribe to multiple topics simultaneously

In order to subscribe to ROS messages of different types from multiple sources, we need to take the timestamps of those messages into account. [ROS2 Message Filters](https://github.com/intel/ros2_message_filters) is the ROS package that synchronizes incoming messages by the timestamps contained in their headers and outputs them in the form of a single callback. Install this package in your ROS2 workspace and build it.

```
cd ~/ros2_ws/src
git clone https://github.com/intel/ros2_message_filters.git
cd ..
colcon build --symlink-install
```

Then, you can import it in your python script by `import message_filters` and use it as below:

```
import rclpy
from rclpy.node import Node
from sensor_msgs.msg import CompressedImage
from geometry_msgs.msg import TwistStamped
import message_filters


class Collect(Node):
    def __init__(self):
        super().__init__('collect')
        
        sub_center_camera = message_filters.Subscriber(self, CompressedImage, '/simulator/sensor/camera/center/compressed')
        sub_left_camera = message_filters.Subscriber(self, CompressedImage, '/simulator/sensor/camera/left/compressed')
        sub_right_camera = message_filters.Subscriber(self, CompressedImage, '/simulator/sensor/camera/right/compressed')
        sub_control = message_filters.Subscriber(self, TwistStamped, '/simulator/control/command')

        ts = message_filters.ApproximateTimeSynchronizer([sub_center_camera, sub_left_camera, sub_right_camera, sub_control], 1, 0.1)
        ts.registerCallback(self.callback)
    
    def callback(self, center_camera, left_camera, right_camera, control):
        self.get_logger().info('Subscribed: {}'.format(control.twist.angular.x))


def main(args=None):
    rclpy.init(args=args)
    collect = Collect()
    rclpy.spin(collect)


if __name__ == '__main__':
    main()
```

## Writing ROS2 Publisher Node

The publisher sends data to a topic. When you create a publisher you have to tell ROS of which type the data will be. In order to drive a car autonomously, the Lane Following model publishes a predicted control command back to the simulator via rosbridge.

### Publish command back to LGSVL Simulator

```
import rclpy
from rclpy.node import Node
from geometry_msgs.msg import TwistStamped


class Drive(Node):
    def __init__(self):
        super().__init__('drive')

        self.control_pub = self.create_publisher(TwistStamped, '/lanefollowing/steering_cmd')
        self.timer_period = 0.02  # seconds
        self.timer = self.create_timer(self.timer_period, self.callback)
        self.steering = 0.

    def callback(self):
        message = TwistStamped()
        message.twist.angular.x = float(self.steering)
        self.control_pub.publish(message)
        self.get_logger().info('Publishing: {}'.format(message.twist.angular.x))


def main(args=None):
    rclpy.init(args=args)
    drive = Drive()
    rclpy.spin(drive)


if __name__ == '__main__':
    main()
```

## Running ROS2 Node

Once you have setup the rosbridge connection to LGSVL Simulator, you can launch your ROS node as follows:

```
source /opt/ros/crystal/setup.bash
source ~/ros2_ws/install/local_setup.bash
ros2 run {your_package} {your_node}
```

## References

- [Lane Following Github Repository](https://github.com/lgsvl/lanefollowing)
- [LGSVL Simulator](https://www.lgsvlsimulator.com/)
- [ROS2 Documentation](https://index.ros.org/doc/ros2/)
- [ROS2 Message Filters](https://github.com/intel/ros2_message_filters)

## Copyright and License

Copyright (c) 2019 LG Electronics, Inc.

This software contains code licensed as described in LICENSE.
