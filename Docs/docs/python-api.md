# Python API Guide

## Overview

LGSVL Simulator exposes runtime functionality to a Python API which you can use
to manipulate object placement and vehicle movement in a loaded scene, retreive
sensor configuration and data, control weather, time state, and more.

## Requirements

Using Python API requires Python version 3.5 or later.

## Quickstart

After unpacking LGSVL simulator zip file or cloning source from the git repository
you should see an `Api` folder in the root. The Python API interface is fully contained
in this folder.

1. Go inside this folder and run the following command to install Python files and
   necessary dependencies:

    ```
    pip3 install --user -e .
    ```

2. Now launch the simulator (either binary .exe file or from Unity Editor) and
   leave it running in the `Menu.unity` scene. Simulator by default listens for connections
   on port 8181.

3. Run the following example to see the API in action:

    ```
    ./quickstart/05-ego-drive-in-circle.py
    ```

    This will load the `SanFrancisco.unity` scene, instantiate one EGO vehicle, then ask
    you to press `Enter` to start driving EGO vehicle in a circle.

When the script is running, it will apply throttle and steering commands to make the car move

## Core concepts

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

## Simulation

To connect to the simulator you need to an instance of the `Simulator` class:

```python
import lgsvl
sim = lgsvl.Simulator("localhost", 8181)
```

You can specify a different address as hostname or IP address. By default only port 8181 is
used for API connection. Only one client can be connected to simulator at a time.

Next, load the scene ("map"). This is done by `load` method:

```python
sim.load("SanFrancisco")
```

Map name is a string representing the name of the scene file in Unity. Currently available scenes:

 * **SanFrancisco** - large city map
 * **SimpleMap** - small city map
 * **SimpleRoom** - for Tugbot robot
 * **SimpleLoop** - for Duckiebot robot
 * **Duckietown** - for Duckiebot robot
 * **DuckieDowntown** - for Duckiebot robot

Check the Unity project for a full list of available scenes.

Once a scene is loaded you can instantiate agents and run simulations. See
the [Agents](#agents) section on how to create vehicles and pedestrians.

Loading scenes takes a while, to reset a scene to the initial state without reloading it call the `reset`
method:

```python
sim.reset()
```

This will remove any vehicles or callbacks currently registered.

After setting up the scene in a desired state you can start advancing time.
During python code execution time is stopped in the simulator. The only way to
advance time in the simulator is to call the `run` method:

```python
sim.run(time_limit = 5.0)
```

`run` accepts an optional argument for a time limit specifying how long to run. The default
value of 0 will run infinitely.

Diagram illustrating API execution:

![](images/python-api-execution.png)

## Agents

You can create vehicles and pedestrians by calling the `add_agent` method of the `Simulator`
object. Example:

```python
ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO)
```

This will create an EGO vehicle from the `XE_Rigged-apollo` template. Other AgentTypes
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

If an incorrect name is entered, a Python exception will be thrown.

Optionally you can create agents in specific positions and orientations in the scene.
For this you need to use the `AgentState` class. For example:

```python
state = lgsvl.AgentState()
state.transform.position = lgsvl.Vector(10, 0, 30)
state.transform.rotation.y = 90
ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, state)
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

## EGO vehicle

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
ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO)
c = lgsvl.VehicleControl()
c.throttle = 0.2
ego.apply_control(c, True)
```

## NPC vehicles

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
  lgsvl.DriveWaypoint(lgsvl.Vector(1,0,3), 5),
  lgsvl.DriveWaypoint(lgsvl.Vector(5,0,3), 10),
  lgsvl.DriveWaypoint(lgsvl.Vector(1,0,5), 5),
]
npc.follow(waypoints, loop=True)
```

Each waypoint has a position in world coordinates and a desired velocity in m/s. The NPC
will ignore all traffic rules and will not avoid collisions to try to get to the next
waypoint. You can receive information on progress by setting the `on_waypoint_reached`
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

## Pedestrians

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

## Callbacks

The Python API can invoke callbacks to inform you of specific events that occur during
simulator runtime. Callbacks are invoked from inside the `Simulator.run` method and
while a callback is running the simulation time is paused. Once the callback finishes time is resumed and the simulation resumes execution. You can call `Simulator.stop`
to stop further execution and return immediately from the callback.

The internals of this process are illustrated in the following sequence diagram:

![](images/python-api-callbacks.png)

Here the code resumes simulation after the first callback, but stops execution when the second
callback is handled.

You set callback functions by calling `on_NAME` method of object, see information below.

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

Callback receives three arguments: `(agent1, agent2, contact)` - the first two are the agents that collide,
one of them can be None if it is a stationary obstacle like a building or a traffic light pole,
and the third is the world position of the contact point.

### [NpcVehicle](#npc-vehicles) callbacks

In addition to Agent callbacks, NpcVehicle has three extra callbacks:

`waypoint_reached` - called when vehicle reaches a waypoint; accepts two
arguments: `(agent, index)` - agent instance and waypoint index as integer

`stop_line` - called when vehicle stops at a stop line for a traffic light or stop sign; accepts one argument: `(agent)` - agent instance

`lane_change` - called when vehicle starts changing lane; accepts one
argument: `(agent)` - agent instance

### [Pedestrian](#pedestrians) callbacks

In addition to Agent callbacks, Pedestrian has one extra callback.

`waypoint_reached` - called when pedestrian reaches waypoint; accepts two
arguments: `(agent, index)` - agent instance and waypoint index as integer.

## Sensors

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

### Camera Sensor

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
ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO)

for sensor in ego.get_sensors():
  if sensor.name = "Main Camera":
    sensor.save("main-camera.png", compression=0)
```

`save` method accepts a path relative to the running simulator, and an optional `compression` for png files (0...9)
or `quality` (0..100) for jpeg files.

### Lidar Sensor

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
ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO)

for sensor in ego.get_sensors():
  if sensor.name = "velodyne":
    sensor.save("lidar.pcd")
```

A `.pcd` file is in the [binary Point Cloud Data format](http://pointclouds.org/documentation/tutorials/pcd_file_format.php) where each point has x/y/z coordinates as 4-byte floats and a 1-byte unsigned int as intensity (0...255).

### IMU Sensor

You can use the IMU sensor to get its position in the vehicle. All measurements an IMU would provide can be obtained by using the `transform` property of the agent.

### GPS Sensor

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

### Radar Sensor

Currently the Radar sensor can be used only to get its position and rotation in the vehicle. Radar measurements can be received in ROS or Cyber by setting the `enabled` property of the sensor.

### CAN bus

Currently CAN bus can be used only to get its position and rotation in the vehicle. CAN bus messages can be received in ROS or Cyber by setting the `enabled` property of the sensor.

## Weather and Time of Day Control

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

## Helper Functions

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

## Changelog

* 2019-04-19

    * initial release


