#
# Copyright (c) 2019 LG Electronics, Inc.
#
# This software contains code licensed as described in LICENSE.
#

import signal
import lgsvl
import os

class TestTimeout(Exception):
    pass

class TestException(Exception):
    pass

class SimConnection:
  def __init__(self, seconds=30, scene="SanFrancisco", error_message=None, load_scene=True):
    if error_message is None:
      error_message = 'test timed out after {}s.'.format(seconds)
    self.seconds = seconds
    self.error_message = error_message
    self.scene = scene
    self.load_scene = load_scene

  def handle_timeout(self, signum, frame):
    raise TestTimeout(self.error_message)

  def __enter__(self):
    signal.signal(signal.SIGALRM, self.handle_timeout)
    signal.alarm(self.seconds)

    self.sim = lgsvl.Simulator(os.environ.get("SIMULATOR_HOST", "127.0.0.1"), 8181)
    if self.load_scene:
      if self.sim.current_scene == self.scene:
        self.sim.reset()
        signal.alarm(self.seconds - 20)
      else:
        self.sim.load(self.scene)
    return self.sim

  def __exit__(self, exc_type, exc_val, exc_tb):
    signal.alarm(0)
    # agents = self.sim.get_agents()
    # for a in agents:
    #   self.sim.remove_agent(a)
    self.sim.close()

def spawnState(sim, index=0):
  state = lgsvl.AgentState()
  state.transform = sim.get_spawn()[index]
  return state

def cmEqual(self, a, b, msg): # Test vectors within 1cm
  self.assertAlmostEqual(a.x, b.x, 2, msg)
  self.assertAlmostEqual(a.y, b.y, 2, msg)
  self.assertAlmostEqual(a.z, b.z, 2, msg)

def mEqual(self, a, b, msg): # Test vectors within 1cm
  self.assertAlmostEqual(a.x, b.x, delta=1.5, msg=msg)
  self.assertAlmostEqual(a.y, b.y, delta=1.5, msg=msg)
  self.assertAlmostEqual(a.z, b.z, delta=1.5, msg=msg)

def notAlmostEqual(a,b):
  return round(a-b,7) != 0
