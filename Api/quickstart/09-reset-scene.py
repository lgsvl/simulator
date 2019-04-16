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
import math

random.seed(0)

sim = lgsvl.Simulator(os.environ.get("SIMULATOR_HOST", "127.0.0.1"), 8181)
if sim.current_scene == "SanFrancisco":
  sim.reset()
else:
  sim.load("SanFrancisco")

spawns = sim.get_spawn()

state = lgsvl.AgentState()
state.transform = spawns[1]
a = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, state)

# 10 meters ahead
sx = state.transform.position.x - 10.0
sz = state.transform.position.z

for i, name in enumerate(["Sedan", "SUV", "Jeep", "HatchBack"]):
  state = lgsvl.AgentState()
  state.transform = spawns[1]

  state.transform.position.x = sx
  state.transform.position.z = sz - 4.0 * i
  sim.add_agent(name, lgsvl.AgentType.NPC, state)

input("press enter to reset")

sim.reset()
