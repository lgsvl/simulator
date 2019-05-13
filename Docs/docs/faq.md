# LGSVL Simulator FAQ



#### What are the recommended system specs? What are the minimum REQUIRED system specs?

For optimal performance, we recommend that you run the simulator on a system with at least
a 4 GHz quad core CPU, Nvidia GTX 1080 graphics card, and 16GB memory or higher, running
on Windows 10. While you can run on a lower-spec system, the performance of the simulator
will be impacted and you will probably see much lower frame rates. The minimum specification
to run is a 3GHz dual core CPU, Nvidia graphics card, and 8 GB memory system.

Note that simulator runs better on Windows due to fact that Unity and nvidia drivers provide
better performance on Windows than on Linux.



#### Does the simulator run on Windows/Mac/Linux?

Officially, you can run LGSVL Simulator on Windows 10 and Ubuntu 16.04 (or later). We do not
support macOS at this time.





#### Which Unity version is required and how do I get it?

LGSVL Simulator is currently on Unity version 2018.2.4, and can be downloaded from the
Unity Download Archive. 

You can download the Windows version here: [https://unity3d.com/get-unity/download/archive](https://unity3d.com/get-unity/download/archive)

You can download the Linux version (2018.2.4f1) here: [https://beta.unity3d.com/download/fe703c5165de/public_download.html](https://beta.unity3d.com/download/fe703c5165de/public_download.html)

We are constantly working to ensure that LGSVL Simulator runs on the latest version of Unity
which supports all of our required functionality.





#### How do I setup development environment for Unity on Ubuntu?

1. Install Unity Editor dependencies:

```
    sudo apt install \
        gconf-service lib32gcc1 lib32stdc++6 libasound2 libc6 libc6-i386 libcairo2 libcap2 libcups2 \
        libdbus-1-3 libexpat1 libfontconfig1 libfreetype6 libgcc1 libgconf-2-4 libgdk-pixbuf2.0-0 \
        libgl1 libglib2.0-0 libglu1 libgtk2.0-0 libgtk-3-0 libnspr4 libnss3 libpango1.0-0 libstdc++6 \
        libx11-6 libxcomposite1 libxcursor1 libxdamage1 libxext6 libxfixes3 libxi6 libxrandr2 \
        libxrender1 libxtst6 zlib1g debconf libgtk2.0-0 libsoup2.4-1 libarchive13 libpng16-16
```

2. Download and install Unity 2018.2.4f1:

```
    curl -fLo UnitySetup https://beta.unity3d.com/download/fe703c5165de/UnitySetup-2018.2.4f1
    chmod +x UnitySetup
    ./UnitySetup --unattended --install-location=/opt/Unity --components=Unity
```

3. Install .NET Core SDK, available from [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)

   On Ubuntu 16.04 run following commands:

```
    wget -q https://packages.microsoft.com/config/ubuntu/16.04/packages-microsoft-prod.deb
    sudo dpkg -i packages-microsoft-prod.deb
    sudo apt-get install apt-transport-https
    sudo apt-get update
    sudo apt-get install dotnet-sdk-2.2
```

4. Install Mono, available from [https://www.mono-project.com/download/stable/#download-lin](https://www.mono-project.com/download/stable/#download-lin)

   On Ubuntu 16.04 run following commands:

```
    sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
    sudo apt install apt-transport-https ca-certificates
    echo "deb https://download.mono-project.com/repo/ubuntu stable-xenial main" | sudo tee /etc/apt/sources.list.d/mono-official-stable.list
    sudo apt update
    sudo apt install mono-devel
```

5. Install Visual Studio Code, available in Ubuntu Software or from [https://code.visualstudio.com/docs/setup/linux](https://code.visualstudio.com/docs/setup/linux)

6. Open VS Code and install C# extension
    1. Press Ctrl+Shift+X
    2. Search for C#
    3. Install extension `C# for Visual Studio Code (powered by OmniSharp)`

7. Install Unity Debug Extension, available here: [https://marketplace.visualstudio.com/items?itemName=Unity.unity-debug](https://marketplace.visualstudio.com/items?itemName=Unity.unity-debug)

8. Set Unity preferences to use VS Code. See instructions here: [https://code.visualstudio.com/docs/other/unity#_setup-vs-code-as-unity-script-editor](https://code.visualstudio.com/docs/other/unity#_setup-vs-code-as-unity-script-editor)
  
    * To find out where Code is installed use

```
    which code
```



#### Why are assets/scenes missing/empty after cloning from git?

We use Git LFS for large file storage to improve performance of cloning. Before cloning,
install and run `git lfs install`. Then repeat the git clone process. You can find the
Git LFS installation instructions here: [https://github.com/git-lfs/git-lfs/wiki/Installation](https://github.com/git-lfs/git-lfs/wiki/Installation)

Typically if you do not have Git LFS installed or configured then you will see the following error
when opening Unity project:

```
error CS026: The type or namespace name "WebSocketSharp" could not be found. 
Are you missing a using directive or an assembly reference?
```



#### Why do I get an error saying some files (e.g. rosbridge_websocket.launch) are missing in Apollo?

If you see that some files are missing from `ros_pkgs` folder in Apollo repository, you need
to make sure that you are cloning all submodules:

```
git clone --recurse-submodules https://github.com/lgsvl/apollo.git
```




#### ROS Bridge won't connect?

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



#### How do I control the ego vehicle (my vehicle) spawn position?

Find the "spawn_transform" game objects in scene and adjust their transform position.

If you are creating new maps make sure you add "SpawnInfo" component to empty game object. The
Simulator will use location of first game object that has SpawnInfo component.



#### How can I add a custom ego vehicle to LGSVL Simulator?

Please see our tutorial on how to add a new ego vehicle to LGSVL Simulator [here](add-new-ego-vehicle.md).



#### How can I add extra sensors to vehicles in LGSVL Simulator?

To add extra camera, or lidar you can copy&paste existing sensor in vehicle prefab. Then you can
adjust its parameters. As last step you need to add sensor component to list of active sensors in vehicle
setup component.

See instruction in GitHub issue [#68](https://github.com/lgsvl/simulator/issues/68#issuecomment-454171107) for how to add extra camera sensor.



#### How can I add a custom map to LGSVL Simulator?

Locate DefaultAssetBundleSettings.asset in Project window and add the new map .scene to the Maps list
and optionally map image. You cannot reuse map image files for different scenes. To have scene available inside Unity Editor, add the new scene to build settings.



#### How can I create or edit map annotations?

Please see our tutorial on how to add map annotations in LGSVL Simulator [here](map-annotation.md).



#### Why are pedestrians not spawning when annotated correctly?

LGSVL Simulator uses Unity's NavMesh API to work correctly.  In Unity Editor, select Window -> AI -> Navigation and bake the NavMesh.



#### Why can't I find catkin_make command when building Apollo?

Make sure you are not running Apollo dev_start/into.sh scripts as root. The will not work as root.
You need to run them as non-root user, without sudo.



#### Why is Apollo perception module turning on and off all the time?


This means that Apollo perception process is exiting with error.

Check `apollo/data/log/perception.ERROR` file for error messages.

Typically you will see following error:

```
Check failed: error == cudaSuccess (8 vs. 0) invalid device function
```

This means one of two things:

1) GPU you are using is not supported by Apollo. Apollo requires CUDA 8 compatible hardware, it won't
work if GPU is too old or too new. Apollo officially supports only GTX 1080. RTX 2080 will not work.

2) Other option is that CUDA driver is broken. To fix this you will need to restart your computer.
Check that CUDA works on your host system by running one of CUDA examples before running Apollo



#### Why does the Apollo vehicle stop at stop line and not cross intersections?

Apollo vehicle continues over intersection only when traffic light is green. If perception module
does not see traffic light, the vehicle won't move.

Check previous question to verify that perception module is running and Apollo is seeing traffic
light (top left of dreamview should say GREEN or RED).



#### Dreamview in Apollo shows "Hardware GPS triggers safety mode. No GNSS status message."

This is expected behavior. LGSVL Simulator does simulation on software level. It sends only ROS
messages to Apollo. Dreamview in Apollo has extra checks that tries to verify if hardware devices are working correctly and are not disconnected. This error message means that Apollo does not see
GPS hardware working (as it is not present).

It it safe to ignore it.



#### Why does Rviz not load the Autoware vector map?

Loading SanFrancisco map in Rviz for Autoware is a very slow process, because SanFrancisco map
has many annotations and Rviz cannot handle them efficiently. It will either crash or will take
many minutes if not hours.

You can checkout older commit of `autoware-data` repository that has annotations only for smaller
part of SanFrancisco.

```
git checkout e3cfe709e4af32ad2ea8ea4de85579b9916fe516
```



#### Why are there no maps when I make a local build?

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



#### Other questions?

See our [Github issues](https://github.com/lgsvl/simulator/issues) page, or email us at <contact@lgsvlsimulator.com>.


