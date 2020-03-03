# EGO Vehicle Dynamics[](#top)

LGSVL Simulator supports multiple dynamics models for EGO vehicles.  The default dynamics model is a C# based model that uses Unity's PhysX physics engine and components.  The model receives controller input and applies force to the Unity wheel colliders.  For users who would like to change or replace vehicle dynamics with their own models, the simulator offers the following ways to do so.

- Simple Model Interface - pure C# dynamics

- Full Model Interface - [FMI 2.0](https://fmi-standard.org/) supported dynamics




## Initial Setup

In order to alter the dynamics model in LGSVL Simulator, there are a few steps needed to setup the development environment. [Link](build-instructions.md)

1. Download and install Unity Hub
2. Download and install Unity 2019.1.10f1 with Windows and Linux build support modules
3. Download and install Node.js
4. Install git-lfs before cloning LGSVL Simulator
5. Clone LGSVL Simulator repository from GitHub
6. Clone CubeTown repository from GitHub into Assets -> External -> Environments -> CubeTown folder in the newly cloned LGSVL Simulator clone 
7. Clone Jaguar2015XE repository from GitHub into Assets -> External -> Vehicles -> Jaguar2015XE folder in the newly cloned LGSVL Simulator clone
8. Navigate to WebUI folder in the root folder of the LGSVL Simulator
9. Open a terminal window and run: npm install
10. Then in the same terminal window run: npm run pack or you can run Simulator -> Build WebUI in Simulator project in Unity Editor
11. Open LGSVL Simulator from Unity Hub




## Simple Model Interface

Simple Model Interface is our pure C# dynamic model.  LGSVL Simulator vehicles that are provided, use the VehicleSMI class.  It is located in Assets -> Scripts -> Dynamics -> Examples.  This class inherits IVehicleDynamics and has required methods for simulation.  Users can make any pure C# dynamic model class as long as it inherits IVehicleDynamics and is compiled in LGSVL Simulator run-time executable.  If creating a new SMI class, place in Assets -> Scripts -> Dynamics folder.  Future updates will create a dll of the class so it won't need to be compiled in the simulator executable.



## Simple Model Interface Setup

[![](images/smi-setup.png)](images/full_size_images/smi-setup.png)

1. Create or select an EGO vehicle prefab in the Project panel only, not in scene

1. Double click this EGO vehicle prefab.  This opens the prefab editor scene

1. Remove VehicleSMI.cs from the vehicle component list in the Inspector panel.  This is the component that you are replacing with your own

1. Add your new c# dynamics class component that inherits IVehicleDynamics

1. If you plan to use Unity's physics engine, be sure to look at how VehicleSMI caches references to the wheel colliders and wheel meshes.  This AxleInfo, in the C# example, enables you to apply force to the wheels and match the movement to the wheel models, e.g.,

   [![](images/smi-unity-physics-setup.png)](images/full_size_images/smi-unity-physics-setup.png)

1. Set any public references needed for your new dynamics model and save the prefab, Ctrl-S

1. Open Simulator -> Build... to open the LGSVL Simulator bundle creation window

1. Select CubeTown and Ego vehicle

1. Select Build and after it is completed, in the root of the LGSVL Simulator you will have a folder called AssetBundles where the new bundles will be created.

1. Open Loader.scene, run in editor and open the WebUI by pressing the Open Browser... button in the Game panel view

1. Under Vehicles, create or assign the new prefab path to the vehicle name you want.  Apply any sensors needed in the JSON configuation

1. Under Maps, create or assign the new CubeTown map.

1. Under Simulations add map and vehicle and run 



## Full Model Interface

Full Model Interface supports [FMI2.0 Functional Mock-up Interface](https://github.com/modelica/fmi-standard).  Since user FMU's can vary greatly, we have provided an exampleFMU.fmu for testing in Windows only.  It can be found in the source code in Assets -> Resources.  ExampleVehicleFMU.cs is provided to see how the Full Model Interface system works.  It is located in Assets -> Scripts -> Dynamics -> Examples. Users can create their own FMU class that inherits from IVehicleDynamics but will need to be compiled with the simulator.

[![](images/fmi-setup-0.png)](images/full_size_images/fmi-setup-0.png)

1. Create or select an EGO vehicle prefab in the Project panel only, not in scene
1. Double click this EGO vehicle prefab.  This opens the prefab editor scene
1. Remove VehicleSMI.cs from the vehicle component list in the Inspector panel.  This is the component that you are replacing
1. Add Component ExampleVehicleFMU.cs



**ExampleFMU.fmu requires Unity physics solver**

[![](images/fmi-setup-1.png)](images/full_size_images/fmi-setup-1.png)

1. Toggle button Non Unity Physics to Unity Physics
1. Set Axles size to 2
1. Setup Axles data.  You will be dragging gameobjects from the Hierarchy panel to the Inspector panel
1. Drag the correct wheel colliders and wheel meshes to public references from the prefab, see SMI setup
1. Enable Motor and Steering for the front axle
1. Set Brake Bias to 0.5f for front and back axles



**Import the ExampleFMU.fmu**

[![](images/fmi-setup-2.png)](images/full_size_images/fmi-setup-2.png)

1. Toggle button Import FMU
1. Choose ExampleFMU.fmu in Assets -> Resources -> ExampleFMU.fmu
1. FMU will unpack in the repository vehicle folder in External -> Vehicles -> VehicleName -> FMUName folder
1. FMUImporter.cs will parse the XML file into FMUData in the VehicleFMU class instance on prefab
1. Model Variables will be listed so users can reference by index
1. Each model variable has multiple values that are displayed in the scroll area
1. Open Simulator -> Build... to open the LGSVL Simulator bundle creation window
1. Select CubeTown if you have not created a bundle yet and Ego vehicle
1. Select Build and after it is completed, in the root of the LGSVL Simulator you will have a folder called AssetBundles where the new bundles will be created.
1. Open Loader.scene, run in editor and open the WebUI by pressing the Open Browser... button in the Game panel view
1. Under Vehicles, create or assign the new prefab path to the vehicle name you want.  Apply any sensors needed in the JSON configuation
1. Under Maps, create or assign the new CubeTown map if not setup yet
1. Under Simulations add map and vehicle and run



## Full Model Interface Run-time behavior

When LGSVL Simulator loads a vehicle bundle with an FMU included, it saves the fmu.dll to a folder in Unity's persistent data folder.  This folder is contained in the hidden folder AppData in Windows.  Here it is loaded by the FMU class, loaded and passed to ExampleVehicleFMU class.  Then ExampleVehicleFMU class can call FMU specific methods in the dll.  Config.cs will hold references all opened dll's so LGSVL Simulator will not try to open it multiple times.  Currently, LGSVL Simulator only supports one FMU in Windows only.



## Copyright and License

Copyright (c) 2020 LG Electronics, Inc.

This software contains code licensed as described in LICENSE.
