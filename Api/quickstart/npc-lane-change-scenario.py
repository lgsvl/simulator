#!/usr/bin/env python3
#
# Copyright (c) 2019 LG Electronics, Inc.
#
# This software contains code licensed as described in LICENSE.
#

# This scenario simulates an NPC vehicle suddenly changing lanes in front of the EGO.
# The lane change is implemented using the waypoint system to ensure that the NPC actually changes lanes.
# When using the waypoint system, the NPC ignores traffic rules and does NOT attempt to avoid collisions.

# A command line argument is required when running this scenario to select the type of NPC vehicle to create.
# SIMULATOR_HOST and BRIDGE_HOST environment variables need to be set. The default for both is localhost.
# The scenario assumes that the EGO's destination is ahead in the same lane.

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
npcWaypoints.append(lgsvl.DriveWaypoint(lgsvl.Vector(nx-15, ny, nz+3), npcSpeed))
npcWaypoints.append(lgsvl.DriveWaypoint(lgsvl.Vector(nx-50, ny, nz+4), npcSpeed))

print("Connecting to bridge")
# Wait for bridge to connect before running simulator
ego.connect_bridge(os.environ.get("BRIDGE_HOST", "127.0.0.1"), 9090)
while not ego.bridge_connected:
    time.sleep(1)
print("Bridge connected")

# The handbrake is applied to the EGO so that it doesn't move in the first second of data collection
egoControl = lgsvl.VehicleControl()
egoControl.handbrake = True
ego.apply_control(egoControl)

# 1 seconds of data sent to AV stack to allow modules to start
sim.run(1)

npc.follow(npcWaypoints)

input("Press enter to run simulation")
egoControl.handbrake = False
ego.apply_control(egoControl)
sim.run(10)
