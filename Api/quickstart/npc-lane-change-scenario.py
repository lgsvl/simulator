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

if len(sys.argv) < 2:
    print("Insufficient arguments")
    sys.exit()

sim = lgsvl.Simulator(os.environ.get("SIMULATOR_HOST", "127.0.0.1"), 8181)
if sim.current_scene == "SanFrancisco":
    sim.reset()
else:
    sim.load("SanFrancisco")

# spawn EGO in right lane
egoState = lgsvl.AgentState()
egoState.transform = sim.get_spawn()[1]
ego = sim.add_agent("XE_Rigged-apollo_3_5", lgsvl.AgentType.EGO, egoState)

# enable sensors required for Apollo 3.5
sensors = ego.get_sensors()
for s in sensors:
    if s.name in ['velodyne', 'Main Camera', 'Telephoto Camera', 'GPS', 'IMU']:
        s.enabled = True

# spawn NPC in left lane
npcState = lgsvl.AgentState()
npcState.transform = sim.get_spawn()[0]
npc = sim.add_agent(sys.argv[1], lgsvl.AgentType.NPC, npcState)

# NPC will drive forward and then change lanes into the right lane at ~25 mph
npcSpeed = 12
nx = npc.state.position.x
ny = npc.state.position.y
nz = npc.state.position.z
npcWaypoints = []
npcWaypoints.append(lgsvl.DriveWaypoint(lgsvl.Vector(nx-2, ny, nz), npcSpeed))
npcWaypoints.append(lgsvl.DriveWaypoint(lgsvl.Vector(nx-15, ny, nz+4), npcSpeed))
npcWaypoints.append(lgsvl.DriveWaypoint(lgsvl.Vector(nx-50, ny, nz+4), npcSpeed))

# Wait for bridge to connect before running simulator
ego.connect_bridge(os.environ.get("BRIDGE_IP", "127.0.0.1"), 9090)
while not ego.bridge_connected:
    time.sleep(1)

egoControl = lgsvl.VehicleControl()
egoControl.handbrake = True
ego.apply_control(egoControl)

# 2 seconds of data sent to AV stack to allow Perception to start
sim.run(2)

npc.follow(npcWaypoints)

input("Press enter to run simulation")
egoControl.handbrake = False
ego.apply_control(egoControl)
sim.run(10)

sys.exit()