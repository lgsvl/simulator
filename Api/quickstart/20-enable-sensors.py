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

sensors = a.get_sensors()

for s in sensors:
  print(type(s), s.enabled)

input("enter to enable lidar")

for s in sensors:
  if isinstance(s, lgsvl.LidarSensor):
    s.enabled = True

for s in sensors:
  print(type(s), s.enabled)

sim.run()
