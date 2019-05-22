#
# Copyright (c) 2019 LG Electronics, Inc.
#
# This software contains code licensed as described in LICENSE.
#

import lgsvl
import math

class TestException(Exception):
    pass

def right_lane_check(simulator, position):
    egoLane = simulator.map_point_on_lane(position)
    rightLane = simulator.map_point_on_lane(lgsvl.Vector(position.x-0.6993, position.y, position.z+9.9755))

    return almost_equal(egoLane.position.x, rightLane.position.x) and \
            almost_equal(egoLane.position.y, rightLane.position.y) and \
            almost_equal(egoLane.position.z, rightLane.position.z)

def almost_equal(a, b, diff= 0.5):
    return abs(a-b) <= diff

def separation(V1, V2):
    xdiff = V1.x - V2.x
    ydiff = V1.y - V2.y
    zdiff = V1.z - V2.z
    return math.sqrt(xdiff * xdiff + ydiff * ydiff + zdiff * zdiff)