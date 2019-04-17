#!/usr/bin/env python3
#
# Copyright (c) 2019 LG Electronics, Inc.
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

sx = state.transform.position.x
sz = state.transform.position.z

state = lgsvl.AgentState()
state.transform = spawns[1]
state.transform.position.x = sx - 10
state.transform.position.z = sz + 5

p = sim.add_agent("Bob", lgsvl.AgentType.PEDESTRIAN, state)
p.walk_randomly(True)

input("enter to walk")

sim.run(3)

input("enter to stop")

p.walk_randomly(False)

sim.run(5)
