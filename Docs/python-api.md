# Python API

## Overview

LGSVL Simulator exposes runtime functionality to Python API which you can use
to manipulate object placement and vehicle movement in loaded scene, retreive
sensor configuration and data, control weather and time state, and more.

## Requirements

Using Python API requires at least Python 3.5 version.

## Quickstart

After unpacking LGSVL simulator zip file or cloning source from git repository
you should see `Api` folder in the root. Python API interface is fully contained
in this folder.

1. Go inside this folder and run follwing command to install Python files and
   necessary dependencies:

    ```
    pip3 install --user -e .
    ```

2. Now launch simulator (either binary .exe file or from Unity Editor) and
   leave it running in main menu. Simulator automatically listens for connections
   on port 8181.

3. Run following example to see API in action:

    ```
    ./quickstart/05-ego-drive-in-circle.py
    ```

    This will load the SanFrancisco scene, instantiate one EGO vehicle, then ask
    you to press `Enter` to start driving EGO vehicle in circle.

When script is running, it will apply throttle and steering to make car move

## Core concepts

Simulator and API communicates by sending json over websocket server running on
8181 port. The API client can be either on same machine or any other machine on
same network.

API exposes following main types:

 * **Simulator** - main object for connecting to simulator and creating other objects
 * **Agent** - superclass of vehicles and pedestrian
 * **EgoVehicle** - [EGO vehicle](#ego-vehicle) with accurate physics simulation and
   [sensors](#sensors)
 * **NpcVehicle** - [NPC vehicle](#npc-vehicles) with simplified physics, useful for
   creating many background vehicles
 * **Pedestrian** - [pedestrian](#pedestrians) walking on sidewalk

Vehicles and Pedestrian are subclasses of Agent which has common properties like
transform, position, velocity.

All coordinates in API returns values in Unity coordinate system. This coordinate
system uses meter as unit value and is left-handed coordinate system - x points left,
z points forward, y poins up.

Simulator class provides helper methods to convert coordinates to and from
latitude/longitude and northing/easting values.

## Simulation

To connect to simulator you need to start by instantiating `Simulator` class:

```python
import lgsvl
sim = lgsvl.Simulator("localhost", 8181)
```

You can specify different address as hostname or IP address. Simulator by default
uses only port 8181 for API connection.

Next thing to do is to load scene ("map"). This is done by `load` method:

```python
sim.load("SanFrancisco")
```

Map name is string that is name of scene file in Unity. Currently available scenes:

 * **SanFrancisco** - large city map
 * **SimpleMap** - small city map
 * **SimpleRoom** - for Tugbot robot
 * **SimpleLoop** - for Duckiebot robot
 * **Duckietown** - for Duckiebot robot
 * **DuckieDowntown** - for Duckiebot robot

Check the Unity project for full list of available scenes.

Once scene is loaded you can instantiate agents and run simulation. See
[Agents](#agents) section on how to create vehicles and pedestrians.

Loading scene takes a while, to reset scene to initial state call the `reset`
method:

```python
sim.reset()
```

This will remove any vehicles or callbacks currently registered.

After you have have set up scene in state you want you can start advancing time.
Any time the python code executes, the time is stopped in simulator. Only way to
advance time in simulator is to call `run` method:

```python
sim.run(time_limit = 5.0)
```

`run` method accepts optional argument for time limit how long to run. By default
limit value is 0, which means run infinitely.

Diagram illustrating API execution:

![](images/python-api-execution.png)

## Agents

You can create vehicles and pedestrians by calling `add_agent` method of simulator
object. Example:

```python
ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO)
```

This will create EGO vehicle from `XE_Rigged-apollo` template. Other AgentTypes
available are:

 * **AgentType.EGO** - EGO vehicle
 * **AgentType.NPC** - NPC vehicle
 * **AgentType.PEDESTRIAN** - pedestrian

Each agent type has predefined names you can use. Currently availble EGO vehicles:

 * **XE_Rigged-apollo** - Apollo 3.0 vehicle
 * **XE_Rigged-apollo_3_5** - Apollo 3.5 vehicle
 * **XE_Rigged-autoware** - Autoware vehicle
 * **Tugbot** - Tugbot warehouse robot
 * **duckiebot-duckietown-ros1** - Duckiebot robot for ROS1
 * **duckiebot-duckietown-ros2** - Duckiebot robot for ROS2

Available NPC vehicles:

 * **Sedan**
 * **SUV**
 * **Jeep**
 * **HatchBack**
 * **SchoolBus**
 * **DeliveryTruck**

Available pedestrian types:

 * **Bob**
 * **Entrepreneur**
 * **Howard**
 * **Johnny**
 * **Pamela**
 * **Presley**
 * **Robin**
 * **Stephen**
 * **Zoe**

In case name is wrong, the Python exception will be thrown.

Optionally you can create agent in specific position and orientation in the scene.
For this you need to use `AgentState` class. For example:

```python
state = lgsvl.AgentState()
state.transform.position = lgsvl.Vector(10, 0, 30)
state.transform.rotation.y = 90
ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, state)
```

This will create vehicle at position x=10, z=30 and rotated 90 degrees around
vertical axis. The position and rotation are set in world coordinate space.

You can always adjust position, rotation, velocity and angular velocity of agent
at any time later:

```python
s = ego.state
s.velocity.x = -50
ego.state = s
```

This will set x component of velocity (in world coordinate space) to -50 meters
per second and leave y and z components of velocity unmodified.

All agents have following common functionality:

 * `state` - property to get or set agent state (position, velocity, ...)
 * `transform` - property to get `transform` member of state (shortcut for
     `state.transform`)
 * `bounding_box` - property to get bounding box in local coordinate space,
   note that bounding box is not centered around (0,0,0) - it depends on actual
   geometry of agent.
 * `on_collision` - method to set callback function which is called when agent
   collides something (other agent or static obstacle), see [callbacks](#callbacks)
   section for more information on this.

## EGO vehicle

EGO vehicle has following additional functionality:

 * `apply_control` - method to apply specified throttle, break, steering or other
    actions to vehicle. Pass `sticky=True` to apply these values on every simulation
    update iteration.
 * `get_sensors` - method to return list of [sensors](#sensors)
 * `connect_bridge` - method to connect to ROS or Cyber RT bridge
 * `bridge_connected` - bool property, `True` is bridge is connected

You can control movement of EGO vehicle either by manually specifying state, applying
manual control, or connecting it to bridge.

Example to apply constant 20% throttle to EGO vehicle:

```python
ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO)
c = lgsvl.VehicleControl()
c.throttle = 0.2
ego.apply_control(c, True)
```

## NPC vehicles

You can create many NPC vehicles on map to drive alogn the lanes or follow specific
waypoints on them map.

NPC vehicle has following additional functionality:

 * `follow` - method to make vehicle follow specific waypoints
 * `follow_closest_lane` - method to make vehicle follow lanes
 * `on_waypoint_reached` - method to set callback function which is called for every
    waypoint the vehicle reaches
 * `on_stop_line` - method to set callback function which is called when vehicle
    reaches stop line at interesection
 * `on_lane_change` - method to set callback function which is called when vehicle
    decides to change lane

You can control movement of NPC vehicle either by manually specifying state, or
instructing it to follow waypoints or lanes.

To make NPC follow waypoints prepare list of `DriveWaypoint` objects and call
`follow` method for npc vehicle:

```python
npc = sim.add_agent("Sedan", lgsvl.AgentType.NPC)
waypoints = [
  lgsvl.DriveWaypoint(lgsvl.Vector(1,0,3), 5),
  lgsvl.DriveWaypoint(lgsvl.Vector(5,0,3), 10),
  lgsvl.DriveWaypoint(lgsvl.Vector(1,0,5), 5),
]
npc.follow(waypoints, loop=True)
```

Each waypoint has position in world coordinates and desired velocity in m/s. NPC
will ignore all traffic rules and will not avoid collisions to try to get to next
waypoint. You can receive information on progress by setting `on_waypoint_reached`
callback. Example (see [callbacks](#callbacks) for more details):

```python
npc = sim.add_agent("Sedan", lgsvl.AgentType.NPC)

def on_waypoint(agent, index):
  print("waypoint {} reached".format(index))

npc.follow(waypoints, loop=True)
npc.on_waypoint_reached(on_waypoint)

sim.run()
```

`follow_closest_lane` will make NPC vehicle to follow whatever lane is the closest.
Upon reaching intersection it will randomly decide to drive straight or turn.

## Pedestrians

You cna create `Pedestrian` agents that will allow you to create pedestrians on sidewalk
and make them walk.

Pedestrians have following additional functionality:

 * `walk_randomly` - method to make pedestrian walk randomly on sidewalk
 * `follow` - method to make pedestrian follow specific waypoints
 * `on_waypoint_reached` - method to set callback function which is called for every
    waypoint pedestrian reaches

You can control movement of pedestrian either by manually specifying state, or
instructing it to follow waypoints or walk randomly.

To make pedestrian follow waypoints prepare list of `WalkWaypoint` objects and call
`follow` method for pedestrian:

```python
npc = sim.add_agent("Bob", lgsvl.AgentType.PEDESTRIAN)
waypoints = [
  lgsvl.WalkWaypoint(lgsvl.Vector(1,0,3), 5),
  lgsvl.WalkWaypoint(lgsvl.Vector(5,0,3), 10),
  lgsvl.WalkWaypoint(lgsvl.Vector(1,0,5), 5),
]
npc.follow(waypoints, loop=True)
```

Each waypoint has position in world coordinates and idle time that pedestrian will
spend standing when it reaches waypoint. You can receive information on progress by
setting `on_waypoint_reached` callback.

## Callbacks

Python API can invoke callbacks to inform your code on some events that happen during
runtime of simulator. Callbacks are invoked from inside of `Simulator.run` method and
wile the callback is running the simulation time is stopped. Once callback finishes
the time is resumed and simulation resumes. You cam call `Simulator.stop` method to
stop further execution and return immediately from callback.

Diagram illustrating how callbacks works:

![](images/python-api-callbacks.png)

Here the code resumes simulation after first callback, but stops execution when second
callback happens.

You set callback function by calling `on_NAME` method of object, see examples below.

### [Agent](#agents) callbacks

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

Callback receives three arguments: `(agent1, agent2, contact)` - two agents that collides,
one of them can be None if it is stationary obstacle like a building or traffic light pole,
and world position of contact point.

### [NpcVehicle](#npc-vehicle) callbacks

Additionally to Agent callbacks NpcVehicle has three extra ones.

`waypoint_reached` - called when vehicle reaches waypoint, callback function accepts two
arguments: `(agent, index)` - agent instance and waypoint index as integer

`stop_line` - called when vehicle stops at stop line for traffic light or stop line, callback
function accepts one argument: `(agent)` - agent instance

`lane_change` - called when vehicle starts changing lanes, callback function accepts one
argument: `(agent)` - agent instance

### [Pedestrian](#pedestrian) callbacks

Additionally to Agent callbacks Pedestrian has one extra callback.

`waypoint_reached` - called when pedestrian reaches waypoint, callback function accepts two
arguments: `(agent, index)` - agent instance and waypoint index as integer.

## Sensors

[EGO vehicles](#ego-vehicle) have sensors attached to the vehicle. You can get list of them by
calling `EgoVehicle.get_sensors()` method. Return is a Python list with instances of following
classes:

 * **CameraSensor** - see [Camera](#camera-sensor) sensor
 * **LidarSensor** - see [Lidar](#lidar-sensor) sensor
 * **ImuSensor** - see [IMU](#imu-sensor) sensor
 * **GpsSensor** - see [GPS](#gps-sensor) sensor
 * **RadarSensor** - see [Radar](#radar-sensor) sensor
 * **CanBusSensor** - see [CAN bus](#can-bus)

Each sensor has following common members:

 * `name` - name of sensor, you can use this, for example, to choose one specific camera from
   multiple cameras
 * `transform` - property that contains position and rotation of sensor relative to agent transform 
 * `enabled` - bool property, set to `True` if sensor is enabled for capturing and sending data to
   ROS or Cyber bridge

### Camera Sensor

Camera sensor has following read only properties:

 * `frequency` - rate the image is captured & sent to ROS or Cyber bridge
 * `width` - image width
 * `height` - image height
 * `fov` - vertical field of view in degrees
 * `near_plane` - distance of near plane
 * `far_plane` - distance of far plane
 * `format` - format of image ("RGB" for 24-bit color image, "DEPTH" - 8-bit grayscale depth buffer,
   "SEMANTIC" - 24-bit color image with sematic segmentation)

Camera image can be saved to disk by calling `save` method:

```python
ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO)

for sensor in ego.get_sensors():
  if sensor.name = "Main Camera":
    sensor.save("main-camera.png", compression=0)
```

`save` method accepts path relative to running simulator, and optional `compression` for png files (0...9)
or `quality` (0..100) for jpeg files.

### Lidar Sensor

Lidar sensor has following read only properties:

  * `min_distance` - minimal distance for capturing points
  * `max_distance` - maximum distance for capturing points
  * `rays` - how many laser rays (vertically) to use
  * `rotations` - frequency of rotation, typically 10Hz
  * `measurements` - how many measurmenets per rotation each ray is doing
  * `fov` - vertical field of view (bottom to top ray) in degrees
  * `angle` - angle lidar is tilted (middle of fov view)
  * `compensated` - bool if lidar point cloud is compensated

Lidar point cloud can be saved to disk to .pcd file by calling `save` method:

```python
ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO)

for sensor in ego.get_sensors():
  if sensor.name = "velodyne":
    sensor.save("lidar.pcd")
```

`.pcd` file is in [binary Point Cloud Data format](http://pointclouds.org/documentation/tutorials/pcd_file_format.php)
Each point has x/y/z coordinates as 4-byte float and 1-byte unsigned int as intensity (0...255).

### IMU Sensor

You can use IMU sensor to get its position in the vehicle. To read actual IMU data, use `transform`
property of agent.

### GPS Sensor

You can retrieve current GPS location from GPS sensor by calling `data` method:

```python
var data = gps_sensor.data()
print("Latitude:", data.latitude)
```

Returned data will contain following fields:

 * `latitude`
 * `longitude`
 * `northing`
 * `easting`
 * `altitude`
 * `orientation` - rotation around up-axis in degrees

### Radar Sensor

Currently Radar sensor can be used only to get its position and rotation in the vehicle.

### CAN bus

Currently CAN bus can be used only to get its position and rotation in the vehicle.

## Weather and Time of Day Control

You can control weather properties of simulator by reading or writing to `weather` property. You can
change `rain`, `fog` or `wetness` (float 0...1). Example:

```python
w = sim.weather
w.rain = 0.5     # set rain to 50%
sim.weather = w
```

Changing time of day allows to control wether you see day or night in the loaded scene.
To get current time read `time_of_day` property:

```python
print("Current time of day:", sim.time_of_day)
```

It will return float from 0 to 24. To set time of day call `set_time_of_day` method:
```python
sim.set_time_of_day(10, fixed=True)
```

This will set current time of day to 10am. Optional bool argument `fixed` indices if simulation
should advance this time automatically or freeze it and not change it (`fixed=True`).

## Helper Functionality

Simulator class offers following helper functionality:

 * `version` - property that returns current version of simulator as string
 * `current_scene` - property that returns currently loaded scene as string, None if none is loaded
 * `current_frame` - property that returns currently simulated frame number as integer
 * `current_time` - property that returns currentl simulation time in seconds as float
 * `get_spawn` - method that returns list of transforms representing good positions where to place
   vehicles in the map. This list can be empty, it depends how map is prepared in Unity. Returned
   transforms contain `position` and `rotation` members as `Vector`
 * `get_agents` - method that returns list of currently added agent objects with `add_agent`

To map point in Unity coordinates to GPS location Simulator class offers following two functions:

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
This will shoot ray in positive x-axis direction from (10,0,20) coordinate.
If it returns None, then nothing was hit. Otherwise it returns `RaycastHit` object with `distance`,
`point` and `normal` fields.

When doing raycast you should specify `layer_mask` argument that specifies which objects to check
collision with. It corressponds to layers in Unity project - check the project for actual values.

## Changelog

* 2019-04-19

    * initial release


