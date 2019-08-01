import os
import lgsvl
import math
import threading
import time

sim = lgsvl.Simulator(os.environ.get("SIMULATOR_HOST", "127.0.0.1"), 8181)
if sim.current_scene == "SimpleMap":
    sim.reset()
else:
    sim.load("SimpleMap")

spawns = sim.get_spawn()

state = lgsvl.AgentState()
state.transform = spawns[0]
ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, state)

gps_sensor = None
for sensor in ego.get_sensors():
    if sensor.name == "GPS":
        gps_sensor = sensor

print(gps_sensor.data)

sim_thread = threading.Thread(target = sim.run)
sim_thread.start()



"""
time.sleep(1)
sensors = ego.get_sensors()
print(len(sensors))
gps_sensor = None
for s in sensors:
    print(s.name)
    if s.name == "gps":
        gps_sensor = s
"""
while True:
    time.sleep(1)
    print("thread running")
    if gps_sensor:
        print(gps_sensor.data)

sim_thread.join()
