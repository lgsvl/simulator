#
# Copyright (c) 2019 LG Electronics, Inc.
#
# This software contains code licensed as described in LICENSE.
#

import unittest
import math
import time

import lgsvl
from .common import SimConnection, cmEqual, mEqual, spawnState

class TestPeds(unittest.TestCase):
    def test_ped_creation(self): # Check if the different types of Peds can be created
        with SimConnection() as sim:
            state=spawnState(sim)
            state.position.x += 5
            sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, state)
            for name in ["Bob", "Entrepreneur", "Howard", "Johnny", \
                "Pamela", "Presley", "Robin", "Stephen", "Zoe"]:
                agent = self.create_ped(sim, name, spawnState(sim))
                cmEqual(self, agent.state.position, sim.get_spawn()[0].position, name)
                self.assertEqual(agent.name, name)
    
    def test_ped_random_walk(self): # Check if pedestrians can walk randomly
        with SimConnection() as sim:
            sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, spawnState(sim))
            state = spawnState(sim)
            state.transform.position.x -= 20
            state.transform.position.z += 10
            spawnPoint = state.transform.position

            bob = self.create_ped(sim, "Bob", state)
            bob.walk_randomly(True)
            sim.run(2)

            randPoint = bob.transform.position
            self.assertNotAlmostEqual(spawnPoint.x, randPoint.x)
            self.assertNotAlmostEqual(spawnPoint.y, randPoint.y)
            self.assertNotAlmostEqual(spawnPoint.z, randPoint.z)

            bob.walk_randomly(False)
            sim.run(2)

            cmEqual(self, randPoint, bob.state.transform.position, "Ped random walk")

    def test_ped_circle_waypoints(self): # Check if pedestrians can follow waypoints
        with SimConnection(60) as sim:
            sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, spawnState(sim))
            state = spawnState(sim)
            sx = state.position.x - 20
            sy = state.position.y
            sz = state.position.z + 10
            radius = 5
            count = 3
            waypointCommands = []
            waypoints = []
            for i in range(count):
                x = radius * math.cos(i * 2 * math.pi / count)
                z = radius * math.sin(i * 2 * math.pi / count)
                idle = 1 if i < count//2 else 0
                waypointCommands.append(lgsvl.WalkWaypoint(lgsvl.Vector(sx + x, sy, sz + z), idle))
                waypoints.append(lgsvl.Vector(sx + x, sy, sz + z))

            state.transform.position = waypoints[0]
            zoe = self.create_ped(sim, "Zoe", state)
            def on_waypoint(agent,index):
                msg = "Waypoint " + str(index)
                mEqual(self, zoe.state.position, waypoints[index], msg)
                if index == len(waypoints)-1:
                    sim.stop()

            zoe.on_waypoint_reached(on_waypoint)
            zoe.follow(waypointCommands, True)
            sim.run()


    def create_ped(self, sim, name, state): # create the specified Pedestrian
        return sim.add_agent(name, lgsvl.AgentType.PEDESTRIAN, state)
