import os
import lgsvl
from lgsvl import Transform, Vector

def print_msg(tag, msg):
    print("{0}: {1}".format(tag, msg))

def open_simulator(map_name, sim_host = "127.0.0.1", port = 8181):
    sim = lgsvl.Simulator(os.environ.get("SIMULATOR_HOST", sim_host), port)
    if sim.current_scene == map_name:
        sim.reset()
    else:
        sim.load(map_name)

    return sim

def spawn_ego(sim, pos = None):

    state = lgsvl.AgentState()
    if pos:
        state.transform = sim.map_point_on_lane(pos)
    else:
        spawns = sim.get_spawn()
        state.transform = spawns[0]
    ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, state)

    return ego

def spawn_npc(sim, pos, car_type, offset = Vector(0, 0, 0)):
    state = lgsvl.AgentState()
    state.transform = sim.map_point_on_lane(pos)
    state.transform.position += offset
    npc = sim.add_agent(car_type, lgsvl.AgentType.NPC, state)
    return npc

def spawn_pedestrian(sim, pos, name, offset = Vector(0, 0, 0), rotation = Vector(0, 0, 0)):
    state = lgsvl.AgentState()
    spawns = sim.get_spawn()
    state.transform = sim.map_point_on_lane(pos)
    state.transform.position += offset
    state.transform.rotation += rotation
    ped = sim.add_agent(name, lgsvl.AgentType.PEDESTRIAN, state)
    return ped

def get_gps_sensor(ego):
    gps_sensor = None
    for sensor in ego.get_sensors():
        if sensor.name == "GPS":
            gps_sensor = sensor
    return gps_sensor

def get_npc_event(sim, npc, way_vecs, speeds):
    waypoints = []
    for i, vec in enumerate(way_vecs):
        on_lane_vec = sim.map_point_on_lane(vec).position
        wp = lgsvl.DriveWaypoint(on_lane_vec, speeds[i])
        waypoints.append(wp)

    def npc_event_func():
        npc.follow(waypoints)

    npc_event = Event(func = npc_event_func, params = None, only_once = True)
    return npc_event

def get_pedestrian_event(ped, waypoints):
    def event_func():
        ped.follow(waypoints, False)
    event = Event(func = event_func, params = None, only_once = True)
    return event

class Event:
    def __init__(self, func, params, only_once):
        self.func = func
        self.params = params
        self.only_once = only_once

        self.triggered = False

    def trigger(self):
        if self.only_once:
            if not self.triggered:
                self._run_func()
        else:
            self._run_func()

    def _run_func(self):
        if self.params and len(self.params) > 0:
            self.func(*self.params)
        else:
            self.func()
        self.triggered = True
