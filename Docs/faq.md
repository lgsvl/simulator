# LGSVL Simulator FAQ



##### What are the recommended system specs? What are the minimum REQUIRED system specs?

For optimal performance, we recommend that you run the simulator on a system with at least
a 4 GHz quad core CPU, Nvidia GTX 1080 graphics card, and 16GB memory or higher, running
on Windows 10. While you can run on a lower-spec system, the performance of the simulator
will be impacted and you will probably see much lower frame rates. The minimum specification
to run is a 3GHz dual core CPU, Nvidia graphics card, and 8 GB memory system.

Note that simulator runs better on Windows due to fact that Unity and nvidia drivers provide
better performance on Windows than on Linux.



##### Does the simulator run on Windows/Mac/Linux?

Officially, you can run LGSVL Simulator on Windows 10 and Ubuntu 16.04 (or later). We do not
support macOS at this time.



##### Which Unity version is required and how do I get it?

LGSVL Simulator is currently on Unity version 2018.2.4, and can be downloaded from the
Unity Download Archive. 

You can download the Windows version here: [https://unity3d.com/get-unity/download/archive](https://unity3d.com/get-unity/download/archive)

You can download the Linux version (2018.2.4f1) here: [https://beta.unity3d.com/download/fe703c5165de/public_download.html](https://beta.unity3d.com/download/fe703c5165de/public_download.html)

We are constantly working to ensure that LGSVL Simulator runs on the latest version of Unity
which supports all of our required functionality.



##### Why are assets/scenes are missing/empty after cloning from git?

We use Git LFS for large file storage to improve performance of cloning. Before cloning,
install and run `git lfs install`. Then repeat the git clone process. You can find the
Git LFS installation instructions here: [https://github.com/git-lfs/git-lfs/wiki/Installation](https://github.com/git-lfs/git-lfs/wiki/Installation)

Typically if you do not have Git LFS installed or configured then you will see following error
when opening Unity project:

```
error CS026: The type or namespace name "WebSocketSharp" could not be found. Are you missing a using directive or an assembly reference?
````


##### Why rosbridge_websocket.launch are missing after cloning Apollo repository from git?

If you see that some files are missing from `ros_pkgs` folder in Apollo repository, you need
to make sure that you are cloning all submodules:

```
git clone --recurse-submodules https://github.com/lgsvl/apollo.git
```


##### Why I cannot find catkin_make command when building Apollo?

Make sure you are not running Apollo dev_start/into.sh scripts as root. The will not work as root.
You need to run them as non-root user, without sudo.



##### Why Apollo perception module is turning on or off all the time?


This means that Apollo perception process is either exiting with error.

Check `apollo/data/log/perception.ERROR` file for error messages.

Typically you will see following error:

```
Check failed: error == cudaSuccess (8 vs. 0) invalid device function
```

This means one of two things:

1) GPU you are using is not supported by Apollo. Apollo requires cuda 8 compatible hardware, it won't
work if GPU is too old or too new. Apollo officially supports only GTX 1080. 2080 will not work

2) Other option is that cuda driver is broken. To fix this you will need to restart computer.
Check that cuda works on your host system by running one of cuda examples before running Apollo



##### Why Apollo vehicle stops at stop line and does not cross intersection?

Apollo vehicle continues over intersection only when traffic light is green. If perception module
does not see traffic light, the vehicle won't moveu.

Check previous question to verify that perception module is running and Apollo is seeing traffic
light (top left of dreamview should say GREEN or RED).



##### Dreamview in Apollo shows "Hardware GPS triggers safety mode. No GNSS status message."

This is expected behaviour. LGSVL Simulator does simulation on software level. It sends only ROS
messages to Apollo. Dreamview in Apollo has extra checks that tries to verify if hardware device
is working correctly and is not disconnected. This error message means that Apollo does not see
GPS harwarde working (as it is not present).

It it safe to ignore it.



##### Why rviz doesn't load Autoware vector map?

Loading SanFransico map in rviz for Autoware is very slow process, because SanFrancisco map
has too many annotations. Rviz cannot handle them efficiently. It will either crash or will take
many minutes if not hours.

You can checkout older commit of `autoware-data` repository that has annotations only for smaller
part of SanFrancisco.
```
git checkout e3cfe709e4af32ad2ea8ea4de85579b9916fe516
```



##### How does GPS sensor know latitude and longitude on the simulated map?

Each map has `MapOrigin` game object that has northing/esting values for (0,0,0) coordinates on the map.
Simulator uses these values calculate latitude/longitude for vehicle.

MapOrigin object has extra values for map rotation (Angle) and altitude offset (AltitudeOffset). You
can adjust these values to fit virtual map to real world location.



##### Why are there no maps when I make a local build?

Default File -> Build... menu will not work inside Unity Editor. Simulator requires
running extra steps to correctly produce working map.

On Linux you can do the build with following command:

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

On Windows you can do the build with following command:
```
mkdir build
"C:\Program Files\Unity\Editor\Unity.exe" ^
    -batchmode ^
    -nographics ^
    -silent-crashes ^
    -quit ^
    -buildDestination build\simulator.exe ^
    -buildTarget Windows64 ^
    -executeMethod BuildScript.Build ^
    -projectPath .
```

You need to run these commands from folder where project is cloned. Unity must be closed when
build is running.



##### ROS Bridge won't connect?

First make sure you are running rosbridge.

If using our Apollo docker image, run:


```
rosbridge.sh
```

For standalone ROS environments run:

```
roslaunch rosbridge_server rosbridge_websocket.launch
```

If you are running ROS bridge on different machine, verify that simulator can connect to it and
you do not have firewall blocking ports.



##### How do I control the ego vehicle (my vehicle) spawn position?

Find the "spawn_transform" game objects in scene and adjust their transform position.

If you are creating new maps make sure you add "SpawnInfo" component to empty game object. The
Simulator will use location of first game object that has SpawnInfo component.



##### How can I add a custom ego vehicle to LGSVL Simulator?

Please see our tutorial on how to add a new ego vehicle to LGSVL Simulator [here](add-new-ego-vehicle.md).



##### How can I add extra sensor to vehicles in LGSVL Simulator?

To add extra camera, or lidar you can copy&paste existing sensor in vehicle prefab. Then you can
adjust its parameters. As last step you need to add sensor component to list of active sensors in vehicle
setup component.

See instruction in GitHub issue [#68](https://github.com/lgsvl/simulator/issues/68#issuecomment-454171107) for
how to add extra camera sensor.



##### How can I add a custom map to LGSVL Simulator?

Locate DefaultAssetBundleSettings.asset in Project window and add the new map .scene to the Maps list
and optionally map image. You cannot reuse map image files for different scenes. To have scene available
inside Unity Editor, add the new scene to build settings.



##### How can I create or edit map annotations?

Please see our tutorial on how to add map annotations in LGSVL Simulator [here](map-annotation.md).



##### Why are pedestrians not spawning when annotated correctly?

LGSVL Simulator uses Unity's NavMesh API to work correctly.  In Unity Editor, select Window -> AI -> Navigation
and bake the NavMesh.



##### Other questions?

See our [Github issues]() page, or email us at contact@lgsvlsimulator.com.
