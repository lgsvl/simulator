#!/usr/bin/env python3
#
# Copyright (c) 2019 LG Electronics, Inc.
#
# This software contains code licensed as described in LICENSE.
#

import os
import lgsvl

sim = lgsvl.Simulator(os.environ.get("SIMULATOR_HOST", "127.0.0.1"), 8181)
if sim.current_scene == "SanFrancisco":
  sim.reset()
else:
  sim.load("SanFrancisco")

spawns = sim.get_spawn()

state = lgsvl.AgentState()
state.transform = spawns[1]
a = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, state)

state = lgsvl.AgentState()
state.transform = spawns[1]

sx = spawns[1].position.x
sz = spawns[1].position.z

# 10 meters ahead, on right lane
state.transform.position.x = sx - 10.0
#state.transform.position.z = sz + 6.0

npc1 = sim.add_agent("Sedan", lgsvl.AgentType.NPC, state)

state = lgsvl.AgentState()
state.transform = spawns[1]

# 10 meters ahead, on left lane
state.transform.position.x = sx - 10.0
state.transform.position.z = sz - 4.0

npc2 = sim.add_agent("SUV", lgsvl.AgentType.NPC, state)

# 11.1 m/s is ~40 km/h
npc1.follow_closest_lane(True, 11.1)

# 5.6 m/s is ~20 km/h
npc2.follow_closest_lane(True, 5.6)

input("press enter to run")

sim.run()
