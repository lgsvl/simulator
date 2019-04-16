#!/usr/bin/env python3
#
# Copyright (c) 2018 LG Electronics, Inc.
#
# This software contains code licensed as described in LICENSE.
#

import os
import lgsvl
import random
import time

sim = lgsvl.Simulator(os.environ.get("SIMULATOR_HOST", "127.0.0.1"), 8181)
if sim.current_scene == "SanFrancisco":
  sim.reset()
else:
  sim.load("SanFrancisco")

spawns = sim.get_spawn()

state = lgsvl.AgentState()
state.transform = spawns[1]
a = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, state)

print(sim.time_of_day)

input("enter to set fixed time to 19:00")

sim.set_time_of_day(19.0)
print(sim.time_of_day)

sim.run(5)

input("enter to set normal time to 10:30")

sim.set_time_of_day(10.5, False)
print(sim.time_of_day)

sim.run(5)

print(sim.time_of_day)
