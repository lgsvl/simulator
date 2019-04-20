#!/usr/bin/env python3
#
# Copyright (c) 2019 LG Electronics, Inc.
#
# This software contains code licensed as described in LICENSE.
#

import os
import lgsvl

sim = lgsvl.Simulator(os.environ.get("SIMULATOR_HOST", "127.0.0.1"), 8181)

print("Current Scene = {}".format(sim.current_scene))

# Loads the named map in the connected simulator. The available maps can be found in the Free Roam map selection drop down
if sim.current_scene == "SanFrancisco":
  sim.reset()
else:
  sim.load("SanFrancisco")

print("Current Scene = {}".format(sim.current_scene))

# This will print out the position and rotation vectors for each of the spawn points in the loaded map
spawns = sim.get_spawn()
for spawn in sim.get_spawn():
  print(spawn)
