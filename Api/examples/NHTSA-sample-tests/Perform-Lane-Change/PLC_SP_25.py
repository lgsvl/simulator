#!/usr/bin/env python3
#
# Copyright (c) 2019 LG Electronics, Inc.
#
# This software contains code licensed as described in LICENSE.
#

# See PLC_SN_15.py for a commented script

import os
import lgsvl
import sys
import time
import evaluator

MAX_SPEED = 11.111 # (40 km/h, 25 mph)
SPEED_VARIANCE = 4
TIME_LIMIT = 30 # seconds

print("PLC_SP_25 - ", end = '')

sim = lgsvl.Simulator(os.environ.get("SIMULATOR_HOST", "127.0.0.1"), 8181)
if sim.current_scene == "SanFrancisco":
    sim.reset()
else:
    sim.load("SanFrancisco")

# spawn EGO in the 2nd to right lane
egoState = lgsvl.AgentState()
# A point close to the desired lane was found in Editor. This method returns the position and orientation of the closest lane to the point.
egoState.transform = sim.map_point_on_lane(lgsvl.Vector(1699.6, 88.38, -601.9))
ego = sim.add_agent("XE_Rigged-apollo_3_5", lgsvl.AgentType.EGO, egoState)
egoX = ego.state.position.x
egoY = ego.state.position.y
egoZ = ego.state.position.z

# enable sensors required for Apollo 3.5
sensors = ego.get_sensors()
for s in sensors:
    if s.name in ['velodyne', 'Main Camera', 'Telephoto Camera', 'GPS', 'IMU']:
        s.enabled = True

ego.connect_bridge(os.environ.get("BRIDGE_HOST", "127.0.0.1"), 9090)

POV1State = lgsvl.AgentState()
POV1State.transform = sim.map_point_on_lane(lgsvl.Vector(egoX - 10.77, egoY, egoZ + 2.85))
POV1 = sim.add_agent("Sedan", lgsvl.AgentType.NPC, POV1State)

POV1.follow_closest_lane(True, MAX_SPEED, False)

def on_collision(agent1, agent2, contact):
    raise evaluator.TestException("{} collided with {}".format(agent1, agent2))


ego.on_collision(on_collision)
POV1.on_collision(on_collision)

try:
    t0 = time.time()
    while True:
        egoCurrentState = ego.state
        if egoCurrentState.speed > MAX_SPEED + SPEED_VARIANCE:
            raise evaluator.TestException("Ego speed exceeded limit, {} > {} m/s".format(egoCurrentState.speed, MAX_SPEED + SPEED_VARIANCE))

        POV1CurrentState = POV1.state
        if POV1CurrentState.speed > MAX_SPEED + SPEED_VARIANCE:
            raise evaluator.TestException("POV1 speed exceeded limit, {} > {} m/s".format(POV1CurrentState.speed, MAX_SPEED + SPEED_VARIANCE))

        sim.run(0.5)

        if time.time() - t0 > TIME_LIMIT:
            break
except evaluator.TestException as e:
    print("FAILED: " + repr(e))
    exit()

finalEgoState = ego.state
if evaluator.right_lane_check(sim, finalEgoState.position):
    print("PASSED")
else:
    print("FAILED: Ego did not change lanes")