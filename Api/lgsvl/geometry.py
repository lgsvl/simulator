#
# Copyright (c) 2018 LG Electronics, Inc.
#
# This software contains code licensed as described in LICENSE.
#

class Vector:
  def __init__(self, x = 0.0, y = 0.0, z = 0.0):
    self.x = x
    self.y = y
    self.z = z

  @staticmethod
  def from_json(j):
    return Vector(j["x"], j["y"], j["z"])

  def to_json(self):
    return {"x": self.x, "y": self.y, "z": self.z}

  def __repr__(self):
    return "Vector({}, {}, {})".format(self.x, self.y, self.z)


class BoundingBox:
  def __init__(self, min, max):
    self.min = min
    self.max = max

  @staticmethod
  def from_json(j):
    return BoundingBox(Vector.from_json(j["min"]), Vector.from_json(j["max"]))

  def to_json(self):
    return {"min": self.min.to_json(), "max": self.max.to_json()}

  def __repr__(self):
    return "BoundingBox({}, {})".format(self.min, self.max)

  @property
  def center(self):
    return Vector(
      (self.max.x + self.min.x) * 0.5,
      (self.max.y + self.min.y) * 0.5,
      (self.max.z + self.min.z) * 0.5,
    )

  @property
  def size(self):
    return Vector(
      self.max.x - self.min.x,
      self.max.y - self.min.y,
      self.max.z - self.min.z,
    )


class Transform:
  def __init__(self, position = None, rotation = None):
    if position is None: position = Vector()
    if rotation is None: rotation = Vector()
    self.position = position
    self.rotation = rotation

  @staticmethod
  def from_json(j):
    return Transform(Vector.from_json(j["position"]), Vector.from_json(j["rotation"]))

  def to_json(self):
    return {"position": self.position.to_json(), "rotation": self.rotation.to_json()}

  def __repr__(self):
    return "Transform(position={}, rotation={})".format(self.position, self.rotation)
