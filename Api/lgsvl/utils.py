#
# Copyright (c) 2018 LG Electronics, Inc.
#
# This software contains code licensed as described in LICENSE.
#

from .geometry import Vector, Transform

import math
import inspect

def accepts(*types):
  def check_accepts(f):
    assert len(types) + 1 == f.__code__.co_argcount
    def new_f(*args, **kwargs):
      names = inspect.getfullargspec(f)[0]
      it = zip(args[1:], types, names[1:])
      for (a, t, n) in it:
        if not isinstance(a, t):
          raise TypeError("Argument '{}' should have '{}' type".format(n, t))
      return f(*args, **kwargs)
    new_f.__name__ = f.__name__
    return new_f
  return check_accepts


def transform_to_matrix(tr):
  px = tr.position.x
  py = tr.position.y
  pz = tr.position.z

  ax = tr.rotation.x * math.pi / 180.0
  ay = tr.rotation.y * math.pi / 180.0
  az = tr.rotation.z * math.pi / 180.0

  sx, cx = math.sin(ax), math.cos(ax)
  sy, cy = math.sin(ay), math.cos(ay)
  sz, cz = math.sin(az), math.cos(az)

  # Unity uses left-handed coordinate system, Rz * Rx * Ry order
  return [ [ sx * sy * sz + cy * cz, cx * sz, sx * cy * sz - sy * cz, 0.0 ],
           [ sx * sy * cz - cy * sz, cx * cz, sy * sz + sx * cy * cz, 0.0 ],
           [ cx * sy, -sx, cx * cy, 0.0 ],
           [ px, py, pz, 1.0 ],
         ]


def vector_dot(a, b):
  return a.x * b.x + a.y * b.y + a.z * b.z


# this works only with transformation matrices (no scaling, no projection)
def matrix_inverse(m):
  x = Vector(m[0][0], m[0][1], m[0][2])
  y = Vector(m[1][0], m[1][1], m[1][2])
  z = Vector(m[2][0], m[2][1], m[2][2])
  v = Vector(m[3][0], m[3][1], m[3][2])
  a = -vector_dot(v, x)
  b = -vector_dot(v, y)
  c = -vector_dot(v, z)
  return [ [ x.x, y.x, z.x, 0.0 ],
           [ x.y, y.y, z.y, 0.0 ],
           [ x.z, y.z, z.z, 0.0 ],
           [   a,   b,   c, 1.0 ],
         ]


def matrix_multiply(a, b):
  r = [[0, 0, 0, 0] for t in range(4)]
  for i in range(4):
    for j in range(4):
      for k in range(4):
        r[i][j] += a[i][k] * b[k][j]
  return r


def vector_multiply(v, m):
  tmp = [None] * 3
  for i in range(3):
    tmp[i] = v.x * m[0][i] + v.y * m[1][i] + v.z * m[2][i] + m[3][i]
  return Vector(tmp[0], tmp[1], tmp[2])
