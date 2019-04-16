#!/usr/bin/env python3
#
# Copyright (c) 2018 LG Electronics, Inc.
#
# This software contains code licensed as described in LICENSE.
#

import os
import lgsvl

sim = lgsvl.Simulator(os.environ.get("SIMULATOR_HOST", "127.0.0.1"), 8181)

print("Current Scene = {}".format(sim.current_scene))

sim.load("SanFrancisco")

print("Current Scene = {}".format(sim.current_scene))

spawns = sim.get_spawn()
for spawn in sim.get_spawn():
  print(spawn)
