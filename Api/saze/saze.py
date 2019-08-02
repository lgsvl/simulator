import os
#import lgsvl

import sys
sys.path.append('../')
from lgsvl import simulator
from lgsvl import agent
import math
import threading
import time

def print_msg(tag, msg):
    print("{0}: {1}".format(tag, msg))

def open_simulator(map_name, sim_host = "127.0.0.1", port = 8181):
    sim = simulator.Simulator(os.environ.get("SIMULATOR_HOST", sim_host), port)
    if sim.current_scene == map_name:
        sim.reset()
    else:
        sim.load(map_name)

    return sim

def spawn_ego(sim):
    spawns = sim.get_spawn()

    state = agent.AgentState()
    state.transform = spawns[0]
    ego = sim.add_agent("XE_Rigged-apollo", agent.AgentType.EGO, state)

    return ego

def spawn_npc(sim, pos, car_type):
    state = agent.AgentState()
    state.transform = sim.map_point_on_lane(pos)
    npc = sim.add_agent(car_type, agent.AgentType.NPC, state)
    return npc

def get_gps_sensor(ego):
    gps_sensor = None
    for sensor in ego.get_sensors():
        if sensor.name == "GPS":
            gps_sensor = sensor
    return gps_sensor

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
