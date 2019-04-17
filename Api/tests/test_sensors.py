#
# Copyright (c) 2019 LG Electronics, Inc.
#
# This software contains code licensed as described in LICENSE.
#

import unittest
import os
import lgsvl
from .common import SimConnection, spawnState, notAlmostEqual

# TODO add tests for bridge to check if enabled sensor actually sends data

class TestSensors(unittest.TestCase):
    def test_apollo_3_5_sensors(self):
        with SimConnection() as sim:
            agent = self.create_EGO(sim, "XE_Rigged-apollo_3_5")
            expectedSensors = ['velodyne', 'GPS', 'Telephoto Camera', \
                'Main Camera', 'IMU', 'RADAR', 'CANBUS', 'Segmentation Camera', \
                'Left Camera', 'Right Camera']
            sensors = agent.get_sensors()
            sensorNames = [s.name for s in sensors]

            for sensor in expectedSensors:
                with self.subTest(sensor):
                    self.assertIn(sensor, sensorNames)

            for s in sensors:
                msg = "Apollo 3.5 Sensor " + s.name
                with self.subTest(msg):
                    self.valid_sensor(s, msg)

    def test_santafe_sensors(self):
        with SimConnection() as sim:
            agent = self.create_EGO(sim, "SF_Rigged-apollo")
            expectedSensors = ['velodyne', 'GPS', 'Telephoto Camera', 'Main Camera', \
                'IMU', 'RADAR', 'CANBUS', 'Segmentation Camera', 'Left Camera', 'Right Camera']
            sensors = agent.get_sensors()
            sensorNames = [s.name for s in sensors]

            for sensor in expectedSensors:
                with self.subTest(sensor):
                    self.assertIn(sensor, sensorNames)

            for s in sensors:
                msg = "Santa Fe Apollo Sensor " + s.name
                with self.subTest(msg):
                    self.valid_sensor(s, msg)

    def test_lgsvl_sensors(self):
        with SimConnection() as sim:
            agent = self.create_EGO(sim, "SF_Rigged-apollo")
            expectedSensors = ['velodyne', 'GPS', 'Telephoto Camera', 'Main Camera', \
                'IMU', 'RADAR', 'CANBUS', 'Segmentation Camera', 'Left Camera', 'Right Camera']
            sensors = agent.get_sensors()
            sensorNames = [s.name for s in sensors]

            for sensor in expectedSensors:
                with self.subTest(sensor):
                    self.assertIn(sensor, sensorNames)

            for s in sensors:
                msg = "lgsvl Sensor " + s.name
                with self.subTest(msg):
                    self.valid_sensor(s, msg)

    def test_ep_sensors(self):
        with SimConnection() as sim:
            agent = self.create_EGO(sim, "EP_Rigged-apollo")
            expectedSensors = ['velodyne', 'GPS', 'Telephoto Camera', 'Main Camera', \
            'IMU', 'RADAR', 'CANBUS', 'Segmentation Camera', 'Left Camera', 'Right Camera']
            sensors = agent.get_sensors()
            sensorNames = [s.name for s in sensors]
            
            for sensor in expectedSensors:
                with self.subTest(sensor):
                    self.assertIn(sensor, sensorNames)

            for s in sensors:
                msg = "EP Sensor " + s.name
                with self.subTest(msg):
                    self.valid_sensor(s, msg)

    def test_apollo_sensors(self): # Check that all the Apollo sensors are there
        with SimConnection() as sim:
            agent = self.create_EGO(sim, "XE_Rigged-apollo")
            expectedSensors = ["velodyne", "GPS", "Telephoto Camera", "Main Camera", "IMU", \
                "RADAR", "CANBUS", "Segmentation Camera", "Left Camera", "Right Camera"]

            sensors = agent.get_sensors()
            sensorNames = [s.name for s in sensors]

            for sensor in expectedSensors:
                with self.subTest(sensor):
                    self.assertIn(sensor, sensorNames)

            # self.assertIn("velodyne", sensorNames)
            # self.assertIn("Telephoto Camera", sensorNames)
            # self.assertIn("Main Camera", sensorNames)
            # self.assertIn("IMU", sensorNames)
            # self.assertIn("Segmentation Camera", sensorNames)
            # self.assertIn("Left Camera", sensorNames)
            # self.assertIn("Right Camera", sensorNames)
            # self.assertIn("RADAR", sensorNames)
            # self.assertIn("CANBUS", sensorNames)

            for s in sensors:
                msg = "Apollo Sensor " + s.name
                with self.subTest(msg):
                    self.valid_sensor(s, msg)

    def test_autoware_sensors(self): # Check that all Autoware sensors are there
        with SimConnection() as sim:
            agent = self.create_EGO(sim, "XE_Rigged-autoware")
            expectedSensors = ["velodyne", "GPS", "Main Camera", "IMU", "Depth Camera", "Segmentation Camera"]

            sensors = agent.get_sensors()
            sensorNames = [s.name for s in sensors]

            for sensor in expectedSensors:
                with self.subTest(sensor):
                    self.assertIn(sensor, sensorNames)

            # self.assertIn("velodyne", sensorNames)
            # self.assertIn("Main Camera", sensorNames)
            # self.assertIn("IMU", sensorNames)
            # self.assertIn("Segmentation Camera", sensorNames)
            # self.assertIn("Depth Camera", sensorNames)
            # self.assertIn("GPS", sensorNames)

            for s in sensors:
                msg = "Autoware Sensor " + s.name
                with self.subTest(msg):
                    self.valid_sensor(s, msg )

    def test_save_sensor(self): # Check that sensor results can be saved
        with SimConnection(120) as sim:

            path = "main-camera.png"
            islocal = os.environ.get("SIMULATOR_HOST", "127.0.0.1") == "127.0.0.1"

            if islocal:
                path = os.getcwd() + path
                if os.path.isfile(path):
                    os.remove(path)

            agent = self.create_EGO(sim, "XE_Rigged-apollo")
            sensors = agent.get_sensors()

            savedSuccess = False

            for s in sensors:
                if s.name == "Main Camera":
                    savedSuccess = s.save(path)
                    break

            self.assertTrue(savedSuccess)

            if islocal:
                self.assertTrue(os.path.isfile(path))
                self.assertGreater(os.path.getsize(path), 0)
                os.remove(path)

    def test_save_lidar(self):
        with SimConnection(240) as sim:
            path = "lidar.pcd"
            islocal = os.environ.get("SIMULATOR_HOST", "127.0.0.1") == "127.0.0.1"

            if islocal:
                path = os.getcwd() + path
                if os.path.isfile(path):
                    os.remove(path)

            agent = self.create_EGO(sim, "XE_Rigged-apollo")
            sensors = agent.get_sensors()
            savedSuccess = False

            for s in sensors:
                if s.name == "velodyne":
                    savedSuccess = s.save(path)
                    break

            self.assertTrue(savedSuccess)

            if islocal:
                self.assertTrue(os.path.isfile(path))
                self.assertGreater(os.path.getsize(path), 0)
                os.remove(path)


    def test_GPS(self):
        with SimConnection() as sim:
            state = lgsvl.AgentState()
            state.transform = sim.get_spawn()[0]
            state.velocity = lgsvl.Vector(-50, 0, 0)
            agent = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, state)
            sensors = agent.get_sensors()
            initialGPSData = None
            gps = None
            for s in sensors:
                if s.name == "GPS":
                    gps = s
                    initialGPSData = gps.data
            sim.run(1)
            finalGPSData = gps.data

            latChanged = notAlmostEqual(initialGPSData.latitude, finalGPSData.latitude)
            lonChanged = notAlmostEqual(initialGPSData.longitude, finalGPSData.longitude)
            northingChanged = notAlmostEqual(initialGPSData.northing, finalGPSData.northing)
            eastingChanged = notAlmostEqual(initialGPSData.easting, finalGPSData.easting)
            self.assertTrue(latChanged or lonChanged)
            self.assertTrue(northingChanged or eastingChanged)
            self.assertNotAlmostEqual(gps.data.latitude, 0)
            self.assertNotAlmostEqual(gps.data.longitude, 0)
            self.assertNotAlmostEqual(gps.data.northing, 0)
            self.assertNotAlmostEqual(gps.data.easting, 0)
            self.assertNotAlmostEqual(gps.data.altitude, 0)
            self.assertNotAlmostEqual(gps.data.orientation, 0)

    def test_sensor_enabling(self): # Check if sensors can be enabled
        with SimConnection() as sim:
            agent = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, spawnState(sim))
            for s in agent.get_sensors():
                if s.name == "velodyne":
                    s.enabled = True
            
            for s in agent.get_sensors():
                with self.subTest(s.name):
                    if not(s.name == "velodyne" or s.name == "CANBUS"):
                        self.assertFalse(s.enabled)
                    else:
                        self.assertTrue(s.enabled)

    def test_sensor_equality(self):
        with SimConnection() as sim:
            agent = sim.add_agent("XE_Rigged-apollo", lgsvl.AgentType.EGO, spawnState(sim))
            prevSensor = None
            for s in agent.get_sensors():
                self.assertTrue(s == s)
                if prevSensor is not None:
                    self.assertFalse(s == prevSensor)
                prevSensor = s

    def create_EGO(self, sim, name): # Creates the speicified EGO and removes any others
        return sim.add_agent(name, lgsvl.AgentType.EGO, spawnState(sim))

    def valid_sensor(self, sensor, msg): # Checks that the sensor is close to the EGO and not overly rotated
        self.assertBetween(sensor.transform.rotation, 0, 360, msg)
        self.assertBetween(sensor.transform.position, -5, 5, msg)

    def assertBetween(self, vector, min, max, msg):
        self.assertGreaterEqual(vector.x, min, msg)
        self.assertLessEqual(vector.x, max, msg)

        self.assertGreaterEqual(vector.y, min, msg)
        self.assertLessEqual(vector.y, max, msg)

        self.assertGreaterEqual(vector.z, min, msg)
        self.assertLessEqual(vector.z, max, msg)
