#!/usr/bin/env python3
#
# Copyright (c) 2019 LG Electronics, Inc.
#
# This software contains code licensed as described in LICENSE.
#

# This scenario simulates a situation where a distracted driver has crossed the double yellow lines and is on a collision course with the EGO.
# The speed limit of the scenario may require the HD map or Apollo's planning configuration to be editted which is out of the scope of this script.

# SIMULATOR_HOST and BRIDGE_HOST environment variables need to be set. The default for both is localhost.
# The scenario assumes that the EGO's destination is ahead in the right lane.

# POV = Principal Other Vehicle (NPC)

import os
import lgsvl
import sys
import time
import evaluator

MAX_EGO_SPEED = 11.111 # (40 km/h, 25 mph)
MAX_POV_SPEED = 8.889 # (32 km/h, 20 mph)
SPEED_VARIANCE = 4 # Without real physics, the calculation for a rigidbody's velocity is very imprecise
TIME_LIMIT = 30
TIME_DELAY = 3 # The EGO should start moving before the POV

print("EOV_C_25_20 - ", end = '')

sim = lgsvl.Simulator(os.environ.get("SIMULATOR_HOST", "127.0.0.1"), 8181)
if sim.current_scene == "SimpleMap":
    sim.reset()
else:
    sim.load("SimpleMap")

# spawn EGO in the 2nd to right lane
egoState = lgsvl.AgentState()
# A point close to the desired lane was found in Editor. This method returns the position and orientation of the closest lane to the point.
egoState.transform = sim.map_point_on_lane(lgsvl.Vector(-77.09, 0.15, -94.05))
ego = sim.add_agent("XE_Rigged-apollo_3_5", lgsvl.AgentType.EGO, egoState)

# enable sensors required for Apollo 3.5
sensors = ego.get_sensors()
for s in sensors:
    if s.name in ['velodyne', 'Main Camera', 'Telephoto Camera', 'GPS', 'IMU']:
        s.enabled = True

ego.connect_bridge(os.environ.get("BRIDGE_HOST", "127.0.0.1"), 9090)

# The POV is not following a lane so waypoints are needed to define its path
POVWaypoints = []
laneX = -89.17999 # If the following waypoints are turned into a lane in Unity editor, this is the Transform Position of the lane
laneY = 0.15
laneZ = -85.25668
POVWaypoints.append(lgsvl.DriveWaypoint(lgsvl.Vector(laneX - 9.79, laneY, laneZ + 13.27667), MAX_POV_SPEED))
POVWaypoints.append(lgsvl.DriveWaypoint(lgsvl.Vector(laneX - 8.802, laneY, laneZ + 9.015), MAX_POV_SPEED))
POVWaypoints.append(lgsvl.DriveWaypoint(lgsvl.Vector(laneX - 7.209, laneY, laneZ + 4.340), MAX_POV_SPEED))
POVWaypoints.append(lgsvl.DriveWaypoint(lgsvl.Vector(laneX - 5.393, laneY, laneZ + 0.969), MAX_POV_SPEED))
POVWaypoints.append(lgsvl.DriveWaypoint(lgsvl.Vector(laneX - 2.91, laneY, laneZ - 2.243), MAX_POV_SPEED))
POVWaypoints.append(lgsvl.DriveWaypoint(lgsvl.Vector(laneX + 0.001, laneY, laneZ - 5.157), MAX_POV_SPEED))
POVWaypoints.append(lgsvl.DriveWaypoint(lgsvl.Vector(laneX + 4.014, laneY, laneZ - 7.823), MAX_POV_SPEED))
POVWaypoints.append(lgsvl.DriveWaypoint(lgsvl.Vector(laneX + 8.385, laneY, laneZ - 9.634), MAX_POV_SPEED))
POVWaypoints.append(lgsvl.DriveWaypoint(lgsvl.Vector(laneX + 12.7, laneY, laneZ - 11.033), MAX_POV_SPEED))

POVState = lgsvl.AgentState()
POVState.transform.position = lgsvl.Vector(laneX - 9.79, laneY, laneZ + 13.27667)
POVState.transform.rotation = lgsvl.Vector(0, -200, 0)
POV = sim.add_agent("Sedan", lgsvl.AgentType.NPC, POVState)

# Any collision results in a failed test
def on_collision(agent1, agent2, contact):
    raise evaluator.TestException("Ego collided with {}".format(agent2))

ego.on_collision(on_collision)
POV.on_collision(on_collision)

try:
    t0 = time.time()
    sim.run(TIME_DELAY)
    POV.follow(POVWaypoints)

    while True:
        # The EGO should stay at it speed limit
        egoCurrentState = ego.state
        if egoCurrentState.speed > MAX_EGO_SPEED + SPEED_VARIANCE:
            raise evaluator.TestException("Ego speed exceeded limit, {} > {} m/s".format(egoCurrentState.speed, MAX_EGO_SPEED + SPEED_VARIANCE))

        # The POV should stay at its speed limit
        POVCurrentState = POV.state
        if POVCurrentState.speed > MAX_EGO_SPEED + SPEED_VARIANCE:
            raise evaluator.TestException("POV1 speed exceeded limit, {} > {} m/s".format(POVCurrentState.speed, MAX_POV_SPEED + SPEED_VARIANCE))

        # The above checks are made every 0.5 seconds
        sim.run(0.5)

        if time.time() - t0 > TIME_LIMIT:
            break
except evaluator.TestException as e:
    print("FAILED: " + repr(e))
    exit()

print("PASSED")