#!/usr/bin/env python3
#
# Copyright (c) 2019 LG Electronics, Inc.
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
# This block creates the list of waypoints that the NPC will follow
# Each waypoint is an position vector paired with the speed that the NPC will drive to it
waypoints = []
z_max = 4
x_delta = 12
for i in range(20):
  speed = 6 if i % 2 == 0 else 12
  px = (i + 1) * x_delta
  pz = z_max * (-1 if i % 2 == 0 else 1)

  wp = lgsvl.DriveWaypoint(lgsvl.Vector(sx - px, sy, sz - pz), speed)
  waypoints.append(wp)

# When the NPC is within 1m of the waypoint, this will be called
def on_waypoint(agent, index):
  print("waypoint {} reached".format(index))

# The above function needs to be added to the list of callbacks for the NPC
npc.on_waypoint_reached(on_waypoint)

# The NPC needs to be given the list of waypoints. 
# A bool can be passed as the 2nd argument that controls whether or not the NPC loops over the waypoints (default false)
npc.follow(waypoints)

input("Press Enter to run")

sim.run()
