#!/usr/bin/env python3
#
# Copyright (c) 2019 LG Electronics, Inc.
#
# This software contains code licensed as described in LICENSE.
#

from lgsvl import *
from lgsvl.utils import *

# global "world" transform (for example, car position)
world = Transform(Vector(10, 20, 30), Vector(11, 22, 33))

# "local" transform, relative to world transform (for example, Lidar sensor)
local = Transform(Vector(0, 2, -0.5), Vector(0, 0, 0))

# combine them in one transform matrix
mtx = matrix_multiply(transform_to_matrix(local), transform_to_matrix(world))

# compute inverse for transforming points
mtx = matrix_inverse(mtx)

# now transform some point from world position into coordinate system of local transform of Lidar
pt = vector_multiply(Vector(15, 20, 30), mtx)
print(pt)
