# Python API Quickstart Script Descriptions

This document describes the example Python scripts that use the LGSVL Simulator Python API. These scripts are located [here](https://github.com/lgsvl/PythonAPI/tree/master/Api/quickstart). You can find the documentation on the API [here](https://www.lgsvlsimulator.com/docs/python-api/).



* [01-connecting-to-simulator.py](https://github.com/lgsvl/PythonAPI/blob/master/quickstart/01-connecting-to-simulator.py): How to connect to an already running instance of the simulator and some information you can get about the instance
* [02-loading-scene-show-spawns.py](https://github.com/lgsvl/PythonAPI/blob/master/quickstart/02-loading-scene-show-spawns.py): How to load a scene and get the scene's predefined spawn transforms
* [03-raycast.py](https://github.com/lgsvl/PythonAPI/blob/master/quickstart/03-raycast.py): How to create an EGO vehicle and do raycasting from a point
* [04-ego-drive-straight.py](https://github.com/lgsvl/PythonAPI/blob/master/quickstart/04-ego-drive-straight.py): How to create an agent with a velocity and then run the simulator for a set amount of time
* [05-ego-drive-in-circle.py](https://github.com/lgsvl/PythonAPI/blob/master/quickstart/05-ego-drive-in-circle.py): How to apply control to an EGO vehicle and then run the simulator indefinitely
* [06-save-camera-image.py](https://github.com/lgsvl/PythonAPI/tree/master/Api/quickstart/06-save-camera-image.py): How to save a camera image in different formats and with various settings
* [07-save-lidar-point-cloud.py](https://github.com/lgsvl/PythonAPI/tree/master/Api/quickstart/07-save-lidar-point-cloud.py): How to save a LIDAR point cloud
* [08-create-npc.py](https://github.com/lgsvl/PythonAPI/tree/master/Api/quickstart/08-create-npc.py): How to create several types of NPC vehicles and spawn them in different positions
* [09-reset-scene.py](https://github.com/lgsvl/PythonAPI/tree/master/Api/quickstart/09-reset-scene.py): How to empty the scene of all EGOs, NPCs, and Pedestrians, but keep the scene loaded
* [10-npc-follow-the-lane.py](https://github.com/lgsvl/PythonAPI/tree/master/Api/quickstart/10-npc-follow-the-lane.py): How to create NPCs and then let them drive in the nearest annotated lane
* [11-collision-callbacks.py](https://github.com/lgsvl/PythonAPI/tree/master/Api/quickstart/11-collision-callbacks.py): How to setup the simulator so that whenever the 3 created agents collide with anything, the name of the agent and the collision point is printed
* [12-create-npc-on-lane.py](https://github.com/lgsvl/PythonAPI/tree/master/Api/quickstart/12-create-npc-on-lane.py): How to create NPC vehicles in random position in a radius around the EGO vehicle, but the NPCs are placed on the nearest lane to the initial random position
* [13-npc-follow-waypoints.py](https://github.com/lgsvl/PythonAPI/tree/master/Api/quickstart/13-npc-follow-waypoints.py): How to create a list of waypoints with fixed wait times and direct an NPC to follow them
* [14-create-pedestrians.py](https://github.com/lgsvl/PythonAPI/tree/master/Api/quickstart/14-create-pedestrians.py): How to create pedestrians in rows in front of the spawn position
* [15-pedestrian-walk-randomly.py](https://github.com/lgsvl/PythonAPI/tree/master/Api/quickstart/15-pedestrian-walk-randomly.py): How to start and stop a pedestrian walking randomly on the sidewalk
* [16-pedestrian-follow-waypoints.py](https://github.com/lgsvl/PythonAPI/tree/master/Api/quickstart/16-pedestrian-follow-waypoints.py): How to create a list of waypoints and direct a pedestrian to follow them
* [17-many-pedestrians-walking.py](https://github.com/lgsvl/PythonAPI/tree/master/Api/quickstart/17-many-pedestrians-walking.py): How to generate an army of pedestrians and have them walk back and forth
* [18-weather-effects.py](https://github.com/lgsvl/PythonAPI/tree/master/Api/quickstart/18-weather-effects.py): How to get the current weather state of the simulator and how to adjust the various settings
* [19-time-of-day.py](https://github.com/lgsvl/PythonAPI/tree/master/Api/quickstart/19-time-of-day.py): How to get the time of date in the simulator and how to set it to a fixed time and a moving time
* [20-enable-sensors.py](https://github.com/lgsvl/PythonAPI/tree/master/Api/quickstart/20-enable-sensors.py): How to enable a specific sensor so that it can send data over a bridge
* [21-map-coordinates.py](https://github.com/lgsvl/PythonAPI/tree/master/Api/quickstart/21-map-coordinates.py): How to convert from simulator coordinates to GPS coordinates and back. Latitude/Longitude and Northing/Easting are supported along with altitude and orientation
* [22-connecting-bridge.py](https://github.com/lgsvl/PythonAPI/tree/master/Api/quickstart/22-connecting-bridge.py): How to command an EGO vehicle to connect to a bridge at a specific IP address and port and then wait for the connection to be established
* [23-npc-callbacks.py](https://github.com/lgsvl/PythonAPI/tree/master/Api/quickstart/23-npc-callbacks.py): How to setup the simulator so that whenever an NPC reaches a stopline or changes lane, the name of the npc is printed
* [24-ego-drive-straight-non-realtime.py](https://github.com/lgsvl/PythonAPI/tree/master/Api/24-ego-drive-straight-non-realtime.py): How to run the simulator at non-realtime.
* [25-waypoint-flying-npc.py](https://github.com/lgsvl/PythonAPI/tree/master/Api/25-waypoint-flying-npc.py): How to use waypoints to define customized motion for npc.
* [26-npc-trigger-waypoints.py](https://github.com/lgsvl/PythonAPI/tree/master/Api/26-npc-trigger-waypoints.py): How to use trigger waypoints that pause npc motion until an ego vehicle approaches.
* [99-utils-examples.py](https://github.com/lgsvl/PythonAPI/tree/master/Api/quickstart/99-utils-examples.py): How to use several of the utility scripts to transform an arbitrary point to the coordinate system of a local transform (relative to sensor)
