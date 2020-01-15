# <a name="top"></a>Sensor Plugins

Sensor plugins are custom sensors that can be added to a vehicle configuration. Sensor plugins
must be built by the simulator and the resultant bundle named `sensor_XXX` must be placed in
the `AssetBundles/Sensors` folder. If running the binary, this folder is included in the downloaded .zip. 
If running in Editor, the sensor will be built into the folder directly.
This must be done before running the simulator (running the executable or pressing `Play` in the Editor).
The sensor can be added to a vehicle configuration just like other sensors, see [here](vehicles-tab.md#how-to-change-the-configuration-of-a-vehicle)

Building sensor plugins to bundle is done as below   
1. Open `Simulator -> Build...` menu item   
2. Select sensor plugins in "Sensor" section of build window   
3. Build plugins with "Build" button  

To make custom sensor, create folder in `Assets/External/Sensors`, for example `Assets/External/Sensors/CustomCameraSensor`.

Inside this folder you must place sensor prefab with same name (`CustomCameraSensor.prefab`) that
will be used by simulator to instantiate at runtime. This prefab must have the sensor scipt added to the root of the prefab.

[![](images/sensor-prefab.png)](images/full_size_images/sensor-prefab.png)

To create a prefab:

1. Right-click in the scene hierarchy and select `Create Empty`
2. Change the name to the name of the sensor (e.g. `CustomCameraSensor`)
3. In the Inspector for this object, select `Add Component`
4. Search for the sensor script
5. Drag this object from the scene hierarchy into the project folder

[![](images/create-sensor-prefab.png)](images/full_size_images/create-sensor-prefab.png)

Additionally you can place C# scripts which will be compiled & bundled with prefab, as well as any additional Unity resources (shaders, materials,
textures, etc...).

Custom sensors must have `SensorType` attribute which specifies the kind of sensor being
implemented as well as the type of data that the sensor sends over the bridge. In addition,
it must have `SensorBase` as the base class and must implement the `OnBridgeSetup`, `OnVisualize`,
and `OnVisualizeToggle` methods. Sensors can optionally include `CheckVisible` method to prevent NPC or Pedestrians from spawning in bounds of the sensor.  See the below codeblock from the ColorCamera sensor:

```C#
namespace Simulator.Sensors
{

    // The SensorType's name will match the `type` when defining a sensor in the JSON configuration of a vehicle
    // The requiredType list is required if data will be sent over the bridge. It can otherwise be empty.
    // Publishable data types are:
    // CanBusData, CLockData, Detected2DObjectData, Detected3DObjectData, DetectedRadarObjectData,
    // GpsData, ImageData, ImuData, PointCloudData, SignalData, VehicleControlData
    [SensorType("Custom Color Camera", new[] { typeof(ImageData)})]

    // Inherits Monobehavior
    // SensorBase also defines the parameters Name, Topic, and Frame
    public partial class CustomCameraSensor : SensorBase 
    {
        private Camera Camera;
        IBridge Bridge;
        IWriter<ImageData> Writer;

        // These public variables can be set in the JSON configuration
        [SensorParameter] 
        [Range(1, 128)]
        public int JpegQuality = 75;

        //Sets up the bridge to send this sensor's data
        public override void OnBridgeSetup(IBridge bridge) 
        {
            Bridge = bridge;
            Writer = bridge.AddWriter<ImageData>(Topic);
        }

        // Defines how the sensor data will be visualized in the simulator
        public override void OnVisualize(Visualizer visualizer) 
        {
            Debug.Assert(visualizer != null);
            visualizer.UpdateRenderTexture(Camera.activeTexture, Camera.aspect);
        }

        // Called when user toggles visibility of sensor visualization
        // This function needs to be implemented, but otherwise can be empty
        public override void OnVisualizeToggle(bool state) 
        {
        }
        
        // Called when NPC and Pedestrian managers need to check if visible by sensor
        // camera or bounds before placing object in scene
        public override void CheckVisible(Bounds bounds)
        {
            var activeCameraPlanes = GeometryUtility.CalculateFrustumPlanes(Camera);
            return GeometryUtility.TestPlanesAABB(activeCameraPlanes, bounds);
        }
    }
}
```

`SensorBase` in inherited from Unity's [Monobehavior](https://docs.unity3d.com/ScriptReference/MonoBehaviour.html) so any of the `Messages` can be used to control how and when the sensor collects data.

Open-source examples are available:

- [Comfort Sensor](https://github.com/lgsvl/ComfortSensor)
