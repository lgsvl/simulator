# <a name="top"></a>Sensor Plugins

Sensor plugins are custom sensors that can be added to a vehicle configuration. Sensor plugins
must be built by the simulator and the resultant bundle named `sensor_XXX` must be placed in
the `AssetBundles/Sensors` folder.

Building sensor plugins to bundle is done with `Simulator -> Build Sensors` menu item.

To make custom sensor, create folder in `Assets/External/Sensors`, for example `Assets/External/Sensors/CustomCameraSensor`.

Inside this folder you must place sensor prefab with same name (`CustomCameraSensor.prefab`) that
will be used by simulator to instantiate at runtime. Additionally can place C# scripts which will
be compiled & bundled with prefab, as well as any additional Unity resources (shaders, materials,
textures, etc...).

Custom sensors must have `SensorType` attribute which specifies the kind of sensor being
implemented as well as the type of data that the sensor sends over the bridge. In addition,
it must have `SensorBase` as the base class and must implement the `OnBridgeSetup`, `OnVisualize`,
and `OnVisualizeToggle` methods. See the below codeblock from the ColorCamera sensor:

```C#
namespace Simulator.Sensors
{
    // The name will match the `type` when defining a sensor in the JSON configuration of avehicle
    // Available data types are:
    // CanBusData, CLockData, Detected2DObjectData, Detected3DObjectData, DetectedRadarObjectData,
    // GpsData, ImageData, ImuData, PointCloudData, SignalData, VehicleControlData
    [SensorType("Custom Color Camera", new[] { typeof(ImageData)})]

    // Inherits Monobehavior
    // SensorBase also defines the parameters Name, Topic, and Frame
    public partial class CustomCameraSensor : SensorBase 
    {
        private Camera Camera;

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
        public override void OnVisualizeToggle(bool state) 
        {
        }
    }
}
```

`SensorBase` in inherited from Unity's [Monobehavior](https://docs.unity3d.com/ScriptReference/MonoBehaviour.html) so any of the `Messages` can be used to control how and when the sensor collects data.

Open-source examples are available:

- [Comfort Sensor](https://github.com/lgsvl/comfort-sensor)
