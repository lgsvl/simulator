# Add New Ego Vehicle

This document will describe how to create a new ego vehicle in the LGSVL Simulator.

## Video

[![Add New Ego Vehicle](images/add-new-ego-vehicle.jpg)](https://www.youtube.com/watch?v=NgW1P75wiuA&)

([Link](https://www.youtube.com/watch?v=NgW1P75wiuA&))

***TODO: Fix the youtube links above, and create thumbnail image!***

## Getting Started

The following text is a list of the steps described in the above YouTube video.

***TODO: We could link these steps (or perhaps only the headings) to the corresponding place in the video...***

1. Launch Unity (***TODO: do they need to open something?***).
1. Create a new scene and add an existing ego vehicle prefab to the hierarchy. This makes it easy to copy components to the new vehicle.
1. Toggle the reference vehicle prefab to *inactive*.

## Create and Name Your Vehicle

1. Create a new empty root gameobject for your vehicle and give it a name.
1. Place vehicle meshes as a child of the root gameobject.
1. Assign root gameobject tag to "Player".
1. Right click on each component on the reference prefab root and copy it.
1. Then paste each copied component onto the new vehicle gameobject root. You'll need to go back to the reference prefab root to copy each additional object before pasting.
1. Note: We will fix the missing references later.

## Add Child Components

Add the following components as children:

* MainCollider
* WheelColliders
* Lights
* GroundTruthDetectBoundingBox
* DriverCamera
* DriverCameraPositions
* DashInteriorUICanvas
* SensorArray
* Note: These are prefabs that were created from an existing ego vehicle prefab.
11. For this tutorial the hierarchy and scripts were adjusted. You don't need to alter them to match; just use the reference vehicle.
12. Next, drag the root object into the project panel to create a prefab. This will serialize the gameobject as a prefab.

## Apply the Correct References

We need to apply the correct references to the vehicle scripts since the public variables are still referencing the other vehicle:

Note: Be sure to click the _Apply_ button in the Inspector after each component change. This saves the change to the prefab in the project. Check that no variable references are still bold after updating the references.

### Vehicle Controller script

For the Vehicle Controller script, reference the following colliders and meshes:

* Reference the FL WheelColliders in Axles Element 0 (Left)
* Reference the FR WheelColliders in Axles Element 0 (Right)
* Reference the RL WheelColliders in Axles Element 1 (Left)
* Reference the RR WheelColliders in Axles Element 1 (Right)
* Reference the FL\_PARENT WheelMeshes in Axles Element 0 (Left Visuals)
* Reference the FR\_PARENT WheelMeshes in Axles Element 0 (Right Visuals)
* Reference the RL\_PARENT WheelMeshes in Axles Element 1 (Left Visuals)
* Reference the RR\_PARENT WheelMeshes in Axles Element 1 (Right Visuals)
* Reference the MainCollider in Car Center

### Car HeadLights script

For the Car Headlights script, reference the following Lights:

* Reference the XE Left Headlight Spot
* Reference the XE Right Headlight Spot
* Reference the XE Left Tail Spot
* Reference the XE Right Tail Spot

### Car Input Controller script

For the Car Input Controller script, reference the DriverCamera.

### Force Feedback script

For the Force Feedback script, reference the FL and FR WheelColliders.

### Vehicle Animation Manager script

For the Vehicle Animation Manager script, reference the following meshes:

* Reference WiperLeft mesh object (under MeshHolder, DashMeshes, then WindshieldWipers) in WiperLeft (Animator)
* Reference WiperRight mesh object (under MeshHolder, DashMeshes, then WindshieldWipers) in WiperRight (Animator)

### Vehicle Position Resetter script

For the Vehicle Position Resetter script, reference the GpsSensor under SensorArray.

### Agent Setup script

For the Agent Setup script, update the following references:

* Reference the DriverCamera in Follow Camera.
* Reference the NewVehicle in Camera Man.

Note that the AgentSetup script has an extra step needed to reference bridge classes in each object of the Needs Bridge array.

To do this, you'll need to drag each class from a second inspector panel:

* Add a new Inspector tab next to the Console tab.
* Lock one panel and use the other to select the sensor object.
* Then drag the class into the NeedsBridge array.
* Do this for all sensors that require a bridge connection:
	* LidarSensor (from sensor inspector)
	* GpsSensor (from sensor inspector)
	* TelephotoCamera (from sensor inspector)
	* CaptureCamera (from sensor inspector)
	* ImuSensor (from sensor inspector)
	* RadarSensor (from sensor inspector)
	* VehicleInputController (from NewVehicle inspector)
	* CanBusSensor (from sensor inspector)
	* SegmentationCamera (from sensor inspector)
	* VehiclePositionResetter (from NewVehicle inspector)
	* UserInterfaceTweakables (from NewVehicle inspector)
* Unlock and close the extra inspector panel
* Now select NewVehicle, and click the *Apply* button to apply changes.

Next, update the child objects public references:

* For the Driver Camera, update the following camera position items:
	* DriverCameraPosition
	* ThirdPersonCameraPosition
	* ReverseViewCameraPosition
* For Cam Fix To, update Fix To with ThirdPersonCameraPosition
* For Cam Smooth Follow:
	* Update Target Position Transform with ThirdPersonCameraPosition
	* Update Target Object with NewVehicle
* Click the *Apply* button to apply changes

Next, update the SensorArray public references:

* For Can Bus script:
	* Update MainRigidBody with NewVehicle
	* Update Controller with NewVehicle
	* Update Input\_controller with NewVehicle
	* Update Gps with GpsSensor
	* Click the *Apply* button to apply changes
* For GpsSensor script:
	* Update Target with NewVehicle
	* Update Agent with NewVehicle
	* Update MainRigidBody with NewVehicle
	* Click the *Apply* button to apply changes
* For ImuSensor script:
	* Update Target with NewVehicle
	* Update MainRigidBody with NewVehicle
	* Click the *Apply* button to apply changes

## Final Steps

1. Set the vehicle and *all child objects* to the Duckiebot layer.
1. Next, apply changes, delete the reference ego vehicle, and save the scene.
1. Finally, select the ROSAgentManager prefab from the project and increase the size of the AgentPrefabs array by one.
	* Add the NewVehicle prefab to the Agent Prefabs array.
	* Be sure to add the prefab from the PROJECT panel, not the scene!
1. Press Play to launch the new scene.
1. Click the Vehicle popup to see the new vehicle in the vehicle list.

Congratulations! You have successfully added a new ego vehicle!
