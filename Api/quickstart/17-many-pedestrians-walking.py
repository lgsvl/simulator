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

sx = state.transform.position.x
sy = state.transform.position.y
sz = state.transform.position.z

names = ["Bob", "Entrepreneur", "Howard", "Johnny", "Pamela", "Presley", "Robin", "Stephen"]

for i in range(20*8):
  px = sx + 5 - (1.0 * (i//8))
  pz = sz + 6 + (1.0 * (i % 8))

  wp = [ lgsvl.WalkWaypoint(lgsvl.Vector(px, sy, pz), 0),
         lgsvl.WalkWaypoint(lgsvl.Vector(px - 10, sy, pz), 0),
       ]

  state = lgsvl.AgentState()
  state.transform = spawns[1]
  state.transform.position.x = px
  state.transform.position.z = pz
  name = random.choice(names)

  p = sim.add_agent(name, lgsvl.AgentType.PEDESTRIAN, state)
  p.follow(wp, True)

input("enter to start")

sim.run()
