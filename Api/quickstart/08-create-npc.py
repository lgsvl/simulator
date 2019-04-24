#!/usr/bin/env python3
#
# Copyright (c) 2019 LG Electronics, Inc.
#
# This software contains code licensed as described in LICENSE.
#

import os
import lgsvl
import random

sim = lgsvl.Simulator(os.environ.get("SIMULATOR_HOST", "127.0.0.1"), 8181)
if sim.current_scene == "SanFrancisco":
  sim.reset()
else:
  sim.load("SanFrancisco")

spawns = sim.get_spawn()

state = lgsvl.AgentState()
state.transform = spawns[1]
a = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, state)

# Spawn NPC vehicles 10 meters ahead of the EGO
sx = spawns[1].position.x - 10.0
sz = spawns[1].position.z

# Spawns one of each of the listed types of NPCS
# The first will be created in front of the EGO and then they will be created to the left
# The available types of NPCs are listed in Unity Editor NPCManager script under NPC Vehicles
for i, name in enumerate(["Sedan", "SUV", "Jeep", "HatchBack"]):
  state = lgsvl.AgentState()
  state.transform = spawns[1]

  state.transform.position.x = sx
  state.transform.position.z = sz - 4.0 * i
  sim.add_agent(name, lgsvl.AgentType.NPC, state)
