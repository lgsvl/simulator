import os
#import lgsvl

import sys
sys.path.append('../')
from lgsvl import simulator
from lgsvl import agent
import math
import threading
import time

sim = simulator.Simulator(os.environ.get("SIMULATOR_HOST", "127.0.0.1"), 8181)
if sim.current_scene == "SimpleMap":
    sim.reset()
else:
    sim.load("SimpleMap")

spawns = sim.get_spawn()

state = agent.AgentState()
state.transform = spawns[0]
ego = sim.add_agent("XE_Rigged-apollo", agent.AgentType.EGO, state)

gps_sensor = None
for sensor in ego.get_sensors():
    if sensor.name == "GPS":
        gps_sensor = sensor

def gps_callback():
    print(gps_sensor.data)

print(sim.loop_callbacks)
sim._add_loop_callback(gps_callback)
sim.step()

"""
sim_thread = threading.Thread(target = )
sim_thread.start()

while True:
    time.sleep(1)
    print("thread running")
    if gps_sensor:
        print(gps_sensor.data)

sim_thread.join()
"""
