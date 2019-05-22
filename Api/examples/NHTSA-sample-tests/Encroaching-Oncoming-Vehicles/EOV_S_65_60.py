#!/usr/bin/env python3
#
# Copyright (c) 2019 LG Electronics, Inc.
#
# This software contains code licensed as described in LICENSE.
#

# See EOV_C_25_20.py for a commented script

import os
import lgsvl
import sys
import time
import evaluator

MAX_EGO_SPEED = 29.167 # (105 km/h, 65 mph)
MAX_POV_SPEED = 26.667 # (96 km/h, 60 mph)
INITIAL_HEADWAY = 105
SPEED_VARIANCE = 4
TIME_LIMIT = 30
TIME_DELAY = 5

print("EOV_S_65_60 - ", end = '')

sim = lgsvl.Simulator(os.environ.get("SIMULATOR_HOST", "127.0.0.1"), 8181)
if sim.current_scene == "SanFrancisco":
    sim.reset()
else:
    sim.load("SanFrancisco")

# spawn EGO in the 2nd to right lane
egoState = lgsvl.AgentState()
# A point close to the desired lane was found in Editor. This method returns the position and orientation of the closest lane to the point.
egoState.transform = sim.map_point_on_lane(lgsvl.Vector(218, 9.9, 4.3))
ego = sim.add_agent("XE_Rigged-apollo_3_5", lgsvl.AgentType.EGO, egoState)

# enable sensors required for Apollo 3.5
sensors = ego.get_sensors()
for s in sensors:
    if s.name in ['velodyne', 'Main Camera', 'Telephoto Camera', 'GPS', 'IMU']:
        s.enabled = True

ego.connect_bridge(os.environ.get("BRIDGE_HOST", "127.0.0.1"), 9090)

finalPOVWaypointPosition = lgsvl.Vector(218, 9.9, 2.207)

POVState = lgsvl.AgentState()
POVState.transform.position = lgsvl.Vector(finalPOVWaypointPosition.x - 4.5 - INITIAL_HEADWAY, finalPOVWaypointPosition.y, finalPOVWaypointPosition.z)
POVState.transform.rotation = lgsvl.Vector(0, 90, 0)
POV = sim.add_agent("Sedan", lgsvl.AgentType.NPC, POVState)

POVWaypoints = []
POVWaypoints.append(lgsvl.DriveWaypoint(finalPOVWaypointPosition, MAX_POV_SPEED))

def on_collision(agent1, agent2, contact):
    raise evaluator.TestException("Ego collided with {}".format(agent2))

ego.on_collision(on_collision)
POV.on_collision(on_collision)

try:
    t0 = time.time()
    sim.run(TIME_DELAY)
    POV.follow(POVWaypoints)

    while True:
        egoCurrentState = ego.state
        if egoCurrentState.speed > MAX_EGO_SPEED + SPEED_VARIANCE:
            raise evaluator.TestException("Ego speed exceeded limit, {} > {} m/s".format(egoCurrentState.speed, MAX_EGO_SPEED + SPEED_VARIANCE))

        POVCurrentState = POV.state
        if POVCurrentState.speed > MAX_EGO_SPEED + SPEED_VARIANCE:
            raise evaluator.TestException("POV1 speed exceeded limit, {} > {} m/s".format(POVCurrentState.speed, MAX_POV_SPEED + SPEED_VARIANCE))

        sim.run(0.5)

        if time.time() - t0 > TIME_LIMIT:
            break
except evaluator.TestException as e:
    print("FAILED: " + repr(e))
    exit()

print("PASSED")