#!/usr/bin/env python3
#
# Copyright (c) 2019 LG Electronics, Inc.
#
# This software contains code licensed as described in LICENSE.
#

# This scenario recreates a situation described by an OpenScenario specification.
# In this situation, an NPC overtakes the EGO on the highway.
# For this scenario, the NPC uses the lane following system.
# This is because the distance traveled by the vehicles could vary and the NPC changes lanes at specific distances from the EGO.
# The scenario specification is available online from OpenScenario and is called "TafficJam"
# http://www.openscenario.org/download.html

# A command line argument is required when running this scenario to select the type of NPC vehicle to create.
# SIMULATOR_HOST and BRIDGE_HOST environment variables need to be set. The default for both is localhost.
# The scenario assumes that the EGO's destination is ahead in the same lane

import os
import lgsvl
import sys
import time
import math
import random

sim = lgsvl.Simulator(os.environ.get("SIMULATOR_HOST", "127.0.0.1"), 8181)
if sim.current_scene == "SanFrancisco":
    sim.reset()
else:
    sim.load("SanFrancisco")

# spawn EGO in the right lane
egoState = lgsvl.AgentState()
# A point close to the desired lane was found in Editor. This method returns the position and orientation of the closest lane to the point.
egoState.transform = sim.map_point_on_lane(lgsvl.Vector(1699.6, 88.38, -601.9))
ego = sim.add_agent("XE_Rigged-apollo_3_5", lgsvl.AgentType.EGO, egoState)

egoX = ego.state.position.x
egoY = ego.state.position.y
egoZ = ego.state.position.z

# enable sensors required for Apollo 3.5
sensors = ego.get_sensors()
for s in sensors:
    if s.name in ['velodyne', 'Main Camera', 'Telephoto Camera', 'GPS', 'IMU']:
        s.enabled = True

npcPossibilities = ["Sedan", "SUV", "Jeep", "HatchBack", "SchoolBus", "DeliveryTruck"]
npcNames = []
if len(sys.argv) == 1:
    npcNames = [random.choice(npcPossibilities) for i in range(6)]
elif len(sys.argv) == 2:
    if sys.argv[1] not in npcPossibilities:
        print("npc name not in list: Sedan , SUV , Jeep , HatchBack , SchoolBus , DeliveryTruck")
        sys.exit()
    npcNames = [sys.argv[1]] * 6
elif len(sys.argv) == 7:
    for i in range(6):
        if sys.argv[i+1] not in npcPossibilities:
            print("npc name not in list: Sedan , SUV , Jeep , HatchBack , SchoolBus , DeliveryTruck")
            sys.exit()
    npcNames = [sys.argv[i+1] for i in range(6)]
else:
    print("incompatible number of arguments")
    sys.exit()

# The bridge in San Francisco is not along an axis, so the positions of the NPCs in the spec need to be transformed to match
npcState = lgsvl.AgentState()
npcState.transform = sim.map_point_on_lane(lgsvl.Vector(egoX-149.6328, egoY, egoZ-10.4895))
npc1 = sim.add_agent(npcNames[0], lgsvl.AgentType.NPC, npcState) #A1 in spec

npcState.transform = sim.map_point_on_lane(lgsvl.Vector(egoX-139.412, egoY, egoZ-13.28))
npc2 = sim.add_agent(npcNames[1], lgsvl.AgentType.NPC, npcState) #A2 in spec

npcState.transform = sim.map_point_on_lane(lgsvl.Vector(egoX-149.388, egoY, egoZ-13.98))
npc3 = sim.add_agent(npcNames[2], lgsvl.AgentType.NPC, npcState) #A3 in spec

npcState.transform = sim.map_point_on_lane(lgsvl.Vector(egoX-138.85, egoY, egoZ-21.26))
npc4 = sim.add_agent(npcNames[3], lgsvl.AgentType.NPC, npcState) #A4 in spec

npcState.transform = sim.map_point_on_lane(lgsvl.Vector(egoX-148.828, egoY, egoZ-21.96))
npc5 = sim.add_agent(npcNames[4], lgsvl.AgentType.NPC, npcState) #A5 in spec

npcState.transform = sim.map_point_on_lane(lgsvl.Vector(egoX-139.6527, egoY, egoZ-9.79))
npc6 = sim.add_agent(npcNames[5], lgsvl.AgentType.NPC, npcState) #A6 in spec

print("Connecting to bridge")
# Wait for bridge to connect before running simulator
ego.connect_bridge(os.environ.get("BRIDGE_HOST", "127.0.0.1"), 9090)
while not ego.bridge_connected:
    time.sleep(1)
print("Bridge connected")

egoControl = lgsvl.VehicleControl()
egoControl.handbrake = True
ego.apply_control(egoControl)

# Collect 1 second of data to initialize AD modules
sim.run(1)

egoControl.handbrake = False
ego.apply_control(egoControl)

# The EGO starts with a speed of 36.111 km/h
egoState.velocity = lgsvl.Vector(math.sin(math.radians(egoState.rotation.y))*10, 0, math.cos(math.radians(egoState.rotation.y))*10)
ego.state = egoState

input("Press enter to run simulation")

# Allow the simulation to run for 15 more seconds
sim.run(45)