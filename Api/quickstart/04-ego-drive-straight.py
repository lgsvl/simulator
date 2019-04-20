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
state.transform = spawns[0]
# Agents can be spawned with a velocity. Default is to spawn with 0 velocity
state.velocity = lgsvl.Vector(-50, 0, 0)
a = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, state)

# The bounding box of an agent are 2 points (min and max) such that the box formed from those 2 points completely encases the agent
print("Vehicle bounding box =", a.bounding_box)

print("Current time = ", sim.current_time)
print("Current frame = ", sim.current_frame)

input("Press Enter to drive forward for 2 seconds")

# The simulator can be run for a set amount of time. time_limit is optional and if omitted or set to 0, then the simulator will run indefinitely
sim.run(time_limit = 2.0)

print("Current time = ", sim.current_time)
print("Current frame = ", sim.current_frame)

input("Press Enter to continue driving for 2 seconds")

sim.run(time_limit = 2.0)

print("Current time = ", sim.current_time)
print("Current frame = ", sim.current_frame)
