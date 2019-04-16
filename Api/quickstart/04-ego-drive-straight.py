#!/usr/bin/env python3
#
# Copyright (c) 2018 LG Electronics, Inc.
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
state.transform = spawns[0]
state.velocity = lgsvl.Vector(-50, 0, 0)
a = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, state)

print("Vehicle bounding box =", a.bounding_box)

print("Current time = ", sim.current_time)
print("Current frame = ", sim.current_frame)

input("press enter to start driving")

sim.run(time_limit = 2.0)

print("Current time = ", sim.current_time)
print("Current frame = ", sim.current_frame)

input("press enter to continue driving")

sim.run(time_limit = 2.0)

print("Current time = ", sim.current_time)
print("Current frame = ", sim.current_frame)
