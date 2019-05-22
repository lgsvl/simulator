#
# Copyright (c) 2019 LG Electronics, Inc.
#
# This software contains code licensed as described in LICENSE.
#

from .geometry import Vector, Transform, BoundingBox
from .sensor import Sensor
from .utils import accepts

from enum import Enum
from collections import namedtuple
from collections.abc import Iterable, Callable
import math

DriveWaypoint = namedtuple("DriveWaypoint", "position speed")
WalkWaypoint = namedtuple("WalkWaypoint", "position idle")

class AgentType(Enum):
  EGO = 1
  NPC = 2
  PEDESTRIAN = 3


class VehicleControl:
  def __init__(self):
    self.steering = 0.0     # [-1..+1]
    self.throttle = 0.0     # [0..1]
    self.braking = 0.0     # [0..1]
    self.reverse = False
    self.handbrake = False

    # optional
    self.headlights = None         # int, 0=off, 1=low, 2=high beams
    self.windshield_wipers = None  # int, 0=off, 1-3=on
    self.turn_signal_left = None   # bool
    self.turn_signal_right = None  # bool

class NPCControl:
  def __init__(self):
    self.headlights = None        # int, 0=off, 1=low, 2=high
    self.hazards = None           # bool
    self.e_stop = None            # bool
    self.turn_signal_left = None  # bool
    self.turn_signal_right = None # bool


class AgentState:
  def __init__(self, transform = None, velocity = None, angular_velocity = None):
    if transform is None: transform = Transform()
    if velocity is None: velocity = Vector()
    if angular_velocity is None: angular_velocity = Vector()
    self.transform = transform
    self.velocity = velocity
    self.angular_velocity = angular_velocity

  @property
  def position(self):
    return self.transform.position

  @property
  def rotation(self):
    return self.transform.rotation

  @property
  def speed(self):
    return math.sqrt(
      self.velocity.x * self.velocity.x +
      self.velocity.y * self.velocity.y +
      self.velocity.z * self.velocity.z)

  @staticmethod
  def from_json(j):
    return AgentState(
      Transform.from_json(j["transform"]),
      Vector.from_json(j["velocity"]),
      Vector.from_json(j["angular_velocity"]),
    )

  def to_json(self):
    return {
      "transform": self.transform.to_json(),
      "velocity": self.velocity.to_json(),
      "angular_velocity": self.angular_velocity.to_json(),
    }

  def __repr__(self):
    return str({
      "transform": str(self.transform),
      "velocity": str(self.velocity),
      "angular_velocity": str(self.angular_velocity),
    })


class Agent:
  def __init__(self, uid, simulator):
    self.uid = uid
    self.remote = simulator.remote
    self.simulator = simulator

  @property
  def state(self):
    j = self.remote.command("agent/state/get", {"uid": self.uid})
    return AgentState.from_json(j)

  @state.setter
  @accepts(AgentState)
  def state(self, state):
    j = state.to_json()
    self.remote.command("agent/state/set", {
      "uid": self.uid,
      "state": state.to_json()
    })

  @property
  def transform(self):
    return self.state.transform

  @property
  def bounding_box(self):
    j = self.remote.command("agent/bounding_box/get", {"uid": self.uid})
    return BoundingBox.from_json(j)

  def __eq__(self, other):
    return self.uid == other.uid

  def __hash__(self):
    return hash(self.uid)

  @accepts(Callable)
  def on_collision(self, fn):
    self.remote.command("agent/on_collision", {"uid": self.uid})
    self.simulator._add_callback(self, "collision", fn)

  @staticmethod
  def create(simulator, uid, agent_type):
    if agent_type == AgentType.EGO:
      return EgoVehicle(uid, simulator)
    elif agent_type == AgentType.NPC:
      return NpcVehicle(uid, simulator)
    elif agent_type == AgentType.PEDESTRIAN:
      return Pedestrian(uid, simulator)
    else:
      raise ValueError("unsupported agent type")


class Vehicle(Agent):
  def __init__(self, uid, simulator):
    super().__init__(uid, simulator)


