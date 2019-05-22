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

MAX_EGO_SPEED = 20 # (72 km/h, 45 mph)
MAX_POV_SPEED = 17.778 # (64 km/h, 40 mph)
SPEED_VARIANCE = 4
TIME_LIMIT = 30
TIME_DELAY = 4

print("EOV_C_45_40 - ", end = '')

sim = lgsvl.Simulator(os.environ.get("SIMULATOR_HOST", "127.0.0.1"), 8181)
if sim.current_scene == "SimpleMap":
    sim.reset()
else:
    sim.load("SimpleMap")

# spawn EGO in the 2nd to right lane
egoState = lgsvl.AgentState()
# A point close to the desired lane was found in Editor. This method returns the position and orientation of the closest lane to the point.
egoState.transform = sim.map_point_on_lane(lgsvl.Vector(-57.4, 0.15, -96.9))
ego = sim.add_agent("XE_Rigged-apollo_3_5", lgsvl.AgentType.EGO, egoState)

# enable sensors required for Apollo 3.5
sensors = ego.get_sensors()
for s in sensors:
    if s.name in ['velodyne', 'Main Camera', 'Telephoto Camera', 'GPS', 'IMU']:
        s.enabled = True

ego.connect_bridge(os.environ.get("BRIDGE_HOST", "127.0.0.1"), 9090)

POVWaypoints = []
laneX = -89.17999 # If the following waypoints are turned into a lane in Unity editor, this is the Transform Position of the lane
laneY = 0.15
laneZ = -85.25668
POVWaypoints.append(lgsvl.DriveWaypoint(lgsvl.Vector(laneX - 10.854, laneY, laneZ + 30.484), MAX_POV_SPEED))
POVWaypoints.append(lgsvl.DriveWaypoint(lgsvl.Vector(laneX - 10.764, laneY, laneZ + 23.5), MAX_POV_SPEED))
POVWaypoints.append(lgsvl.DriveWaypoint(lgsvl.Vector(laneX - 10.527, laneY, laneZ + 18.2), MAX_POV_SPEED))
POVWaypoints.append(lgsvl.DriveWaypoint(lgsvl.Vector(laneX - 9.79, laneY, laneZ + 13.27667), MAX_POV_SPEED))
POVWaypoints.append(lgsvl.DriveWaypoint(lgsvl.Vector(laneX - 8.802, laneY, laneZ + 9.015), MAX_POV_SPEED))
POVWaypoints.append(lgsvl.DriveWaypoint(lgsvl.Vector(laneX - 7.209, laneY, laneZ + 4.340), MAX_POV_SPEED))
POVWaypoints.append(lgsvl.DriveWaypoint(lgsvl.Vector(laneX - 5.393, laneY, laneZ + 0.969), MAX_POV_SPEED))
POVWaypoints.append(lgsvl.DriveWaypoint(lgsvl.Vector(laneX - 2.91, laneY, laneZ - 2.243), MAX_POV_SPEED))
POVWaypoints.append(lgsvl.DriveWaypoint(lgsvl.Vector(laneX + 0.001, laneY, laneZ - 5.157), MAX_POV_SPEED))
POVWaypoints.append(lgsvl.DriveWaypoint(lgsvl.Vector(laneX + 4.014, laneY, laneZ - 7.823), MAX_POV_SPEED))
POVWaypoints.append(lgsvl.DriveWaypoint(lgsvl.Vector(laneX + 8.385, laneY, laneZ - 9.634), MAX_POV_SPEED))
POVWaypoints.append(lgsvl.DriveWaypoint(lgsvl.Vector(laneX + 12.7, laneY, laneZ - 11.033), MAX_POV_SPEED))
POVWaypoints.append(lgsvl.DriveWaypoint(lgsvl.Vector(laneX + 17.273, laneY, laneZ - 12.167), MAX_POV_SPEED))
POVWaypoints.append(lgsvl.DriveWaypoint(lgsvl.Vector(laneX + 23.956, laneY, laneZ - 13.266), MAX_POV_SPEED))
POVWaypoints.append(lgsvl.DriveWaypoint(lgsvl.Vector(laneX + 30.928, laneY, laneZ - 14.0), MAX_POV_SPEED))

POVState = lgsvl.AgentState()
POVState.transform.position = lgsvl.Vector(laneX - 10.854, laneY, laneZ + 30.484)
POVState.transform.rotation = lgsvl.Vector(0, -190, 0)
POV = sim.add_agent("Sedan", lgsvl.AgentType.NPC, POVState)

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