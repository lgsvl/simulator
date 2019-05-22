#!/usr/bin/env python3
#
# Copyright (c) 2019 LG Electronics, Inc.
#
# This software contains code licensed as described in LICENSE.
#

# The speed limit of the scenario may require the HD map or Apollo's planning configuration to be editted which is out of the scope of this script.

# SIMULATOR_HOST and BRIDGE_HOST environment variables need to be set. The default for both is localhost.
# The scenario assumes that the EGO's destination is ahead in the right lane.

# POV = Principal Other Vehicle (NPC)

import os
import lgsvl
import sys
import time
import evaluator

MAX_SPEED = 6.667 # (24 km/h, 15 mph)
SPEED_VARIANCE = 4 # Without real physics, the calculation for a rigidbody's velocity is very imprecise
TIME_LIMIT = 30 # seconds

print("MOTL_Simp_15 - ", end = '')

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

# The POV is created in the right lane 12 m ahead of the EGO
POVState = lgsvl.AgentState()
POVState.transform = sim.map_point_on_lane(lgsvl.Vector(egoX - 4.55 - 12, egoY, egoZ + 3.6))
# The EGO is 4.5m long and the Sedan is 4.6m long. 4.55 is half the length of an EGO and Sedan
POV = sim.add_agent("Sedan", lgsvl.AgentType.NPC, POVState)

def on_collision(agent1, agent2, contact):
    raise evaluator.TestException("{} collided with {}".format(agent1, agent2))

ego.on_collision(on_collision)
POV.on_collision(on_collision)

try:
    t0 = time.time()
    while True:
        # The EGO should stay at or below the speed limit
        egoCurrentState = ego.state
        if egoCurrentState.speed > MAX_SPEED + SPEED_VARIANCE:
            raise evaluator.TestException("Ego speed exceeded limit, {} > {} m/s".format(egoCurrentState.speed, MAX_SPEED + SPEED_VARIANCE))

        # The POV should not move
        POVCurrentState = POV.state
        if POVCurrentState.speed > 0 + SPEED_VARIANCE:
            raise evaluator.TestException("POV speed exceeded limit, {} > {} m/s".format(POVCurrentState.speed, 0 + SPEED_VARIANCE))

        # The above checks are made every 0.5 seconds
        sim.run(0.5)

        if time.time() - t0 > TIME_LIMIT:
            break
except evaluator.TestException as e:
    print("FAILED: " + repr(e))
    exit()

# These 2 positions are the beginning and end of the parking zone
parkingZoneBeginning = sim.map_point_on_lane(lgsvl.Vector(POVState.position.x - 2.3, POVState.position.y, POVState.position.z))
parkingZoneEnd = sim.map_point_on_lane(lgsvl.Vector(POVState.position.x - 2.3 - 24, POVState.position.y, POVState.position.z))

finalEgoState = ego.state
# The EGO should end in the right lane
if not evaluator.right_lane_check(sim, finalEgoState.position):
    print("FAILED: Ego did not change lanes")
# The EGO should end within the parking zone
elif not evaluator.in_parking_zone(parkingZoneBeginning.position, parkingZoneEnd.position, finalEgoState.position):
    print("FAILED: Ego did not stop in parking zone")
# The EGO should end in Park
elif finalEgoState.speed > 0.2:
    print("FAILED: Ego did not park")
else:
    print("PASSED")