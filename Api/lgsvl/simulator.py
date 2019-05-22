#
# Copyright (c) 2019 LG Electronics, Inc.
#
# This software contains code licensed as described in LICENSE.
#

from .remote import Remote
from .agent import Agent, AgentType, AgentState
from .sensor import GpsData
from .geometry import Vector, Transform
from .utils import accepts

from collections import namedtuple

RaycastHit = namedtuple("RaycastHit", "distance point normal")

WeatherState = namedtuple("WeatherState", "rain fog wetness")


class Simulator:

  @accepts(str, int)
  def __init__(self, address = "localhost", port = 8181):
    if port <= 0 or port > 65535: raise ValueError("port value is out of range")
    self.remote = Remote(address, port)
    self.agents = {}
    self.callbacks = {}
    self.stopped = False

  def close(self):
    self.remote.close()

  @accepts(str)
  def load(self, scene):
    self.remote.command("simulator/load_scene", {"scene": scene})
    self.agents.clear()
    self.callbacks.clear()

  @property
  def version(self):
    return self.remote.command("simulator/version")

  @property
  def current_scene(self):
    return self.remote.command("simulator/current_scene")

  @property
  def current_frame(self):
    return self.remote.command("simulator/current_frame")

  @property
  def current_time(self):
    return self.remote.command("simulator/current_time")

  def reset(self):
    self.remote.command("simulator/reset")
    self.agents.clear()
    self.callbacks.clear()

  def stop(self):
    self.stopped = True

  @accepts((int, float))
  def run(self, time_limit = 0.0):
    self._process("simulator/run", {"time_limit": time_limit})

  @accepts(int, (int, float))
  def step(self, frames = 1, framerate = 30.0):
    raise NotImplementedError()

  def _add_callback(self, agent, name, fn):
    if agent not in self.callbacks:
      self.callbacks[agent] = {}
    if name not in self.callbacks[agent]:
      self.callbacks[agent][name] = set()
    self.callbacks[agent][name].add(fn)

  def _process_events(self, events):
    self.stopped = False
    for ev in events:
      agent = self.agents[ev["agent"]]
      if agent in self.callbacks:
        callbacks = self.callbacks[agent]
        event_type = ev["type"]
        if event_type in callbacks:
          for fn in callbacks[event_type]:
            if event_type == "collision":
              fn(agent, self.agents.get(ev["other"]), Vector.from_json(ev["contact"]))
            elif event_type == "waypoint_reached":
              fn(agent, ev["index"])
            elif event_type == "stop_line":
              fn(agent)
            elif event_type == "lane_change":
              fn(agent)
            if self.stopped:
              return

  def _process(self, cmd, args):
    j = self.remote.command(cmd, args)
    while True:
      if j is None:
        return
      if "events" in j:
        self._process_events(j["events"])
        if self.stopped:
          break
      j = self.remote.command("simulator/continue")

  @accepts(str, AgentType, AgentState)
  def add_agent(self, name, agent_type, state = None):
    if state is None: state = AgentState()
    args = {"name": name, "type": agent_type.value, "state": state.to_json()}
    uid = self.remote.command("simulator/add_agent", args)
    agent = Agent.create(self, uid, agent_type)
    agent.name = name
    self.agents[uid] = agent
    return agent

  @accepts(Agent)
  def remove_agent(self, agent):
    self.remote.command("simulator/agent/remove", {"uid": agent.uid})
    del self.agents[agent.uid]
    if agent in self.callbacks:
      del self.callbacks[agent]

  def get_agents(self):
    return list(self.agents.values())

  @property
  def weather(self):
    j = self.remote.command("environment/weather/get")
    return WeatherState(j["rain"], j["fog"], j["wetness"])

  @weather.setter
  @accepts(WeatherState)
  def weather(self, state):
    self.remote.command("environment/weather/set", {"rain": state.rain, "fog": state.fog, "wetness": state.wetness})

  @property
  def time_of_day(self):
    return self.remote.command("environment/time/get")

  @accepts((int, float), bool)
  def set_time_of_day(self, time, fixed = True):
    self.remote.command("environment/time/set", {"time": time, "fixed": fixed})

  def get_spawn(self):
    spawns = self.remote.command("map/spawn/get")
    return [Transform.from_json(spawn) for spawn in spawns]

  @accepts(Transform)
  def map_to_gps(self, transform):
    j = self.remote.command("map/to_gps", {"transform": transform.to_json()})
    return GpsData(j["latitude"], j["longitude"], j["northing"], j["easting"], j["altitude"], j["orientation"])

  def map_from_gps(self, latitude = None, longitude = None, northing = None, easting = None, altitude = None, orientation = None):
    j = {}
    numtype = (int, float)
    if latitude is not None and longitude is not None:
      if not isinstance(latitude, numtype): raise TypeError("Argument 'latitude' should have '{}' type".format(numtype))
      if not isinstance(longitude, numtype): raise TypeError("Argument 'longitude' should have '{}' type".format(numtype))
      if latitude < -90 or latitude > 90: raise ValueError("Latitude is out of range")
      if longitude < -180 or longitude > 180: raise ValueError("Longitude is out of range")
      j["latitude"] = latitude
      j["longitude"] = longitude
    elif northing is not None and easting is not None:
      if not isinstance(northing, numtype): raise TypeError("Argument 'northing' should have '{}' type".format(numtype))
      if not isinstance(easting, numtype): raise TypeError("Argument 'easting' should have '{}' type".format(numtype))
      if northing < 0 or northing > 10000000: raise ValueError("Northing is out of range")
      if easting < -340000 or easting > 334000 : raise ValueError("Easting is out of range")
      j["northing"] = northing
      j["easting"] = easting
    else:
      raise Exception("Either latitude and longitude or northing and easting should be specified")
    if altitude is not None:
      if not isinstance(altitude, numtype): raise TypeError("Argument 'altitude' should have '{}' type".format(numtype))
      j["altitude"] = altitude
    if orientation is not None:
      if not isinstance(orientation, numtype): raise TypeError("Argument 'orientation' should have '{}' type".format(numtype))
      j["orientation"] = orientation
    j = self.remote.command("map/from_gps", j)
    return Transform.from_json(j)

  @accepts(Vector)
  def map_point_on_lane(self, point):
    j = self.remote.command("map/point_on_lane", {"point": point.to_json()})
    return Transform.from_json(j)

  @accepts(Vector, Vector, int, float)
  def raycast(self, origin, direction, layer_mask = -1, max_distance = float("inf")):
    hit = self.remote.command("simulator/raycast", {
      "origin": origin.to_json(),
      "direction": direction.to_json(),
      "layer_mask": layer_mask,
      "max_distance": max_distance
    })
    if hit is None:
      return None
    return RaycastHit(hit["distance"], Vector.from_json(hit["point"]), Vector.from_json(hit["normal"]))

  # @accepts(bool)
  # def set_physics(self, isPhysicsSimple):
  #   self.remote.command("vehicle/set_npc_physics", {"isPhysicsSimple": isPhysicsSimple})