class EgoVehicle(Vehicle):
  def __init__(self, uid, simulator):
    super().__init__(uid, simulator)

  @property
  def bridge_connected(self):
    return self.remote.command("vehicle/bridge/connected", {"uid": self.uid})

  @accepts(str, int)
  def connect_bridge(self, address, port):
    if port <= 0 or port > 65535: raise ValueError("port value is out of range")
    self.remote.command("vehicle/bridge/connect", {"uid": self.uid, "address": address, "port": port})

  def get_sensors(self):
    j = self.remote.command("vehicle/sensors/get", {"uid": self.uid})
    return [Sensor.create(self.remote, sensor) for sensor in j]

  @accepts(bool, float)
  def set_fixed_speed(self, isCruise, speed=None):
    self.remote.command("vehicle/set_fixed_speed", {"uid": self.uid, "isCruise": isCruise, "speed": speed})

  @accepts(VehicleControl, bool)
  def apply_control(self, control, sticky = False):
    args = {
      "uid": self.uid,
      "sticky": sticky,
      "control": {
        "steering": control.steering,
        "throttle": control.throttle,
        "braking": control.braking,
        "reverse": control.reverse,
        "handbrake": control.handbrake,
      }
    }
    if control.headlights is not None:
      args["control"]["headlights"] = control.headlights
    if control.windshield_wipers is not None:
      args["control"]["windshield_wipers"] = control.windshield_wipers
    if control.turn_signal_left is not None:
      args["control"]["turn_signal_left"] = control.turn_signal_left
    if control.turn_signal_right is not None:
      args["control"]["turn_signal_right"] = control.turn_signal_right
    self.remote.command("vehicle/apply_control", args)


class NpcVehicle(Vehicle):
  def __init__(self, uid, simulator):
    super().__init__(uid, simulator)

  @accepts(Iterable, bool)
  def follow(self, waypoints, loop = False):
    self.remote.command("vehicle/follow_waypoints", {
      "uid": self.uid,
      "waypoints": [{"position": wp.position.to_json(), "speed": wp.speed} for wp in waypoints],
      "loop": loop,
    })

  def follow_closest_lane(self, follow, max_speed, isLaneChange=True):
    self.remote.command("vehicle/follow_closest_lane", {"uid": self.uid, "follow": follow, "max_speed": max_speed, "isLaneChange": isLaneChange})

  @accepts(bool)
  def change_lane(self, isLeftChange):
    self.remote.command("vehicle/change_lane", {"uid": self.uid, "isLeftChange": isLeftChange})

  @accepts(NPCControl)
  def apply_control(self, control):
    args = {
      "uid": self.uid,
      "control":{}
    }
    if control.headlights is not None:
      if not control.headlights in [0,1,2]:
        raise ValueError("unsupported intensity value")
      args["control"]["headlights"] = control.headlights
    if control.hazards is not None:
      args["control"]["hazards"] = control.hazards
    if control.e_stop is not None:
      args["control"]["e_stop"] = control.e_stop
    if control.turn_signal_left is not None or control.turn_signal_right is not None:
      args["control"]["isLeftTurnSignal"] = control.turn_signal_left
      args["control"]["isRightTurnSignal"] = control.turn_signal_right
    self.remote.command("vehicle/apply_npc_control", args)

  def on_waypoint_reached(self, fn):
    self.remote.command("agent/on_waypoint_reached", {"uid": self.uid})
    self.simulator._add_callback(self, "waypoint_reached", fn)

  def on_stop_line(self, fn):
    self.remote.command("agent/on_stop_line", {"uid": self.uid})
    self.simulator._add_callback(self, "stop_line", fn)

  def on_lane_change(self, fn):
    self.remote.command("agent/on_lane_change", {"uid": self.uid})
    self.simulator._add_callback(self, "lane_change", fn)


class Pedestrian(Agent):
  def __init__(self, uid, simulator):
    super().__init__(uid, simulator)
  
  @accepts(bool)
  def walk_randomly(self, enable):
    self.remote.command("pedestrian/walk_randomly", {"uid": self.uid, "enable": enable})

  @accepts(Iterable, bool)
  def follow(self, waypoints, loop = False):
    self.remote.command("pedestrian/follow_waypoints", {
      "uid": self.uid,
      "waypoints": [{"position": wp.position.to_json(), "idle": wp.idle} for wp in waypoints],
      "loop": loop,
    })

  @accepts(Callable)
  def on_waypoint_reached(self, fn):
    self.remote.command("agent/on_waypoint_reached", {"uid": self.uid})
    self.simulator._add_callback(self, "waypoint_reached", fn)
