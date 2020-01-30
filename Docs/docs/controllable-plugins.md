# <a name="top"></a>Controllable Plugins

Controllable plugins are custom controllables that can be added to a scene at runtime with the API. Controllable plugins must be built by the simulator using Simulator Build menu before running the simulator (running the executable or pressing `Play` in the Editor).



**Building controllable plugins**   

1. Open `Simulator -> Build...` menu item   
2. Select controllable plugins in "Controllables" section of build window   
3. Build plugins with "Build" button

[![](images/controllables-build.png)](images/full_size_images/controllables-build.png)

The bundle named `controllable_XXX` will be placed in the `AssetBundles/Controllables` folder. If running the binary, this folder is included in the downloaded .zip.



**Creating controllable plugin**

Create a folder in `Assets/External/Controllables ` , e.g. Assets/External/Controllables/TrafficCone.

Inside this folder you must place the controllable prefab with same name (`TrafficCone.prefab`) that will be used by simulator to instantiate at runtime, the controllable logic script and Models folder with materials and textures. 

[![](images/controllables-folder-structure.png)](images/full_size_images/controllables-folder-structure.png)

This prefab must have a logic script that inherits interface IControllable and added to the root of the prefab.    The controllable tag and layer for traffic cone is set to Obstacle.  If collisions are desired, then add a collider components.  A Rigidbody component can be added if velocity changes are desired, if not, velocity change commands will be ignored.

[![](images/controllable-prefab-components.png)](images/full_size_images/controllable-prefab-components.png)

To create a prefab:

1. Right-click in the scene hierarchy and select `Create Empty`
2. Change the name to the name of the controllable (e.g. `TrafficCone`)
3. In the Inspector for this object, select `Add Component`
4. Search for the controllable script
5. Add Rigidbody if needed
6. Add Collider if needed
7. Drag this object from the scene hierarchy into the project folder to create a new prefab, delete prefab in Hierarchy panel

[![](images/controllable-create-prefab.png)](images/full_size_images/controllable-create-prefab.png)



**Controllable Logic**

Additionally you can place a C# script which will be compiled & bundled with prefab, as well as any additional Unity resources (shaders, materials, textures, etc...).

Controllable scripts must inherit interface `IControllable` which allows controllables to receive API commands. In addition, it must implement all interface variables and methods.  See the below code block from TrafficCone.cs:

```C#
namespace Simulator.Controllable
{
    public class TrafficCone : MonoBehaviour, IControllable,
    {
        public bool Spawned { get; set; }
        public string UID { get; set; }
        public string Key => UID;
        public string ControlType { get; set; } = "cone";
        public string CurrentState { get; set; }
        public string[] ValidStates { get; set; } = new string[] { };
        public string[] ValidActions { get; set; } = new string[] { };
        public string DefaultControlPolicy { get; set; } = "";
        public string CurrentControlPolicy { get; set; }
        
        private void Awake()
        {
            CurrentControlPolicy = DefaultControlPolicy;
            CurrentState = "";
        }

        public void Control(List<ControlAction> controlActions)
        {
            for (int i = 0; i < controlActions.Count; i++)
            {
                var action = controlActions[i].Action;
                var value = controlActions[i].Value;

                switch (action)
                {
                    case = "state": // set in policy parse
            		    CurrentState = value;
                        // switch (CurrentState)
                        //     set logic
            		    break;
                    default:
                        Debug.LogError($"'{action}' is an invalid action for '{ControlType}'");
                        break;
                }
            }
        }
    }
}
```

