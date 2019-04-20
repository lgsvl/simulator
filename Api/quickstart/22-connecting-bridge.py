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
state.transform = spawns[0]
a = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, state)

# An EGO will not connect to a bridge unless commanded to
print("Bridge connected:", a.bridge_connected)

# The EGO is now looking for a bridge at the specified IP and port
a.connect_bridge("127.0.0.1", 9090)

print("Waiting for connection...")

while not a.bridge_connected:
  time.sleep(1)

print("Bridge connected:", a.bridge_connected)
