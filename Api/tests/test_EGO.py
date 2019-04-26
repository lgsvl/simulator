#
# Copyright (c) 2019 LG Electronics, Inc.
#
# This software contains code licensed as described in LICENSE.
#

import unittest
import math

import lgsvl
from .common import SimConnection, spawnState, cmEqual, notAlmostEqual

# TODO add tests for bridge connection

class TestEGO(unittest.TestCase):
    def test_agent_name(self): # Check if EGO Apollo is created
        with SimConnection() as sim:
            agent = self.create_EGO(sim)

        self.assertEqual(agent.name, "XE_Rigged-apollo")

    def test_different_spawns(self): # Check if EGO is spawned in the spawn positions
        with SimConnection() as sim:
            spawns = sim.get_spawn()
            agent = self.create_EGO(sim)
            cmEqual(self, agent.state.position, spawns[0].position, "Spawn Position 0")
            cmEqual(self, agent.state.rotation, spawns[0].rotation, "Spawn Rotation 0")

            state = spawnState(sim)
            state.transform = spawns[1]
            agent2 = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, state)

            cmEqual(self, agent2.state.position, spawns[1].position, "Spawn Position 1")
            cmEqual(self, agent2.state.rotation, spawns[1].rotation, "Spawn Rotation 1")

    def test_agent_velocity(self): # Check EGO velocity
        with SimConnection() as sim:
            state = spawnState(sim)
            agent = self.create_EGO(sim)
            cmEqual(self, agent.state.velocity, state.velocity, "0 Velocity")

            sim.reset()
            state.velocity = lgsvl.Vector(-50, 0, 0)
            agent = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, state)
            
            cmEqual(self, agent.state.velocity, state.velocity, "50 Velocity")

    def test_ego_different_directions(self): # Check that the xyz velocities equate to xyz changes in position
        with SimConnection() as sim:
            state = spawnState(sim)
            state.velocity = lgsvl.Vector(-10,0,0)
            ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, state)
            sim.run(.5)
            self.assertNotAlmostEqual(state.position.x, ego.state.position.x, places=1)
            self.assertAlmostEqual(state.position.y, ego.state.position.y, places=1)
            self.assertAlmostEqual(state.position.z, ego.state.position.z, places=1)
            sim.remove_agent(ego)
            state.velocity = lgsvl.Vector(0,10,0)
            ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, state)
            sim.run(.5)
            self.assertNotAlmostEqual(state.position.y, ego.state.position.y, places=1)
            self.assertAlmostEqual(state.position.x, ego.state.position.x, places=1)
            self.assertAlmostEqual(state.position.z, ego.state.position.z, places=1)
            sim.remove_agent(ego)
            state.velocity = lgsvl.Vector(0,0,-10)
            ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, state)
            sim.run(.5)
            self.assertNotAlmostEqual(state.position.z, ego.state.position.z, places=1)
            self.assertAlmostEqual(state.position.y, ego.state.position.y, places=1)
            self.assertAlmostEqual(state.position.x, ego.state.position.x, places=1)

    def test_speed(self): # check that speed returns a reasonable number
        with SimConnection() as sim:
            state = spawnState(sim)
            state.velocity = lgsvl.Vector(-10,10,10)
            ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, state)
            self.assertAlmostEqual(ego.state.speed, math.sqrt(300))

    def test_rotation_on_highway_ramp(self): # Check that vehicle is rotated when spawned on the highway ramp
        with SimConnection() as sim:
            state = spawnState(sim)
            state.transform.position = lgsvl.Vector(100.4229, 15.67488, -469.6401)
            ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, state)
            self.assertAlmostEqual(ego.state.rotation.z, state.rotation.z)
            sim.run(0.5)
            self.assertAlmostEqual(ego.state.rotation.z, 356, delta=0.5)

    def test_ego_steering(self): # Check that a steering command can be given to an EGO vehicle, and the car turns
        with SimConnection() as sim:
            ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, spawnState(sim))
            control = lgsvl.VehicleControl()
            control.throttle = 0.3
            control.steering = -1.0
            ego.apply_control(control, True)
            initialRotation = ego.state.rotation
            sim.run(1)
            finalRotation = ego.state.rotation
            self.assertNotAlmostEqual(initialRotation.y, finalRotation.y)

    def test_ego_throttle(self): # Check that a throttle command can be given to an EGO vehicle, and the car accelerates
        with SimConnection() as sim:
            ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, spawnState(sim))
            control = lgsvl.VehicleControl()
            control.throttle = 0.5
            ego.apply_control(control, True)
            initialSpeed = ego.state.speed
            sim.run(2)
            finalSpeed = ego.state.speed
            self.assertGreater(finalSpeed, initialSpeed)

    def test_ego_braking(self): # Check that a brake command can be given to an EGO vehicle, and the car stops sooner than without brakes
        with SimConnection() as sim:
            state = spawnState(sim)
            ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, state)
            control = lgsvl.VehicleControl()
            control.throttle = 1
            ego.apply_control(control, True)
            sim.run(1)
            control = lgsvl.VehicleControl()
            ego.apply_control(control,True)
            sim.run(3)
            noBrakePosition = ego.state.position.x
            
            sim.reset()
            ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, state)
            control = lgsvl.VehicleControl()
            control.throttle = 1
            ego.apply_control(control, True)
            sim.run(1)
            control = lgsvl.VehicleControl()
            control.braking = 1
            ego.apply_control(control, True)
            sim.run(3)
            self.assertGreater(ego.state.position.x, noBrakePosition)

    def test_ego_handbrake(self): # Check that the handbrake can be enable on an EGO vehicle, and the car stops sooner than without brakes
        with SimConnection() as sim:
            state = spawnState(sim)
            ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, state)
            control = lgsvl.VehicleControl()
            control.throttle = 1
            ego.apply_control(control, True)
            sim.run(1)
            control = lgsvl.VehicleControl()
            ego.apply_control(control,True)
            sim.run(3)
            noBrakePosition = ego.state.position.x
            
            sim.reset()
            ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, state)
            control = lgsvl.VehicleControl()
            control.throttle = 1
            ego.apply_control(control, True)
            sim.run(1)
            control = lgsvl.VehicleControl()
            control.handbrake = True
            ego.apply_control(control, True)
            sim.run(3)
            self.assertGreater(ego.state.position.x, noBrakePosition)
    
    def test_ego_reverse(self): # Check that the gear can be changed in an EGO vehicle, and the car moves in reverse
        with SimConnection() as sim:
            ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, spawnState(sim))
            control = lgsvl.VehicleControl()
            control.throttle = 0.5
            control.reverse = True
            ego.apply_control(control, True)
            sim.run(2)
            self.assertGreater(ego.state.position.x, sim.get_spawn()[0].position.x)

    def test_not_sticky_control(self): # Check that the a non sticky control is removed
        with SimConnection() as sim:
            ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, spawnState(sim))
            control = lgsvl.VehicleControl()
            control.throttle = 0.5
            ego.apply_control(control, True)
            sim.run(1)
            initialSpeed = ego.state.speed
            control = lgsvl.VehicleControl()
            control.throttle = 0.5
            ego.apply_control(control, False)
            sim.run(1)
            finalSpeed = ego.state.speed
            self.assertGreater(initialSpeed, finalSpeed)

    def test_vary_throttle(self): # Check that different throttle values accelerate differently
        with SimConnection() as sim:
            ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, spawnState(sim))
            control = lgsvl.VehicleControl()
            control.throttle = 0.5
            ego.apply_control(control, True)
            sim.run(1)
            initialSpeed = ego.state.speed

            sim.reset()
            ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, spawnState(sim))
            control = lgsvl.VehicleControl()
            control.throttle = 0.1
            ego.apply_control(control, True)
            sim.run(1)
            self.assertLess(ego.state.speed, initialSpeed)

    def test_vary_steering(self): # Check that different steering values turn the car differently
        with SimConnection() as sim:
            ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, spawnState(sim))
            control = lgsvl.VehicleControl()
            control.throttle = 0.5
            control.steering = -0.8
            ego.apply_control(control, True)
            sim.run(1)
            initialAngle = ego.state.rotation.y

            sim.reset()
            ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, spawnState(sim))
            control = lgsvl.VehicleControl()
            control.throttle = 0.5
            control.steering = -0.3
            ego.apply_control(control, True)
            sim.run(1)
            self.assertGreater(ego.state.rotation.y, initialAngle)

    def test_bounding_box_size(self): # Check that the bounding box is calculated properly and is reasonable
        with SimConnection() as sim:
            ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, spawnState(sim))
            bBox = ego.bounding_box
            self.assertAlmostEqual(bBox.size.x, abs(bBox.max.x-bBox.min.x))
            self.assertAlmostEqual(bBox.size.y, abs(bBox.max.y-bBox.min.y))
            self.assertAlmostEqual(bBox.size.z, abs(bBox.max.z-bBox.min.z))
            self.assertLess(bBox.size.x, 10)
            self.assertLess(bBox.size.y, 10)
            self.assertLess(bBox.size.z, 10)

    def test_bounding_box_center(self): # Check that the bounding box center is calcualted properly
        with SimConnection() as sim:
            ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, spawnState(sim))
            bBox = ego.bounding_box
            self.assertAlmostEqual(bBox.center.x, (bBox.max.x+bBox.min.x)/2)
            self.assertAlmostEqual(bBox.center.y, (bBox.max.y+bBox.min.y)/2)
            self.assertAlmostEqual(bBox.center.z, (bBox.max.z+bBox.min.z)/2)

    def test_equality(self): # Check that agent == operation works
        with SimConnection() as sim:
            ego1 = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, spawnState(sim))
            ego2 = sim.add_agent("XE_Rigged-autoware", lgsvl.AgentType.EGO, spawnState(sim))
            self.assertTrue(ego1 == ego1)
            self.assertFalse(ego1 == ego2)

    def test_set_fixed_speed(self):
        with SimConnection(60) as sim:
            ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, spawnState(sim))
            ego.set_fixed_speed(True, 15.0)
            self.assertAlmostEqual(ego.state.speed, 0, delta=0.001)
            sim.run(5)
            self.assertAlmostEqual(ego.state.speed, 15, delta=1)

    def create_EGO(self, sim): # Only create an EGO is none are already spawned
        return sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, spawnState(sim))

    # def test_large_throttle(self):
    #     with SimConnection(60) as sim:
    #         ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, spawnState(sim))
    #         control = lgsvl.VehicleControl()
    #         control.throttle = 1.0
    #         ego.apply_control(control, True)
    #         sim.run(3)
    #         full_speed = ego.state.speed

    #         sim.reset()
    #         ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, spawnState(sim))
    #         control = lgsvl.VehicleControl()
    #         control.throttle = 0.574
    #         ego.apply_control(control, True)
    #         sim.run(3)
    #         self.assertAlmostEqual(full_speed, ego.state.speed)

    # def test_large_steering(self):
    #     with SimConnection() as sim:
    #         ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, spawnState(sim))
    #         control = lgsvl.VehicleControl()
    #         control.throttle = 0.3
    #         control.steering = -10
    #         ego.apply_control(control, True)
    #         sim.run(1)
    #         self.assertAlmostEqual(ego.state.rotation.y, spawnState(sim).rotation.y, places=4)

    # def test_negative_braking(self):
    #     with SimConnection() as sim:
    #         ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, spawnState(sim))
    #         control = lgsvl.VehicleControl()
    #         control.braking = -1
    #         ego.apply_control(control, True)
    #         sim.run(1)
    #         self.assertAlmostEqual(ego.state.speed, 0)

    # def test_negative_throttle(self):
    #     with SimConnection() as sim:
    #         ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, spawnState(sim))
    #         control = lgsvl.VehicleControl()
    #         control.throttle = -1
    #         ego.apply_control(control, True)
    #         sim.run(1)
    #         self.assertAlmostEqual(ego.state.speed, 0.00000, places=3)
