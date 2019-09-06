# <a name="top"></a> Python API Guide

## Overview

LGSVL Simulator exposes runtime functionality to a Python API which you can use
to manipulate object placement and vehicle movement in a loaded scene, retreive
sensor configuration and data, control weather, time state, and more.

The interface to listen for incoming API calls is defined in `config.yml`. See
[Configuration File and Command Line Parameters](config-and-cmd-line-params.md)
for more information.

<h2> Table of Contents</h2>
[TOC]

## Requirements <sub><sup>[top](#top)</sup></sub> {: #requirements data-toc-label='Requirements'}

Using Python API requires Python version 3.5 or later.

## Quickstart <sub><sup>[top](#top)</sup></sub> {: #quickstart data-toc-label='Quickstart'}

Python API is available in separate repository: https://github.com/lgsvl/PythonAPI
After cloning or downloading it from the git repository follow these steps:

1. Run the following command to install Python files and necessary dependencies:

    ```
    pip3 install --user -e .
    ```

2. Now launch the simulator (either binary .exe file or from Unity Editor). Simulator
   by default listens for connections on port 8181 on localhost.

3. Click the `Open Browser` button to open the Simulator UI.

4. After the default maps and vehicles have been downloaded, navigate to the `Simulations` tab.

5. Create a new Simulation. Give it a name and check the `API Only` option. Click `Submit`.

6. Select the newly created Simulation and click the "Play" button in the bottom right.

3. Run the following example to see the API in action:

    ```
    ./quickstart/05-ego-drive-in-circle.py
    ```

    This will load the `BorregasAve.unity` scene, instantiate one EGO vehicle, then ask
    you to press `Enter` to start driving EGO vehicle in a circle.

When the script is running, it will apply throttle and steering commands to make the car move.

## Core concepts <sub><sup>[top](#top)</sup></sub> {: #core-concepts data-toc-label='Core Concepts'}

The Simulator and API communicate by sending json over a websocket server running on
8181 port. The API client can be either on the same machine or any other machine on
the same network.

