#
# Copyright (c) 2019 LG Electronics, Inc.
#
# This software contains code licensed as described in LICENSE.
#

import unittest

import lgsvl
import lgsvl.utils

from .common import SimConnection

class TestUtils(unittest.TestCase): # Check that transform_to_matrix calculates the right values
    def test_transform_to_matrix(self):
        transform = lgsvl.Transform(lgsvl.Vector(1,2,3), lgsvl.Vector(4,5,6))
        expectedMatrix = [[0.9913729386253347, 0.10427383718471564, -0.07941450396586013, 0.0], \
                            [-0.0980843287345578, 0.9920992900156518, 0.07822060602635744, 0.0], \
                            [0.08694343573875718, -0.0697564737441253, 0.9937680178757644, 0.0], \
                            [1, 2, 3, 1.0]]

        matrix = lgsvl.utils.transform_to_matrix(transform)
        for i in range(4):
            for j in range(4):
                    self.assertAlmostEqual(matrix[i][j], expectedMatrix[i][j])

    def test_matrix_multiply(self): # Check that matrix_multiply calculates the right values
        inputMatrix = lgsvl.utils.transform_to_matrix(lgsvl.Transform(lgsvl.Vector(1,2,3), lgsvl.Vector(4,5,6)))
        expectedMatrix = [[0.9656881042915112, 0.21236393599051254, -0.1494926216255657, 0.0], \
                            [-0.18774677387638924, 0.9685769782741936, 0.1631250626244768, 0.0], \
                            [0.1794369920860106, -0.12946117505974142, 0.9752139098799174, 0.0], \
                            [2.0560345883724906, 3.8792029959836434, 6.058330761714148, 1.0]]
        matrix = lgsvl.utils.matrix_multiply(inputMatrix, inputMatrix)
        for i in range(4):
            for j in range(4):
                    self.assertAlmostEqual(matrix[i][j], expectedMatrix[i][j])

    def test_matrix_inverse(self): # Check that matrix_inverse calculates the right values
        inputMatrix = lgsvl.utils.transform_to_matrix(lgsvl.Transform(lgsvl.Vector(1,2,3), lgsvl.Vector(4,5,6)))
        expectedMatrix = [[0.9913729386253347, -0.0980843287345578, 0.08694343573875718, 0.0], \
                            [0.10427383718471564, 0.9920992900156518, -0.0697564737441253, 0.0], \
                            [-0.07941450396586013, 0.07822060602635744, 0.9937680178757644, 0.0], \
                            [-0.9616771010971856, -2.120776069375818, -2.9287345418778, 1.0]]
        matrix = lgsvl.utils.matrix_inverse(inputMatrix)
        for i in range(4):
            for j in range(4):
                    self.assertAlmostEqual(matrix[i][j], expectedMatrix[i][j])

    def test_vector_multiply(self): # Check that vector_multiply calculates the right values
        inputMatrix = lgsvl.utils.transform_to_matrix(lgsvl.Transform(lgsvl.Vector(1,2,3), lgsvl.Vector(4,5,6)))
        inputVector = lgsvl.Vector(10,20,30)
        expectedVector = lgsvl.Vector(11.560345883724906, 20.792029959836434, 33.58330761714148)

        vector = lgsvl.utils.vector_multiply(inputVector, inputMatrix)
        self.assertAlmostEqual(vector.x, expectedVector.x)
        self.assertAlmostEqual(vector.y, expectedVector.y)
        self.assertAlmostEqual(vector.z, expectedVector.z)

    def test_vector_dot(self): # Check that vector_dot calculates the right values
        result = lgsvl.utils.vector_dot(lgsvl.Vector(1,2,3), lgsvl.Vector(4,5,6))
        self.assertAlmostEqual(result, 32)
