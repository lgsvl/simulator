#
# Copyright (c) 2018 LG Electronics, Inc.
#
# This software contains code licensed as described in LICENSE.
#

# DO NOT INCLUDE IN __init__.py

import unittest
import math

import lgsvl
from .common import SimConnection, spawnState, TestTimeout

class TestManual(unittest.TestCase):
    def test_wipers(self):
        try:
            with SimConnection() as sim:
                ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, spawnState(sim))
                control = lgsvl.VehicleControl()
                control.windshield_wipers = 1
                ego.apply_control(control, True)
                sim.run(1)
                input("Press Enter if wipers are on low")

                control = lgsvl.VehicleControl()
                control.windshield_wipers = 2
                ego.apply_control(control, True)
                sim.run(1)
                input("Press Enter if wipers are on medium")

                control = lgsvl.VehicleControl()
                control.windshield_wipers = 3
                ego.apply_control(control, True)
                sim.run(1)
                input("Press Enter if wipers are on high")
        except TestTimeout:
            self.fail("Wipers were not on")
    
    def test_headlights(self):
        try:
            with SimConnection() as sim:
                ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, spawnState(sim))
                control = lgsvl.VehicleControl()
                control.headlights = 1
                ego.apply_control(control, True)
                sim.run(1)
                input("Press Enter if headlights are on")

                control = lgsvl.VehicleControl()
                control.headlights = 2
                ego.apply_control(control, True)
                sim.run(1)
                input("Press Enter if high beams are on")
        except TestTimeout:
            self.fail("Headlights were not on")

    def test_blinkers(self):
        try:
            with SimConnection() as sim:
                ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, spawnState(sim))
                control = lgsvl.VehicleControl()
                control.turn_signal_left = True
                ego.apply_control(control, True)
                sim.run(3)
                input("Press Enter if left turn signal is on")

                control = lgsvl.VehicleControl()
                control.turn_signal_right = True
                ego.apply_control(control, True)
                sim.run(3)
                input("Press Enter if right turn signal is on")
        except TestTimeout:
            self.fail("Turn signals were not on")

    def test_wiper_large_value(self):
        try:
            with SimConnection() as sim:
                ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, spawnState(sim))
                control = lgsvl.VehicleControl()
                control.windshield_wipers = 4
                ego.apply_control(control, True)
                sim.run(1)
                input("Press Enter if wipers are off")
        except TestTimeout:
            self.fail("Wipers were on")

    def test_wiper_str(self):
        try:
            with SimConnection() as sim:
                ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, spawnState(sim))
                control = lgsvl.VehicleControl()
                control.windshield_wipers = "on"
                ego.apply_control(control, True)
                sim.run(1)
                input("Press Enter if wipers are off")
        except TestTimeout:
            self.fail("Wipers were on")

    def test_headlights_large_value(self):
        try:
            with SimConnection() as sim:
                ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, spawnState(sim))
                control = lgsvl.VehicleControl()
                control.headlights = 123
                ego.apply_control(control, True)
                sim.run(1)
                input("Press Enter if headlights are off")
        except TestTimeout:
            self.fail("Headlights were on")

    def test_headlights_str(self):
        try:
            with SimConnection() as sim:
                ego = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, spawnState(sim))
                control = lgsvl.VehicleControl()
                control.headlights = "123"
                ego.apply_control(control, True)
                sim.run(1)
                input("Press Enter if headlights are off")
        except TestTimeout:
            self.fail("Headlights were on")
