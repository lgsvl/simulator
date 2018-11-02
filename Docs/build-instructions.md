# Instructions to build standalone executable

Build steps for Ubuntu host:

1. Install Unity 2018.2.4f1:
   https://beta.unity3d.com/download/fe703c5165de/public_download.html

 install into the /opt/Unity folder:

```
 chmod +x ~/Downloads/UnitySetup-2018.2.4f1
 ./UnitySetup-2018.2.4f1 --unattended --install-location=/opt/Unity --components=Unity,Windows-Mono
```

 run Unity and make sure it's activated

1. Make sure you have [git-lfs](https://git-lfs.github.com/) installed **before cloning this repository**. 
2. Clone simulator from GitHub:

```
 git clone https://github.com/lgsvl/simulator.git
```

1. Run build:

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

1. Run rosbridge:

```
   roslaunch rosbridge_server rosbridge_websocket.launch
```

1. Run simulator from build/simulator
   Choose "Free Roaming" -> "DuckieDowntown" as map -> "Duckiebot-duckietown-ros1" as robot
   make sure it's connected, click "RUN" - make sure it's running, you can operate the robot
2. Run rviz or rqt_image_view and see image from topic "/simulator/camera_node/image/compressed"