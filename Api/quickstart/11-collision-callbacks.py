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

spawns = sim.get_spawn()

sx = spawns[0].position.x
sz = spawns[0].position.z
ry = spawns[0].rotation.y

# ego vehicle

state = lgsvl.AgentState()
state.transform = spawns[0]
ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, state)

# school bus, 20m ahead, perpendicular to road, stopped

state = lgsvl.AgentState()
state.transform = spawns[0]
state.transform.position.x = sx - 20.0
state.transform.rotation.y = 0.0
bus = sim.add_agent("SchoolBus", lgsvl.AgentType.NPC, state)

# sedan, 10m ahead, driving forward
state = lgsvl.AgentState()
state.transform = spawns[0]
state.transform.position.x = sx - 10.0
state.transform.rotation.y = ry
sedan = sim.add_agent("Sedan", lgsvl.AgentType.NPC, state)
# Even though the sedan is commanded to follow the lane, obstacle avoidance takes precedence and it will not drive into the bus
sedan.follow_closest_lane(True, 11.1) # 11.1 m/s is ~40 km/h

vehicles = {
  ego: "EGO",
  bus: "SchoolBus",
  sedan: "Sedan",
}

# This function gets called whenever any of the 3 vehicles above collides with anything
def on_collision(agent1, agent2, contact):
  name1 = vehicles[agent1]
  name2 = vehicles[agent2] if agent2 is not None else "OBSTACLE"
  print("{} collided with {} at {}".format(name1, name2, contact))

# The above on_collision function needs to be added to the callback list of each vehicle
ego.on_collision(on_collision)
bus.on_collision(on_collision)
sedan.on_collision(on_collision)

input("Press Enter to run")

# Manually drive into the sedan, bus, or obstacle to get a callback
sim.run()
