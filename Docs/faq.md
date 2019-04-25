# LGSVL Simulator FAQ



##### What are the recommended system specs? What are the minimum REQUIRED system specs?

For optimal performance, we recommend that you run the simulator on a system with at least a 4 GHz quad core CPU, Nvidia GTX 1080 graphics card, and 16GB memory or higher, running on Windows 10. While you can run on a lower-spec system, the performance of the simulator will be impacted and you will probably see much lower frame rates. The minimum specification to run is a 3GHz dual core CPU, Nvidia graphics card, and 8 GB memory system.



##### Does the simulator run on Windows/Mac/Linux?

Officially, you can run LGSVL Simulator on Windows 10 and Ubuntu 16.04 (or later). We do not support Mac OS at this time.




##### Which Unity version is required and how do I get it?

LGSVL Simulator is currently on Unity version 2018.2.4, and can be downloaded with Unity Hub or from the Unity Download Archive. 

You can download the Windows version here: [https://unity3d.com/get-unity/download/archive](https://unity3d.com/get-unity/download/archive)

You can download the Linux version (2018.2.4f1) here: [https://beta.unity3d.com/download/fe703c5165de/public_download.html](https://beta.unity3d.com/download/fe703c5165de/public_download.html)

We are constantly working to ensure that LGSVL Simulator runs on the latest version of Unity which supports all of our required functionality.



##### Why are assets missing after cloning LGSVL Simulator?

We use Git LFS for large file storage to improve performance of cloning.  Before cloning, install and run Git LFS and repeat the git clone process. You can find the Git LFS installation instructions here: [https://github.com/git-lfs/git-lfs/wiki/Installation](https://github.com/git-lfs/git-lfs/wiki/Installation)



##### Why are there no maps when I make a local build?

Do not build with Unity Editor.  Use command line. 

```
mkdir build
 /opt/Unity/Editor/Unity \
    -batchmode \
    -nographics \
    -silent-crashes \
    -quit \
    -buildDestination ./build/simulator \
    -buildTarget Linux64 or Windows64 \ **choose one**
    -executeMethod BuildScript.Build \
    -projectPath . \
    -logFile /dev/stdout
```



##### ROS Bridge won't connect?

Be sure to run rosbridge.

```
roslaunch rosbridge_server rosbridge_websocket.launch
```

Note: Windows 10 local host resolves to IPv06 loopback address which causes issues, be sure to set to IPv04



##### How do I control the ego vehicle (my vehicle) spawn position?

Find the spawn_transform.prefab in the project or scene and adjust its transform.



##### How can I add a custom ego vehicle to LGSVL Simulator?

Please see our tutorial on how to add a new ego vehicle to LGSVL Simulator [here](add-new-ego-vehicle.md).



##### How can I add a custom map to LGSVL Simulator?

Locate DefaultAssetBundleSettings.asset and add the new map .scene to the Maps list and a map image named map_newmapscenename.png.  In Unity Editor, add the new scene to build settings.



##### How can I create or edit map annotations?

Please see our tutorial on how to add map annotations in LGSVL Simulator [here](map-annotation.md).



##### Why are pedestrians not spawning when annotated correctly?

LGSVL Simulator uses Unity's NavMesh API to work correctly.  In Unity Editor, select Window -> AI -> Navigation and bake the NavMesh.



##### Other questions?

See our [Github issues]() page, or email us at contact@lgsvlsimulator.com.