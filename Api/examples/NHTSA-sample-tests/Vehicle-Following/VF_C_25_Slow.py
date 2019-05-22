#!/usr/bin/env python3
#
# Copyright (c) 2019 LG Electronics, Inc.
#
# This software contains code licensed as described in LICENSE.
#

# This scenario simulates a scenario where the EGO approaches a vehicle travelling at a slower speed.
# The speed limit of the scenario may require the HD map or Apollo's planning configuration to be editted which is out of the scope of this script.

# SIMULATOR_HOST and BRIDGE_HOST environment variables need to be set. The default for both is localhost.
# The scenario assumes that the EGO's destination is ahead in the right lane.

# POV = Principal Other Vehicle (NPC)

import os
import lgsvl
import sys
import time
import evaluator

MAX_EGO_SPEED = 11.18 # (40 km/h, 25 mph)
SPEED_VARIANCE = 4 # Without real physics, the calculation for a rigidbody's velocity is very imprecise
MAX_POV_SPEED = 8.94 # (32 km/h, 20 mph)
MAX_POV_ROTATION = 5 #deg/s
TIME_LIMIT = 45 # seconds
TIME_DELAY = 3 # The EGO starts moving before the POV to allow it to catch up
MAX_FOLLOWING_DISTANCE = 10 # The maximum distance the EGO should be from the POV

print("VF_C_25_Slow - ", end = '')

sim = lgsvl.Simulator(os.environ.get("SIMULATOR_HOST", "127.0.0.1"), 8181)
if sim.current_scene == "SanFrancisco":
    sim.reset()
else:
    sim.load("SanFrancisco")

# spawn EGO in the 2nd to right lane
egoState = lgsvl.AgentState()
# A point close to the desired lane was found in Editor. This method returns the position and orientation of the closest lane to the point.
egoState.transform = sim.map_point_on_lane(lgsvl.Vector(3716.48, 79.8, -404.05))
ego = sim.add_agent("XE_Rigged-apollo_3_5", lgsvl.AgentType.EGO, egoState)

# enable sensors required for Apollo 3.5
sensors = ego.get_sensors()
for s in sensors:
    if s.name in ['velodyne', 'Main Camera', 'Telephoto Camera', 'GPS', 'IMU']:
        s.enabled = True

ego.connect_bridge(os.environ.get("BRIDGE_HOST", "127.0.0.1"), 9090)

# The POV starts about 30m ahead of the EGO
POVState = lgsvl.AgentState()
POVState.transform = sim.map_point_on_lane(lgsvl.Vector(3744.26, 81.44, -393.46))
POV = sim.add_agent("Sedan", lgsvl.AgentType.NPC, POVState)

# Any collision results in a failed test
def on_collision(agent1, agent2, contact):
    raise evaluator.TestException("Ego collided with {}".format(agent2))

ego.on_collision(on_collision)
POV.on_collision(on_collision)

try:
    t0 = time.time()
    sim.run(TIME_DELAY) # The EGO should start moving first
    POV.follow_closest_lane(True, MAX_POV_SPEED, False)

    while True:
        # The following checks happen every 0.5 seconds
        sim.run(0.5)

        # The EGO should stay at its speed limit
        egoCurrentState = ego.state
        if egoCurrentState.speed > MAX_EGO_SPEED + SPEED_VARIANCE:
            raise evaluator.TestException("Ego speed exceeded limit, {} > {} m/s".format(egoCurrentState.speed, MAX_EGO_SPEED + SPEED_VARIANCE))

        # The POV should stay at its speed limit for both linear speed and rotational speed
        POVCurrentState = POV.state
        if POVCurrentState.speed > MAX_POV_SPEED + SPEED_VARIANCE:
            raise evaluator.TestException("POV speed exceeded limit, {} > {} m/s".format(POVCurrentState.speed, MAX_POV_SPEED + SPEED_VARIANCE))
        if POVCurrentState.angular_velocity.y > MAX_POV_ROTATION:
            raise evaluator.TestException("POV angular rotation exceeded limit, {} > {} deg/s".format(POVCurrentState.angular_velocity, MAX_POV_ROTATION))

        if time.time() - t0 > TIME_LIMIT:
            break
except evaluator.TestException as e:
    print("FAILED: " + repr(e))
    exit()

# The EGO should follow the POV within the following distance
separation = evaluator.separation(egoCurrentState.position, POVCurrentState.position)
if separation > MAX_FOLLOWING_DISTANCE:
    print("FAILED: EGO following distance was not maintained, {} > {}".format(separation, MAX_FOLLOWING_DISTANCE))
else:
    print("PASSED")