# EGO Vehicle Dynamics[](#top)

LGSVL Simulator supports multiple dynamics models for EGO vehicles.

By default, LGSVL Simulator default vehicles' vehicle dynamics are handled by the Unity Physics Engine, PhysX. For users who would like to change or replace vehicle dynamics with their own models, the simulator offers the following ways to do so.

- Simple Model Interface - pure C# dynamics

- Full Model Interface - [FMI 2.0](https://fmi-standard.org/) supported dynamics

  


## Simple Model Interface

Simple Model Interface is our pure C# dynamic model.  LGSVL Simulator vehicles that are provided, use the VehicleSMI class.  It is located in Assets -> Scripts -> Dynamics -> Examples.  This class inherits IVehicleDynamics and has required methods for simulation.  Users can make any pure C# dynamic model class as long as it inherits IVehicleDynamics and is compiled in LGSVL Simulator run-time executable.  If creating a new SMI class, place in Assets -> Scripts -> Dynamics folder.  Future updates will create a dll of the class so it won't need to compile in simulator executable.



## Simple Model Interface Setup

[![](images/smi-setup.png)](images/full_size_images/smi-setup.png)

1. Double click on EGO vehicle prefab
1. Remove VehicleDynamics.cs from the vehicle prefab if present
1. Add Component VehicleSMI.cs
1. Setup Axles data. Drag the correct wheel colliders and wheel meshes to public references from the prefab
1. Setup RPM Curve, Shift Up Curve and Shift Down Curve.  Copy from a LGSVL vehicle if needed.
1. Save and build asset bundle

[![](images/smi-unity-physics-setup.png)](images/full_size_images/smi-unity-physics-setup.png)



## Full Model Interface

Full Model Interface supports [FMI2.0 Functional Mock-up Interface](https://github.com/modelica/fmi-standard).  Since user FMU's can vary greatly, we have provided an exampleFMU.fmu for testing in Windows only.  It can be found in the source code in Assets -> Resources.  ExampleVehicleFMU.cs is provided to see how the Full Model Interface system works.  It is located in Assets -> Scripts -> Dynamics -> Examples. Users can create their own FMU class that inherits from IVehicleDynamics but will need to be compiled with the simulator.

[![](images/fmi-setup-0.png)](images/full_size_images/fmi-setup-0.png)

1. Double click on EGO vehicle prefab
1. Remove VehicleDynamics.cs from the vehicle prefab if present
1. Add Component ExampleVehicleFMU.cs



**ExampleFMU.fmu requires Unity physics solver**

[![](images/fmi-setup-1.png)](images/full_size_images/fmi-setup-1.png)

1. Toggle button Non Unity Physics to Unity Physics
1. Setup Axles data. Drag the correct wheel colliders and wheel meshes to public references from the prefab
1. Enable Motor and Steering for the front axle
1. Set Brake Bias to 0.5f for front and back axles



**Import the ExampleFMU.fmu**

[![](images/fmi-setup-2.png)](images/full_size_images/fmi-setup-2.png)

1. Toggle button Import FMU
1. Choose ExampleFMU.fmu
1. FMU will unpack in the vehicle folder in External -> Vehicles -> VehicleName -> FMUName folder
1. FMUImporter.cs will parse the XML file into FMUData in the VehicleFMU class
1. Model Variables will be listed so users can reference by index
1. Each model variable has multiple values that are displayed in the scroll area
1. Build asset bundle



## Full Model Interface Run-time behavior

When LGSVL Simulator loads a vehicle bundle with an FMU included, it saves the fmu.dll to a folder in Unity's persistent data folder.  This folder is contained in the hidden folder AppData in Windows.  Here it is loaded by the FMU class, loaded and passed to ExampleVehicleFMU class.  Then ExampleVehicleFMU class can call FMU specific methods in the dll.  Config.cs will hold references all opened dll's so LGSVL Simulator will not try to open it multiple times.  Currently, LGSVL Simulator only supports one FMU in Windows only.



## Copyright and License

Copyright (c) 2020 LG Electronics, Inc.

This software contains code licensed as described in LICENSE.
