#
# Copyright (c) 2019 LG Electronics, Inc.
#
# This software contains code licensed as described in LICENSE.
#

import lgsvl
import math

class TestException(Exception):
    pass

def separation(V1, V2):
    xdiff = V1.x - V2.x
    ydiff = V1.y - V2.y
    zdiff = V1.z - V2.z
    return math.sqrt(xdiff * xdiff + ydiff * ydiff + zdiff * zdiff)