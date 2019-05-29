#
# Copyright (c) 2019 LG Electronics, Inc.
#
# This software contains code licensed as described in LICENSE.
#

import unittest

import lgsvl
from .common import SimConnection, spawnState

# TODO add tests for collisions between NPCs, EGO & obstacles

class TestCollisions(unittest.TestCase):
    def test_ego_collision(self): # Check that a collision between Ego and NPC is reported
        with SimConnection() as sim:
            mover, bus = self.setup_collision(sim, "XE_Rigged-apollo", lgsvl.AgentType.EGO, "SchoolBus", lgsvl.AgentType.NPC)
            collisions = []

            def on_collision(agent1, agent2, contact):
                collisions.append([agent1, agent2, contact])
                sim.stop()
            
            mover.on_collision(on_collision)
            bus.on_collision(on_collision)

            sim.run(15.0)

            self.assertGreater(len(collisions), 0)
            self.assertInBetween(collisions[0][2], collisions[0][0].state.position, collisions[0][1].state.position, "Ego Collision")

            self.assertTrue(collisions[0][0].name == "XE_Rigged-apollo" or collisions[0][1].name == "XE_Rigged-apollo")
            self.assertTrue(True)

    def test_sim_stop(self): # Check that sim.stop works properly
        with SimConnection() as sim:
            mover, bus = self.setup_collision(sim, "XE_Rigged-apollo", lgsvl.AgentType.EGO, "SchoolBus", lgsvl.AgentType.NPC)
            collisions = []

            def on_collision(agent1, agent2, contact):
                collisions.append([agent1, agent2, contact])
                sim.stop()
            
            mover.on_collision(on_collision)
            bus.on_collision(on_collision)

            sim.run(15.0)

            self.assertLess(sim.current_time, 15.5)

    @unittest.skip("Peds limited to NavMesh, activate this when crosswalks added")
    def test_ped_collision(self): # Check if a collision between EGO and pedestrian is reported
        with SimConnection() as sim:
            ego, ped = self.setup_collision(sim, "XE_Rigged-apollo", lgsvl.AgentType.EGO, "Howard", lgsvl.AgentType.PEDESTRIAN)
            self.assertTrue(isinstance(ego, lgsvl.EgoVehicle))
            self.assertTrue(isinstance(ped, lgsvl.Pedestrian))
            collisions = []

            def on_collision(agent1, agent2, contact):
                collisions.append([agent1, agent2, contact])
                sim.stop()
            ped.on_collision(on_collision)
            ego.on_collision(on_collision)

            sim.run(15)
            self.assertGreater(len(collisions), 0)
            self.assertInBetween(collisions[0][2], collisions[0][0].state.position, collisions[0][1].state.position, "Ped Collision")
            self.assertTrue(collisions[0][0].name == "XE_Rigged-apollo" or collisions[0][1].name == "XE_Rigged-apollo")

    @unittest.skip("Peds limited to NavMesh, activate this when crosswalks added")
    def test_ped_npc_collisions(self): # Check that collision between NPC and Pedestrian is reported
        with SimConnection() as sim:
            state = spawnState(sim)
            state.position.y += 10
            bus  = sim.add_agent("SchoolBus", lgsvl.AgentType.NPC, state)

            state = spawnState(sim)
            ped = sim.add_agent("Bob", lgsvl.AgentType.PEDESTRIAN, state)
            collisions = []
            def on_collision(agent1, agent2, contact):
                collisions.append([agent1, agent2, contact])
                sim.stop()
            ped.on_collision(on_collision)
            bus.on_collision(on_collision)
            sim.run(15)
            
            self.assertGreater(len(collisions), 0)
            self.assertInBetween(collisions[0][2], collisions[0][0].state.position, collisions[0][1].state.position, "Ped/NPC Collision")
            self.assertTrue(collisions[0][0].name == "Bob" or collisions[0][1].name == "Bob")

    @unittest.skip("NPCs ignore collisions with other NPCs, activate this when NPCs use real physics")
    def test_npc_collision(self): # Check that collision between NPC and NPC is reported
        with SimConnection() as sim:
            state = spawnState(sim)
            state.position.x += 10
            sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, state)
            jeep, bus = self.setup_collision(sim, "Jeep", lgsvl.AgentType.NPC, "SchoolBus", lgsvl.AgentType.NPC)
            collisions = []

            def on_collision(agent1, agent2, contact):
                collisions.append([agent1, agent2, contact])
                sim.stop()
            
            jeep.on_collision(on_collision)
            bus.on_collision(on_collision)

            sim.run(15.0)

            self.assertGreater(len(collisions), 0)
            self.assertInBetween(collisions[0][2], collisions[0][0].state.position, collisions[0][1].state.position, "NPC Collision")
            self.assertTrue(collisions[0][0].name == "Jeep" or collisions[0][1].name == "Jeep")
            self.assertTrue(collisions[0][0].name == "SchoolBus" or collisions[0][1].name == "SchoolBus")

    def test_wall_collision(self): # Check that an EGO collision with a wall is reported properly
        with SimConnection() as sim:
            state = spawnState(sim)
            state.position.z += 15
            state.velocity.x += -50
            ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, state)
            collisions = []

            def on_collision(agent1, agent2, contact):
                collisions.append([agent1, agent2, contact])
                sim.stop()
            
            ego.on_collision(on_collision)

            sim.run(15)

            self.assertGreater(len(collisions), 0)
            if collisions[0][0] is None:
                self.assertTrue(collisions[0][1].name == "XE_Rigged-apollo")
            elif collisions[0][1] is None:
                self.assertTrue(collisions[0][0].name == "XE_Rigged-apollo")
            else:
                self.fail("Collision not with object")

    def setup_collision(self, sim, mover_name, agent_type, still_name, still_type): 
        # Creates 2 agents, the mover is created with a forward velocity
        # still is rotated 90 degree in and in front of the mover
        state = spawnState(sim)
        state.velocity = lgsvl.Vector(-50, 0, 0)
        mover = sim.add_agent(mover_name, agent_type, state)

        # school bus, 20m ahead, perpendicular to road, stopped

        state = lgsvl.AgentState()
        state.transform = sim.get_spawn()[0]
        state.transform.position.x -= 20.0
        state.transform.rotation.y = 0.0
        still = sim.add_agent(still_name, still_type, state)

        return mover, still

    def assertInBetween(self, position, a, b, msg): # Tests that at least one component of the input position vector is between the a and b vectors
        xmid = (a.x+b.x)/2
        xdiff = abs(a.x-xmid)
        xmin = xmid-xdiff
        xmax = xmid+xdiff
        
        ymid = (a.y+b.y)/2
        ydiff = abs(a.y-ymid)
        ymin = ymid-ydiff
        ymax = ymid+ydiff

        zmid = (a.z+b.z)/2
        zdiff = abs(a.z-zmid)
        zmin = zmid-zdiff
        zmax = zmid+zdiff

        validCollision = False
        validCollision |= (position.x <= xmax and position.x >= xmin)
        validCollision |= (position.y <= ymax and position.y >= ymin)
        validCollision |= (position.z <= zmax and position.z >= zmin)
        self.assertTrue(validCollision, msg)
