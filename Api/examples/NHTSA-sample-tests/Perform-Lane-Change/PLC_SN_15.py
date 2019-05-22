#!/usr/bin/env python3
#
# Copyright (c) 2019 LG Electronics, Inc.
#
# This software contains code licensed as described in LICENSE.
#

# This scenario simulates a situation where the EGO needs to change lanes in the presence of other traffic
# The speed limit of the scenario may require the HD map or Apollo's planning configuration to be editted which is out of the scope of this script.

# SIMULATOR_HOST and BRIDGE_HOST environment variables need to be set. The default for both is localhost.
# The scenario assumes that the EGO's destination is ahead in the right lane.

# POV = Principal Other Vehicle (NPC)

import os
import lgsvl
import sys
import time
import evaluator

MAX_SPEED = 6.667 # (24 km/h, 15 mph) Max speed of EGO and POVs
SPEED_VARIANCE = 4 # Without real physics, the calculation for a rigidbody's velocity is very imprecise
MAX_POV_SEPARATION = 5+4.6 # The POVs in the right lane should keep a constant distance between them
SEPARATION_VARIANCE = 2 # The allowable variance in the POV separation
TIME_LIMIT = 30 # seconds

print("PLC_SN_15 - ", end = '')

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

# The first POV is positioned such that its rear bumper is 5m in front of the EGO's front bumper in the right lane
POV1State = lgsvl.AgentState()
POV1State.transform = sim.map_point_on_lane(lgsvl.Vector(egoX - 9.27, egoY, egoZ + 2.96))
POV1 = sim.add_agent("Sedan", lgsvl.AgentType.NPC, POV1State)
POV1.follow_closest_lane(True, MAX_SPEED, False)

# The second POV is positioned such that its front bumper is even with the EGO's front bumper in the right lane
POV2State = lgsvl.AgentState()
POV2State.transform = sim.map_point_on_lane(lgsvl.Vector(egoX + 4.29, egoY, egoZ + 3.91))
POV2 = sim.add_agent("Sedan", lgsvl.AgentType.NPC, POV2State)
POV2.follow_closest_lane(True, MAX_SPEED, False)

# The third POV is positioned such that its front bumper is 8m behind the EGO's rear bumper in the same lane as the EGO
POV3State = lgsvl.AgentState()
POV3State.transform = sim.map_point_on_lane(lgsvl.Vector(egoX + 9.03, egoY, egoZ + 0.63))
POV3 = sim.add_agent("Sedan", lgsvl.AgentType.NPC, POV3State)
POV3.follow_closest_lane(True, MAX_SPEED, False)

# Any collision results in a failed test
def on_collision(agent1, agent2, contact):
    raise evaluator.TestException("{} collided with {}".format(agent1, agent2))

ego.on_collision(on_collision)
POV1.on_collision(on_collision)
POV2.on_collision(on_collision)
POV3.on_collision(on_collision)

try:
    t0 = time.time()
    while True:
        # The EGO should not exceed the max specified speed
        egoCurrentState = ego.state
        if egoCurrentState.speed > MAX_SPEED + SPEED_VARIANCE:
            raise evaluator.TestException("Ego speed exceeded limit, {} > {} m/s".format(egoCurrentState.speed, MAX_SPEED + SPEED_VARIANCE))

        POV1CurrentState = POV1.state
        POV2CurrentState = POV2.state
        POV3CurrentState = POV3.state

        # The POVs in the right lane should maintain their starting separation
        POVSeparation = evaluator.separation(POV1CurrentState.position, POV2CurrentState.position)
        if POVSeparation > MAX_POV_SEPARATION + SEPARATION_VARIANCE:
            raise evaluator.TestException("POV1 and POV2 are too far apart: {} > {}".format(POVSeparation, MAX_POV_SEPARATION + SEPARATION_VARIANCE))

        # The POVs should not exceed the speed limit
        if POV1CurrentState.speed > MAX_SPEED + SPEED_VARIANCE:
            raise evaluator.TestException("POV1 speed exceeded limit, {} > {} m/s".format(POV1CurrentState.speed, MAX_SPEED + SPEED_VARIANCE))
        
        if POV2CurrentState.speed > MAX_SPEED + SPEED_VARIANCE:
            raise evaluator.TestException("POV2 speed exceeded limit, {} > {} m/s".format(POV2CurrentState.speed, MAX_SPEED + SPEED_VARIANCE))

        if POV3CurrentState.speed > MAX_SPEED + SPEED_VARIANCE:
            raise evaluator.TestException("POV3 speed exceeded limit, {} > {} m/s".format(POV3CurrentState.speed, MAX_SPEED + SPEED_VARIANCE))

        # The above checks are made every 0.5 seconds.
        sim.run(0.5)

        if time.time() - t0 > TIME_LIMIT:
            break
except evaluator.TestException as e:
    print("FAILED: " + repr(e))
    exit()

# This checks that the EGO actually changed lanes
finalEgoState = ego.state
if evaluator.right_lane_check(sim, finalEgoState.position):
    print("PASSED")
else:
    print("FAILED: Ego did not change lanes")