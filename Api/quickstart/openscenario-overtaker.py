#!/usr/bin/env python3
#
# Copyright (c) 2019 LG Electronics, Inc.
#
# This software contains code licensed as described in LICENSE.
#

import os
import lgsvl
import sys
import time

sim = lgsvl.Simulator(os.environ.get("SIMULATOR_HOST", "127.0.0.1"), 8181)
if sim.current_scene == "SanFrancisco":
    sim.reset()
else:
    sim.load("SanFrancisco")

# spawn EGO in 2nd to right lane
egoState = lgsvl.AgentState()
egoState.transform.position = sim.map_point_on_lane(lgsvl.Vector(50,26.1,-685.4))
egoState.transform.rotation = lgsvl.Vector(0,85,0)
ego = sim.add_agent("XE_Rigged-apollo_3_5", lgsvl.AgentType.EGO, egoState)

# enable sensors required for Apollo 3.5
sensors = ego.get_sensors()
for s in sensors:
    if s.name in ['velodyne', 'Main Camera', 'Telephoto Camera', 'GPS', 'IMU']:
        s.enabled = True

# spawn NPC 50m behind the EGO in the same lane
npcState = lgsvl.AgentState()
npcState.transform.position = sim.map_point_on_lane(lgsvl.Vector(0,26.1,-691.7))
npcState.transform.rotation = lgsvl.Vector(0,85,0)
npc = sim.add_agent(sys.argv[1], lgsvl.AgentType.NPC, npcState)
npc.follow_closest_lane(True, 41.666666)

# Wait for bridge to connect before running simulator
ego.connect_bridge(os.environ.get("BRIDGE_IP", "127.0.0.1"), 9090)
while not ego.bridge_connected:
    time.sleep(1)

input("Press enter to run simulation")

while True:
    vehicleSeparation = npc.state.position.x - ego.state.position.x
    if  vehicleSeparation >= -20:
        npc.change_lane(True)
    if vehicleSeparation >= 15:
        npc.change_lane(False)
        break
    sim.run(1)

sim.run(5)