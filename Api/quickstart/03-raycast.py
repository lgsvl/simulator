#!/usr/bin/env python3
#
# Copyright (c) 2019 LG Electronics, Inc.
#
# This software contains code licensed as described in LICENSE.
#

import os
import lgsvl

sim = lgsvl.Simulator(os.environ.get("SIMULATOR_HOST", "127.0.0.1"), 8181)
if sim.current_scene == "SanFrancisco":
  sim.reset()
else:
  sim.load("SanFrancisco")

# The next few lines spawns an EGO vehicle in the map
spawns = sim.get_spawn()

state = lgsvl.AgentState()
state.transform = spawns[0]
sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, state)

# This is the point from which the rays will originate from. It is raised 1m from the ground
p = spawns[0].position
p.y += 1

# useful bits in layer mask
# 0 - Default (buildings, obstacles)
# 4 - Water
# 8 - EGO vehicles
# 13 - Ground and Road, Sidewalks
# 14 - NPC vehicles
# 18 - Pedestrian

# Included layers can be hit by the rays. Otherwise the ray will go through the layer
layer_mask = 0
for bit in [0, 4, 13, 14, 18]: # do not put 8 here, to not hit EGO vehicle itself
  layer_mask |= 1 << bit

# raycast returns None if the ray doesn't collide with anything
# hit also has the point property which is the Unity position vector of where the ray collided with something
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
