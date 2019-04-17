#
# Copyright (c) 2019 LG Electronics, Inc.
#
# This software contains code licensed as described in LICENSE.
#

from .geometry import Transform
from .utils import accepts

from collections import namedtuple

GpsData = namedtuple("GpsData", "latitude longitude northing easting altitude orientation")

class Sensor:
  def __init__(self, remote, uid, name):
    self.remote = remote
    self.uid = uid
    self.name = name
    
  @property
  def transform(self):
    j = self.remote.command("sensor/transform/get", {"uid": self.uid})
    return Transform.from_json(j)

  @property
  def enabled(self):
    return self.remote.command("sensor/enabled/get", {"uid": self.uid})

  @enabled.setter
  @accepts(bool)
  def enabled(self, value):
    self.remote.command("sensor/enabled/set", {"uid": self.uid, "enabled": value})

  def __eq__(self, other):
    return self.uid == other.uid

  def __hash__(self):
    return hash(self.uid)

  @staticmethod
  def create(remote, j):
    if j["type"] == "camera":
      return CameraSensor(remote, j)
    if j["type"] == "lidar":
      return LidarSensor(remote, j)
    if j["type"] == "imu":
      return ImuSensor(remote, j)
    if j["type"] == "gps":
      return GpsSensor(remote, j)
    if j["type"] == "radar":
      return RadarSensor(remote, j)
    if j["type"] == "canbus":
      return CanBusSensor(remote, j)
    raise ValueError("Sensor type '{}' not supported".format(j["type"]))


class CameraSensor(Sensor):
  def __init__(self, remote, j):
    super().__init__(remote, j["uid"], j["name"])
    self.frequency = j["frequency"]
    self.width = j["width"]
    self.height = j["height"]
    self.fov = j["fov"]
    self.near_plane = j["near_plane"]
    self.far_plane = j["far_plane"]
    """
    RGB      - 24-bit color image
    DEPTH    - 8-bit grayscale depth buffer
    SEMANTIC - 24-bit color image with semantic segmentation
    """
    self.format = j["format"]

  @accepts(str, int, int)
  def save(self, path, quality = 75, compression = 6):
    success = self.remote.command("sensor/camera/save", {
      "uid": self.uid,
      "path": path,
      "quality": quality,
      "compression": compression,
    })
    return success


class LidarSensor(Sensor):
  def __init__(self, remote, j):
    super().__init__(remote, j["uid"], j["name"])
    self.min_distance = j["min_distance"]
    self.max_distance = j["max_distance"]
    self.rays = j["rays"]
    self.rotations = j["rotations"] # rotation frequency, Hz
    self.measurements = j["measurements"] # how many measurements each ray does per one rotation
    self.fov = j["fov"]
    self.angle = j["angle"]
    self.compensated = j["compensated"]

  @accepts(str)
  def save(self, path):
    success = self.remote.command("sensor/lidar/save", {
      "uid": self.uid,
      "path": path,
    })
    return success


class ImuSensor(Sensor):
  def __init__(self, remote, j):
    super().__init__(remote, j["uid"], j["name"])


class GpsSensor(Sensor):
  def __init__(self, remote, j):
    super().__init__(remote, j["uid"], j["name"])
    self.frequency = j["frequency"]

  @property
  def data(self):
    j = self.remote.command("sensor/gps/data", {"uid": self.uid})
    return GpsData(j["latitude"], j["longitude"], j["northing"], j["easting"], j["altitude"], j["orientation"])


class RadarSensor(Sensor):
  def __init__(self, remote, j):
    super().__init__(remote, j["uid"], j["name"])


class CanBusSensor(Sensor):
  def __init__(self, remote, j):
    super().__init__(remote, j["uid"], j["name"])
    self.frequency = j["frequency"]
