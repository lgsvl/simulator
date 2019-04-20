#!/usr/bin/env python3
#
# Copyright (c) 2019 LG Electronics, Inc.
#
# This software contains code licensed as described in LICENSE.
#

import os
import lgsvl
import math
import random

sim = lgsvl.Simulator(os.environ.get("SIMULATOR_HOST", "127.0.0.1"), 8181)
if sim.current_scene == "SanFrancisco":
  sim.reset()
else:
  sim.load("SanFrancisco")

spawns = sim.get_spawn()

state = lgsvl.AgentState()
state.transform = spawns[0]
a = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, state)

sx = spawns[0].position.x
sy = spawns[0].position.y
sz = spawns[0].position.z

mindist = 10.0
maxdist = 40.0

random.seed(0)

# Along with collisions and waypoints, NPCs can send a callback when they change lanes and reach a stopline
def on_stop_line(agent):
  print(agent.name, "reached stop line")

# This will be called when an NPC begins to change lanes
def on_lane_change(agent):
  print(agent.name, "is changing lanes")

# This creates 4 NPCs randomly in an area around the EGO
for name in ["Sedan", "SUV", "Jeep", "HatchBack"]:
  angle = random.uniform(0.0, 2*math.pi)
  dist = random.uniform(mindist, maxdist)

  point = lgsvl.Vector(sx + dist * math.cos(angle), sy, sz + dist * math.sin(angle))

  state = lgsvl.AgentState()
  state.transform = sim.map_point_on_lane(point)
  n = sim.add_agent(name, lgsvl.AgentType.NPC, state)
  n.follow_closest_lane(True, 10)
  n.on_lane_change(on_lane_change)
  n.on_stop_line(on_stop_line)

sim.run()
