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

MAX_SPEED = 6.667 # (24 km/h, 15 mph)
SPEED_VARIANCE = 4
TIME_LIMIT = 30 # seconds

print("MOTL_Comp_15 - ", end = '')

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

def on_collision(agent1, agent2, contact):
    raise evaluator.TestException("{} collided with {}".format(agent1, agent2))

ego.on_collision(on_collision)

POVList = []

POV1State = lgsvl.AgentState()
POV1State.transform = sim.map_point_on_lane(lgsvl.Vector(egoX - 4.55 - 11, egoY, egoZ + 3.6))
POV1 = sim.add_agent("Sedan", lgsvl.AgentType.NPC, POV1State)
POV1.on_collision(on_collision)
POVList.append(POV1)

POV2State = lgsvl.AgentState()
POV2State.transform = sim.map_point_on_lane(lgsvl.Vector(POV1State.position.x - 4.6 - 24, POV1State.position.y, POV1State.position.z))
POV2 = sim.add_agent("Sedan", lgsvl.AgentType.NPC, POV2State)
POV2.on_collision(on_collision)
POVList.append(POV2)

for i in range(2,28):
    POVState = lgsvl.AgentState()
    POVState.transform = sim.map_point_on_lane(lgsvl.Vector(POVList[i-1].state.position.x - 7, POVList[i-1].state.position.y, POVList[i-1].state.position.z))
    POV = sim.add_agent("Sedan", lgsvl.AgentType.NPC, POVState)
    POV.on_collision(on_collision)
    POVList.append(POV)

try:
    t0 = time.time()
    while True:
        egoCurrentState = ego.state
        if egoCurrentState.speed > MAX_SPEED + SPEED_VARIANCE:
            raise evaluator.TestException("Ego speed exceeded limit, {} > {} m/s".format(egoCurrentState.speed, MAX_SPEED + SPEED_VARIANCE))

        for i in range(len(POVList)):
            POVCurrentState = POVList[i].state
            if POVCurrentState.speed > 0 + SPEED_VARIANCE:
                raise evaluator.TestException("POV{} speed exceeded limit, {} > {} m/s".format(i, POVCurrentState.speed, 0 + SPEED_VARIANCE))

        sim.run(0.5)

        if time.time() - t0 > TIME_LIMIT:
            break
except evaluator.TestException as e:
    print("FAILED: " + repr(e))
    exit()

parkingZoneBeginning = sim.map_point_on_lane(lgsvl.Vector(POV1State.position.x - 2.3, POV1State.position.y, POV1State.position.z))
parkingZoneEnd = sim.map_point_on_lane(lgsvl.Vector(POV1State.position.x - 2.3 - 24, POV1State.position.y, POV1State.position.z))

finalEgoState = ego.state
if not evaluator.right_lane_check(sim, finalEgoState.position):
    print("FAILED: Ego did not change lanes")
elif not evaluator.in_parking_zone(parkingZoneBeginning.position, parkingZoneEnd.position, finalEgoState.position):
    print("FAILED: Ego did not stop in parking zone")
elif finalEgoState.speed > 0.2:
    print("FAILED: Ego did not park")
else:
    print("PASSED")