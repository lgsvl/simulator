#!/usr/bin/env python3
#
# Copyright (c) 2019 LG Electronics, Inc.
#
# This software contains code licensed as described in LICENSE.
#

# See MOTL_Simp_15.py for a commented script

import os
import lgsvl
import sys
import time
import evaluator

MAX_SPEED = 11.111 # (40 km/h, 25 mph)
SPEED_VARIANCE = 4
TIME_LIMIT = 30 # seconds

print("MOTL_Simp_25 - ", end = '')

sim = lgsvl.Simulator(os.environ.get("SIMULATOR_HOST", "127.0.0.1"), 8181)
if sim.current_scene == "SanFrancisco":
    sim.reset()
else:
    sim.load("SanFrancisco")

# spawn EGO in the 2nd to right lane
egoState = lgsvl.AgentState()
# A point close to the desired lane was found in Editor. This method returns the position and orientation of the closest lane to the point.
egoState.transform = sim.map_point_on_lane(lgsvl.Vector(218, 9.9, 4.3))
egoX = egoState.position.x
egoY = egoState.position.y
egoZ = egoState.position.z
ego = sim.add_agent("XE_Rigged-apollo_3_5", lgsvl.AgentType.EGO, egoState)

# enable sensors required for Apollo 3.5
sensors = ego.get_sensors()
for s in sensors:
    if s.name in ['velodyne', 'Main Camera', 'Telephoto Camera', 'GPS', 'IMU']:
        s.enabled = True

ego.connect_bridge(os.environ.get("BRIDGE_HOST", "127.0.0.1"), 9090)

POVState = lgsvl.AgentState()
POVState.transform = sim.map_point_on_lane(lgsvl.Vector(egoX - 4.55 - 12, egoY, egoZ + 3.6))
POV = sim.add_agent("Sedan", lgsvl.AgentType.NPC, POVState)

def on_collision(agent1, agent2, contact):
    raise evaluator.TestException("{} collided with {}".format(agent1, agent2))

ego.on_collision(on_collision)
POV.on_collision(on_collision)

try:
    t0 = time.time()
    while True:
        egoCurrentState = ego.state
        if egoCurrentState.speed > MAX_SPEED + SPEED_VARIANCE:
            raise evaluator.TestException("Ego speed exceeded limit, {} > {} m/s".format(egoCurrentState.speed, MAX_SPEED + SPEED_VARIANCE))

        POVCurrentState = POV.state
        if POVCurrentState.speed > 0 + SPEED_VARIANCE:
            raise evaluator.TestException("POV1 speed exceeded limit, {} > {} m/s".format(POVCurrentState.speed, 0 + SPEED_VARIANCE))

        sim.run(0.5)

        if time.time() - t0 > TIME_LIMIT:
            break
except evaluator.TestException as e:
    print("FAILED: " + repr(e))
    exit()

parkingZoneBeginning = sim.map_point_on_lane(lgsvl.Vector(POVState.position.x - 2.3, POVState.position.y, POVState.position.z))
parkingZoneEnd = sim.map_point_on_lane(lgsvl.Vector(POVState.position.x - 2.3 - 24, POVState.position.y, POVState.position.z))

finalEgoState = ego.state
if not evaluator.right_lane_check(sim, finalEgoState.position):
    print("FAILED: Ego did not change lanes")
elif not evaluator.in_parking_zone(parkingZoneBeginning.position, parkingZoneEnd.position, finalEgoState.position):
    print("FAILED: Ego did not stop in parking zone")
elif finalEgoState.speed > 0.2:
    print("FAILED: Ego did not park")
else:
    print("PASSED")