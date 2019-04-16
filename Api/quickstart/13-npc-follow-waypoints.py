#!/usr/bin/env python3
#
# Copyright (c) 2018 LG Electronics, Inc.
#
# This software contains code licensed as described in LICENSE.
#

import os
import lgsvl
import math

sim = lgsvl.Simulator(os.environ.get("SIMULATOR_HOST", "127.0.0.1"), 8181)
if sim.current_scene == "SanFrancisco":
  sim.reset()
else:
  sim.load("SanFrancisco")

spawns = sim.get_spawn()

# EGO

state = lgsvl.AgentState()
state.transform = spawns[0]
a = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, state)

# NPC, 10 meters ahead

sx = spawns[0].position.x - 10.0
sy = spawns[0].position.y
sz = spawns[0].position.z

state = lgsvl.AgentState()
state.transform = spawns[0]
state.transform.position.x = sx
state.transform.position.x = sz
npc = sim.add_agent("Sedan", lgsvl.AgentType.NPC, state)

# snake-drive

waypoints = []
z_max = 4
x_delta = 12
for i in range(20):
  speed = 6 if i % 2 == 0 else 12
  px = (i + 1) * x_delta
  pz = z_max * (-1 if i % 2 == 0 else 1)

  wp = lgsvl.DriveWaypoint(lgsvl.Vector(sx - px, sy, sz - pz), speed)
  waypoints.append(wp)

def on_waypoint(agent, index):
  print("waypoint {} reached".format(index))

npc.on_waypoint_reached(on_waypoint)

npc.follow(waypoints)

input("press enter to run")

sim.run()
