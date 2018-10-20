# A ROS/ROS2 Multi-robot Simulator for Autonomous Vehicles

## Summary

We present a Unity-based multi-robot simulator for autonomous vehicle developers. We provide an open-source, out-of-the-box solution which can meet the needs of developers wishing to focus on testing their autonomous vehicle algorithms. It currently has integration with the MIT DuckieTown, Autoware, and [Apollo](https://github.com/lgsvl/apollo) platforms, can generate HD maps, and be immediately used for testing and validation of a whole system with little need for custom integrations. We hope to build a collaborative community among robotics and autonomous vehicle developers by open sourcing our efforts.

*To use the simulator with Apollo, after following the [build steps](#build) for the simulator, follow the guide on our [Apollo fork](https://github.com/lgsvl/apollo).*

## Video

[![A ROS/ROS2 Multi-robot Simulator for Autonomous Vehicles](http://img.youtube.com/vi/uCaOzrZ8wls/0.jpg)](https://youtu.be/uCaOzrZ8wls)


## Build

Build steps for Ubuntu host:

1. Install Unity 2018.2.4f1:
 https://beta.unity3d.com/download/fe703c5165de/public_download.html

 install into the /opt/Unity folder:
```
 chmod +x ~/Downloads/UnitySetup-2018.2.4f1
 ./UnitySetup-2018.2.4f1 --unattended --install-location=/opt/Unity --components=Unity,Windows-Mono
```
 run Unity and make sure it's activated

2. Make sure you have [git-lfs](https://git-lfs.github.com/) installed **before cloning this repository**. 

3. Clone simulator from GitHub:
```
 git clone https://github.com/lgsvl/simulator.git
```

4. Run build:
```
 mkdir build
 /opt/Unity/Editor/Unity \
    -batchmode \
    -nographics \
    -silent-crashes \
    -quit \
    -buildDestination ./build/simulator \
    -buildTarget Linux64 \
    -executeMethod BuildScript.Build \
    -projectPath . \
    -logFile /dev/stdout
```

Test simulator:

5. Run rosbridge:
```
   roslaunch rosbridge_server rosbridge_websocket.launch
```

6. Run simulator from build/simulator
   Choose "Free Roaming" -> "DuckieDowntown" as map -> "Duckiebot-duckietown-ros1" as robot
   make sure it's connected, click "RUN" - make sure it's running, you can operate the robot

7. Run rviz or rqt_image_view and see image from topic "/simulator/camera_node/image/compressed"


## Copyright and License

Copyright (c) 2018 LG Electronics, Inc.

This software contains code licensed as described in LICENSE.