API exposes the following main types:

 * **Simulator** - main object for connecting to simulator and creating other objects
 * **Agent** - superclass of vehicles and pedestrian
 * **EgoVehicle** - [EGO vehicle](#ego-vehicle) with accurate physics simulation and
   [sensors](#sensors)
 * **NpcVehicle** - [NPC vehicle](#npc-vehicles) with simplified physics, useful for
   creating many background vehicles
 * **Pedestrian** - [pedestrian](#pedestrians) walking on sidewalks

Vehicles and Pedestrian are a subclasses of Agent which has common properties like
transform, position, and velocity.

All coordinates in the API return values in the Unity coordinate system. This coordinate
system uses meters as a unit of distance and is a left-handed coordinate system - x points left,
z points forward, and y points up.

The Simulator class provides helper methods to convert coordinates to and from
latitude/longitude and northing/easting values.

## Simulation <sub><sup>[top](#top)</sup></sub> {: #simulation data-toc-label='Simulation'}

To connect to the simulator you need to an instance of the `Simulator` class:

```python
import lgsvl
sim = lgsvl.Simulator(address = "localhost", port = 8181)
```

You can specify a different address as hostname or IP address. By default only port 8181 is
used for API connection. Only one client can be connected to simulator at a time.

Next, load the scene ("map"). This is done by `load` method:

```python
sim.load(scene = "BorregasAve", seed = 650387)
```

Scene is a string representing the name of the `Map` in the Web UI. Currently available scenes:

 * **BorregasAve** - small suburban map
 * **AutonomouStuff** - small office park

Seed (optional) is an Integer (-2,147,483,648 - 2,147,483,647) that determines the "random" behavior of the NPC vehicles and rain effects.

Check the Web UI Maps tab for a full list of available scenes.

Once a scene is loaded you can instantiate agents and run simulations. See
the [Agents](#agents) section on how to create vehicles and pedestrians.

Loading scenes takes a while, to reset a scene to the initial state without reloading it call the `reset`
method:

```python
sim.reset()
```

This will remove any vehicles or callbacks currently registered.

After setting up the scene in a desired state you can start advancing time.
During python code execution time is stopped in the simulator. To run the simulator in realtime, call the `run` method:

```python
sim.run(time_limit = 5.0)
```

`run` accepts an optional argument for a time limit specifying how long to run. The default
value of 0 will run infinitely.

Diagram illustrating API execution:

![](images/python-api-execution.png)

### Non-realtime Simulation <sub><sup>[top](#top)</sup></sub> {: #non-realtime-simulation data-toc-label='Non-realtime Simulation'}
The simulator can be run at faster-than-realtime speeds depending on the performance of the computer running the simulator. 
This is done by calling the `run` method with the `time_scale` argument:

```python
sim.run(time_limit = 6, time_scale = 2)
```

`run` takes a 2nd optional argument specifying how much faster to run.
In the above example, if the computer is fast enough the run call will finish in 3 seconds (6 divided by 2),
but 6 virtual seconds of data would be generated. If only `time_scale` is specified or `time_limit` = 0,
then simulation will run continuously at non-realtime speed.

The value of time_scale can be lower than 1 which gives ability to run simulation in slower than real time.


## Agents <sub><sup>[top](#top)</sup></sub> {: #agents data-toc-label='Agents'}

You can create vehicles and pedestrians by calling the `add_agent` method of the `Simulator`
object. Example:

```python
ego = sim.add_agent(name = "Lincoln2017MKZ (Apollo 5.0)", agent_type = lgsvl.AgentType.EGO, state = None)
```

This will create the `Lincoln2017MKZ (Apollo 5.0)` vehicle from the Web UI Vehicles tab. Other AgentTypes
available are:

 * **AgentType.EGO** - EGO vehicle
 * **AgentType.NPC** - NPC vehicle
 * **AgentType.PEDESTRIAN** - pedestrian

Each agent type has predefined names you can use. Currently availble EGO vehicles:

 * **Jaguar2015XE (Apollo 3.0)** - Apollo 3.0 vehicle
 * **Jaguar2015XE (Apollo 5.0)** - Apollo 5.0 vehicle
 * **Jaguar2015XE (Autoware)** - Autoware vehicle
 * **Lexus2016RXHybrid (Autoware)** - Autoware vehicle
 * **Lincoln2017MKZ (Apollo 5.0)** - Apollo 5.0 vehicle

Available NPC vehicles:

 * **Sedan**
 * **SUV**
 * **Jeep**
 * **Hatchback**
 * **SchoolBus**
 * **BoxTruck**

Available pedestrian types:

 * **Bob**
 * **EntrepreneurFemale**
 * **Howard**
 * **Johny**
 * **Pamela**
 * **Presley**
 * **Red**
 * **Robin**
 * **Stephen**
 * **Zoe**

If an incorrect name is entered, a Python exception will be thrown.

Optionally you can create agents in specific positions and orientations in the scene.
For this you need to use the `AgentState` class. For example:

```python
state = lgsvl.AgentState()
state.transform.position = lgsvl.Vector(10, 0, 30)
state.transform.rotation.y = 90
ego = sim.add_agent("Lincoln2017MKZ (Apollo 5.0)", lgsvl.AgentType.EGO, state)
```

This will create a vehicle at position x=10, z=30 which is rotated 90 degrees around the
vertical axis. The position and rotation are set in the world coordinates space.

You can always adjust the position, rotation, velocity and angular velocity of the agent
at any later time:

```python
s = ego.state
s.velocity.x = -50
ego.state = s
```

This will set x component of velocity (in world coordinate space) to -50 meters
per second and leave y and z components of velocity unmodified.

All agents have the following common functionality:

 * `state` - property to get or set agent state (position, velocity, ...)
 * `transform` - property to get `transform` member of the state (shortcut for
     `state.transform`)
 * `bounding_box` - property to get bounding box in local coordinate space.
   Note that bounding box is not centered around (0,0,0) - it depends on the actual
   geometry of the agent.
 * `on_collision` - method to set a callback function to be called when the agent
   collides with something (other agent or static obstacle), see [callbacks](#callbacks)
   section for more information.

### EGO vehicle <sub><sup>[top](#top)</sup></sub> {: #ego-vehicle data-toc-label='EGO Vehicle'}

EGO vehicle has following additional functionality:

 * `apply_control` - method to apply specified throttle, break, steering or other
    actions to vehicle. Pass `sticky=True` to apply these values on every simulation
    update iteration.
 * `get_sensors` - method to return list of [sensors](#sensors)
 * `connect_bridge` - method to connect to ROS or Cyber RT bridge
 * `bridge_connected` - bool property, `True` if bridge is connected

You can control the movement of the EGO vehicle either by manually specifying state, applying
manual control, or connecting through the bridge.

Example to apply constant 20% throttle to EGO vehicle:

```python
ego = sim.add_agent("Lincoln2017MKZ (Apollo 5.0)", lgsvl.AgentType.EGO)
c = lgsvl.VehicleControl()
c.throttle = 0.2
ego.apply_control(c, True)
```

### NPC vehicles <sub><sup>[top](#top)</sup></sub> {: #npc-vehicles data-toc-label='NPC Vehicles'}

You can create multiple NPC vehicles on the map to drive along the lanes or follow specific
waypoints on the map.

NPC vehicle has the following additional functionality:

 * `change_lane` - method to make the vehicle change lanes
 * `follow` - method to make vehicle follow specific waypoints
 * `follow_closest_lane` - method to make vehicle follow lanes
 * `on_waypoint_reached` - method to set callback function which is called for every
    waypoint the vehicle reaches
 * `on_stop_line` - method to set callback function which is called when vehicle
    reaches a stop line at interesection
 * `on_lane_change` - method to set callback function which is called when vehicle
    decides to change lanes

You can control the movement of an NPC vehicle either by manually specifying state, or
instructing it to follow waypoints or lanes.

To make an NPC follow waypoints prepare a list of `DriveWaypoint` objects and call
the `follow` method for the npc vehicle:

```python
npc = sim.add_agent("Sedan", lgsvl.AgentType.NPC)
waypoints = [
  lgsvl.DriveWaypoint(lgsvl.Vector(1,0,3), 5, lgsvl.Vector(0, 0, 0), 0, 0),
  lgsvl.DriveWaypoint(lgsvl.Vector(5,0,3), 10, lgsvl.Vector(0, 0, 0), 0, 0),
  lgsvl.DriveWaypoint(lgsvl.Vector(1,0,5), 5, lgsvl.Vector(0, 0, 0), 0, 0),
]
npc.follow(waypoints, loop=True)
```

Each waypoint has a position in world coordinates, a desired velocity in m/s, a desired angular orientation as a vector of Euler angles, an optional wait-time for the vehicle to stay idle, and an optional trigger distance. The NPC
will ignore all traffic rules and will not avoid collisions to try to get to the next
waypoint. The angular orientation of the NPC will be interpolated in such a manner that it will pass through the waypoint at the angle specified in the `DriveWaypoint`. The trigger distance, if used, provides a method to pause the NPC until an ego vehicle approaches. The NPC will begin to drive as soon as its distance to an ego vehicle drops below the value specified as trigger distance in the `DriveWaypoint`.
You can receive information on progress by setting the `on_waypoint_reached`
callback. Example (see [callbacks](#callbacks) for more details):

```python
npc = sim.add_agent("Sedan", lgsvl.AgentType.NPC)

def on_waypoint(agent, index):
  print("waypoint {} reached".format(index))

npc.follow(waypoints, loop=True)
npc.on_waypoint_reached(on_waypoint)

sim.run()
```

`follow_closest_lane` will make the NPC vehicle follow whatever lane is the closest.
Upon reaching intersections it will randomly decide to either drive straight or turn.

### Pedestrians <sub><sup>[top](#top)</sup></sub> {: #pedestrians data-toc-label='Pedestrians'}

You can create `Pedestrian` agents that will allow you to create pedestrians on sidewalks
and make them walk.

Pedestrians have the following additional functionality:

 * `walk_randomly` - method to make pedestrian walk randomly on the sidewalk
 * `follow` - method to make pedestrian follow specific waypoints
 * `on_waypoint_reached` - method to set callback function which is called for every
    waypoint reached

You can control the movement of pedestrians either by manually specifying state, or
instructing them to follow waypoints or walk randomly.

To make pedestrians follow waypoints prepare a list of `WalkWaypoint` objects and call
the `follow` method for pedestrians:

```python
npc = sim.add_agent("Bob", lgsvl.AgentType.PEDESTRIAN)
waypoints = [
  lgsvl.WalkWaypoint(lgsvl.Vector(1,0,3), 5),
  lgsvl.WalkWaypoint(lgsvl.Vector(5,0,3), 10),
  lgsvl.WalkWaypoint(lgsvl.Vector(1,0,5), 5),
]
npc.follow(waypoints, loop=True)
```

Each waypoint has a position in world coordinates and an idle time that the pedestrian will
spend standing in-place when it reaches the waypoint. You can receive information on progress by
setting the `on_waypoint_reached` callback.

## Callbacks <sub><sup>[top](#top)</sup></sub> {: #callbacks data-toc-label='Callbacks'}

The Python API can invoke callbacks to inform you of specific events that occur during
simulator runtime. Callbacks are invoked from inside the `Simulator.run` method and
while a callback is running the simulation time is paused. Once the callback finishes time is resumed and the simulation resumes execution. You can call `Simulator.stop`
to stop further execution and return immediately from the callback.

The internals of this process are illustrated in the following sequence diagram:

![](images/python-api-callbacks.png)

Here the code resumes simulation after the first callback, but stops execution when the second
callback is handled.

You set callback functions by calling `on_NAME` method of object, see information below.

### [Agent](#agents) Callbacks <sub><sup>[top](#top)</sup></sub> {: #agent-callbacks data-toc-label='Agent Callbacks'}

`collision` - called when agent collides with something (other agent or stationary
obstacle).

Example usage:

```python
def on_collision(agent1, agent2, contact):
  name1 = "STATIC OBSTACLE" if agent1 is None else agent1.name
  name2 = "STATIC OBSTACLE" if agent2 is None else agent2.name
  print("{} collided with {} at {}".format(name1, name2, contact))

ego.on_collision(on_collision)
```

Callback receives three arguments: `(agent1, agent2, contact)` - the first two are the agents that collide,
one of them can be None if it is a stationary obstacle like a building or a traffic light pole,
and the third is the world position of the contact point.

### [NpcVehicle](#npc-vehicles) Callbacks <sub><sup>[top](#top)</sup></sub> {: #npcvehicle-callbacks data-toc-label='NpcVehicle Callbacks'}

In addition to Agent callbacks, NpcVehicle has three extra callbacks:

`waypoint_reached` - called when vehicle reaches a waypoint; accepts two
arguments: `(agent, index)` - agent instance and waypoint index as integer

`stop_line` - called when vehicle stops at a stop line for a traffic light or stop sign; accepts one argument: `(agent)` - agent instance

`lane_change` - called when vehicle starts changing lane; accepts one
argument: `(agent)` - agent instance

### [Pedestrian](#pedestrians) Callbacks <sub><sup>[top](#top)</sup></sub> {: #pedestrian-callbacks data-toc-label='Pedestrian Callbacks'}

In addition to Agent callbacks, Pedestrian has one extra callback.

`waypoint_reached` - called when pedestrian reaches waypoint; accepts two
arguments: `(agent, index)` - agent instance and waypoint index as integer.

## Sensors <sub><sup>[top](#top)</sup></sub> {: #sensors data-toc-label='Sensors'}

[EGO vehicles](#ego-vehicle) have sensors attached. You can get a list of them by
calling `EgoVehicle.get_sensors()` which returns a Python list with instances of the following
classes:

 * **CameraSensor** - see [Camera](#camera-sensor) sensor
 * **LidarSensor** - see [Lidar](#lidar-sensor) sensor
 * **ImuSensor** - see [IMU](#imu-sensor) sensor
 * **GpsSensor** - see [GPS](#gps-sensor) sensor
 * **RadarSensor** - see [Radar](#radar-sensor) sensor
 * **CanBusSensor** - see [CAN bus](#can-bus)

Each sensor has the following common members:

 * `name` - name of sensor, to diffrentiate sensors of the same type, for example, to choose one out of multiple cameras attached to EgoVehicle
 * `transform` - property that contains position and rotation of a sensor relative to the agent transform 
 * `enabled` - bool property, set to `True` if sensor is enabled for capturing and sending data to
   ROS or Cyber bridge

### Camera Sensor <sub><sup>[top](#top)</sup></sub> {: #camera-sensor data-toc-label='Camera Sensor'}

The Camera sensor has the following read only properties:

 * `frequency` - rate at which images are captured & sent to ROS or Cyber bridge
 * `width` - image width
 * `height` - image height
 * `fov` - vertical field of view in degrees
 * `near_plane` - distance of near plane
 * `far_plane` - distance of far plane
 * `format` - format of image ("RGB" for 24-bit color image, "DEPTH" - 8-bit grayscale depth buffer,
   "SEMANTIC" - 24-bit color image with sematic segmentation)

Camera image can be saved to disk by calling `save`:

```python
ego = sim.add_agent("Lincoln2017MKZ (Apollo 5.0)", lgsvl.AgentType.EGO)

for sensor in ego.get_sensors():
  if sensor.name = "Main Camera":
    sensor.save("main-camera.png", compression=0)
```

`save` method accepts a path relative to the running simulator, and an optional `compression` for png files (0...9)
or `quality` (0..100) for jpeg files.

### Lidar Sensor <sub><sup>[top](#top)</sup></sub> {: #lidar-sensor data-toc-label='Lidar Sensor'}

Lidar sensor has following read only properties:

  * `min_distance` - minimal distance for capturing points
  * `max_distance` - maximum distance for capturing points
  * `rays` - how many laser rays (vertically) to use
  * `rotations` - frequency of rotation, typically 10Hz
  * `measurements` - how many measurmenets per rotation each ray is taking
  * `fov` - vertical field of view (bottom to top ray) in degrees
  * `angle` - angle lidar is tilted (middle of fov view)
  * `compensated` - bool, whether lidar point cloud is compensated

Lidar point cloud can be saved to disk as a .pcd file by calling `save`:

```python
ego = sim.add_agent("Lincoln2017MKZ (Apollo 5.0)", lgsvl.AgentType.EGO)

for sensor in ego.get_sensors():
  if sensor.name = "Lidar":
    sensor.save("lidar.pcd")
```

A `.pcd` file is in the [binary Point Cloud Data format](http://pointclouds.org/documentation/tutorials/pcd_file_format.php) where each point has x/y/z coordinates as 4-byte floats and a 1-byte unsigned int as intensity (0...255).

### IMU Sensor <sub><sup>[top](#top)</sup></sub> {: #imu-sensor data-toc-label='IMU Sensor'}

You can use the IMU sensor to get its position in the vehicle. All measurements an IMU would provide can be obtained by using the `transform` property of the agent.

### GPS Sensor <sub><sup>[top](#top)</sup></sub> {: #gps-sensor data-toc-label='GPS Sensor'}

You can retrieve the current GPS location from the GPS sensor by calling `data`:

```python
data = gps_sensor.data()
print("Latitude:", data.latitude)
```

Returned data will contain following fields:

 * `latitude`
 * `longitude`
 * `northing`
 * `easting`
 * `altitude`
 * `orientation` - rotation around up-axis in degrees

### Radar Sensor <sub><sup>[top](#top)</sup></sub> {: #radar-sensor data-toc-label='Radar Sensor'}

Currently the Radar sensor can be used only to get its position and rotation in the vehicle. Radar measurements can be received in ROS or Cyber by setting the `enabled` property of the sensor.

### CAN bus <sub><sup>[top](#top)</sup></sub> {: #can-bus data-toc-label='CAN bus'}

Currently CAN bus can be used only to get its position and rotation in the vehicle. CAN bus messages can be received in ROS or Cyber by setting the `enabled` property of the sensor.

## Weather and Time of Day Control <sub><sup>[top](#top)</sup></sub> {: #weather-and-time-of-day-control data-toc-label='Weather and Time of Day Control'}

You can control the weather properties of the simulation by reading or writing to the `weather` property. You can
set `rain`, `fog` or `wetness` (float 0...1). Example:

```python
w = sim.weather
w.rain = 0.5     # set rain to 50%
sim.weather = w
```

Changing time of day allows to control whether the loaded scene appears as day or night.
To get the current time read the `time_of_day` property:

```python
print("Current time of day:", sim.time_of_day)
```

It will return a float between 0 and 24. To set time of day call `set_time_of_day`:
```python
sim.set_time_of_day(10, fixed=True)
```

This will set current time of day to 10am. The optional bool argument `fixed` indicates whether the simulation
should advance this time automatically or freeze it and not change it (`fixed=True`).

## Controllable Objects <sub><sup>[top](#top)</sup></sub> {: #controllable-objects data-toc-label='Controllable Objects'}

A controllable object is an object that you can control by performing an action using Python APIs. Each controllable object has its own `valid actions` (e.g., green, yellow, red, trigger, wait, loop) that it can take and is controlled based on `control policy`, which defines rules for control actions.

For example, a traffic light is a controllable object, and you can change its behavior by updating control policy: `"trigger=50;green=1;yellow=1.5;red=2;loop"`

 * `trigger=50` - Wait until an ego vehicle approaches this controllable object within 50 meters
 * `green=1` - Change current state to `green` and wait for 1 second
 * `yellow=1.5` - Change current state to `yellow` and wait for 1.5 second
 * `red=2` - Change current state to `red` and wait for 2 second
 * `loop` - Loop over this control policy from the beginning

Available controllable object types:

 * **signal**

To get a list of controllable objects in a scene:

```python
controllables = sim.get_controllables()
```

For a controllable object of interest, you can get following information:

```python
signal = controllables[0]
print("Type:", signal.type)
print("Transform:", signal.transform)
print("Current state:", signal.current_state)
print("Valid actions:", signal.valid_actions)
```

For control policy, each controllable object always has default control policy (read-only). When you load a scene for the first time or reset a scene to the initial state, a controllable object resets current control policy to default one follows it.

You can get default control policy and current control policy as follows:
```python
print("Default control policy:", signal.default_control_policy)
print("Current control policy:", signal.control_policy)
```

To change a current control policy, you can create a new control policy and call `control` function as below:

```python
control_policy = "trigger=50;green=1;yellow=1.5;red=2;loop"
signal.control(control_policy)
```

## Helper Functions <sub><sup>[top](#top)</sup></sub> {: #helper-functions data-toc-label='Helper Functions'}

Simulator class offers following helper functions:

 * `version` - property that returns current version of simulator as string
 * `current_scene` - property that returns currently loaded scene as string, None if none is loaded
 * `current_frame` - property that returns currently simulated frame number as integer
 * `current_time` - property that returns currentl simulation time in seconds as float
 * `get_spawn` - method that returns list of transforms representing good positions where to place
   vehicles in the map. This list can be empty, it depends on how the map is prepared in Unity. Returned
   transforms contain `position` and `rotation` members as a `Vector`
 * `get_agents` - method that returns a list of currently available agent objets added with `add_agent`

To map points in Unity coordinates to GPS coordinates the Simulator class offers the following two functions:

  * `map_to_gps` - maps transform (position & rotation) to GPS location, returns same type as
    [GPS Sensor](#gps-sensor) `data` method
  * `map_from_gps` - maps GPS location (latitude/longitude or northing/easting) to transform
  * `raycast` - shoots a ray from specific location and returns closest object it hits

`map_from_gps` accepts two different inputs - latitude/longitude or northing/easting. Examples:

```python
tr1 = sim.map_from_gps(latitude=10, longitude=-30)
tr2 = sim.map_from_gps(northing=123455, easting=552341)
```

Optionally you can pass altitude and orientation.

`raycast` method can be used in following way:

```python
origin = lgsvl.Vector(10, 0, 20)
direction = lgsvl.Vector(1, 0, 0)
hit = sim.raycast(origin, direction, layer_mask=1)
if hit:
  print("Distance right:", hit.distance)
```
This will shoot a ray in the positive x-axis direction from the (10,0,20) coordinates.
A `RaycastHit` object with `distance`, `point` and `normal` fields is returned if something is hit, otherwise `None` is returned.

When raycasting you should specify a `layer_mask` argument that specifies which objects to check
collision with. It corressponds to layers in the Unity project - check the project for actual values.

## Changelog <sub><sup>[top](#top)</sup></sub> {: #changelog data-toc-label='Changelog'}

* 2019-08-12
	* Added `time_scale` argument to run function for running simulation in non-realtime
	* Added `seed` argument to `Simulator.load` for deterministic NPCs
* 2019-04-19
	* initial release


## Copyright and License <sub><sup>[top](#top)</sup></sub> {: #copyright-and-license data-toc-label='Copyright and License'}

Copyright (c) 2019 LG Electronics, Inc.

This software contains code licensed as described in LICENSE.

