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
import math

sim = lgsvl.Simulator(os.environ.get("SIMULATOR_HOST", "127.0.0.1"), 8181)
if sim.current_scene == "SanFrancisco":
  sim.reset()
else:
  sim.load("SanFrancisco")

spawns = sim.get_spawn()

state = lgsvl.AgentState()
state.transform = spawns[1]
a = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, state)

sx = state.position.x - 10
sy = state.position.y
sz = state.position.z + 10

# This will create waypoints in a circle for the pedestrian to follow
radius = 5
count = 8
wp = []
for i in range(count):
  x = radius * math.cos(i * 2 * math.pi / count)
  z = radius * math.sin(i * 2 * math.pi / count)
  # idle is how much time the pedestrian will wait once it reaches the waypoint
  idle = 1 if i < count//2 else 0
  wp.append(lgsvl.WalkWaypoint(lgsvl.Vector(sx + x, sy, sz + z), idle))

state = lgsvl.AgentState()
state.transform = spawns[1]
state.transform.position = wp[0].position

p = sim.add_agent("Bob", lgsvl.AgentType.PEDESTRIAN, state)

def on_waypoint(agent, index):
  print("Waypoint {} reached".format(index))

p.on_waypoint_reached(on_waypoint)

# This sends the list of waypoints to the pedestrian. The bool controls whether or not the pedestrian will continue walking (default false)
p.follow(wp, True)

input("Press Enter to walk in circle")

sim.run()
