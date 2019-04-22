# LGSVL Simulator FAQ



##### What are the recommended system specs?

4 GHz Dual Core CPU, Nvidia GTX 1080, Windows 10 64 bit



##### Which Unity version is required and how do I get it?

Unity 2018.2.4f1 and it can be downloaded with Unity Hub or from the Unity Download Archive



##### Why are assets missing after cloning LGSVL Simulator?

We use Git LFS for large file storage to improve performance of cloning.  Before cloning, install and run Git LFS and repeat the git clone process.



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



##### How to control the Ego vehicle spawn position?

Find the spawn_transform.prefab in the project or scene and adjust it's transform.



##### How can I add a custom Ego vehicle to LGSVL Simulator?

Locate ROSAgentManager.prefab in the project and add the new agent to the AgentPrefabs list.



##### How can I add a custom map to LGSVL Simulator?

Locate DefaultAssetBundleSettings.asset and add the new map .scene to the Maps list and a map image named map_newmapscenename.png.  In Unity Editor, add the new scene to build settings.



##### How can I create or edit map annotation?

In Unity Editor, select Window -> MapToolPanel.  This tool is for editing or creating scene annotation.



##### Why are pedestrians not spawning when annotated correctly?

LGSVL Simulator uses Unity's NavMesh API to work correctly.  In Unity Editor, select Window -> AI -> Navigation and bake the NavMesh.