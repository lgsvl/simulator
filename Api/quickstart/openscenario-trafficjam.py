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

import os
import lgsvl
import sys
import time
import math

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

# enable sensors required for Apollo 3.5
sensors = ego.get_sensors()
for s in sensors:
    if s.name in ['velodyne', 'Main Camera', 'Telephoto Camera', 'GPS', 'IMU']:
        s.enabled = True