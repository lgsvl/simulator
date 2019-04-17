#!/usr/bin/env python3
#
# Copyright (c) 2019 LG Electronics, Inc.
#
# This software contains code licensed as described in LICENSE.
#

import os
import lgsvl

sim = lgsvl.Simulator(os.environ.get("SIMULATOR_HOST", "127.0.0.1"), 8181)
sim.load("SanFrancisco")

spawns = sim.get_spawn()

state = lgsvl.AgentState()
state.transform = spawns[0]
sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, state)

p = spawns[0].position
p.y += 1

# useful bits in layer mask
# 0 - Default (buildings, obstacles)
# 4 - Water
# 8 - EGO vehicles
# 13 - Ground and Road, Sidewalks
# 14 - NPC vehicles
# 18 - Pedestrian

layer_mask = 0
for bit in [0, 4, 13, 14, 18]: # do not put 8 here, to not hit EGO vehicle itself
  layer_mask |= 1 << bit

hit = sim.raycast(p, lgsvl.Vector(0,0,1), layer_mask)
if hit:
  print("Distance right:", hit.distance)

hit = sim.raycast(p, lgsvl.Vector(0,0,-1), layer_mask)
if hit:
  print("Distance left:", hit.distance)

hit = sim.raycast(p, lgsvl.Vector(1,0,0), layer_mask)
if hit:
  print("Distance back:", hit.distance)

hit = sim.raycast(p, lgsvl.Vector(-1,0,0), layer_mask)
if hit:
  print("Distance forward:", hit.distance)

hit = sim.raycast(p, lgsvl.Vector(0,1,0), layer_mask)
if hit:
  print("Distance up:", hit.distance)

hit = sim.raycast(p, lgsvl.Vector(0,-1,0), layer_mask)
if hit:
  print("Distance down:", hit.distance)
