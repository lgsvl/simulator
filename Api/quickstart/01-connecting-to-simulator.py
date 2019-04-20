#!/usr/bin/env python3
#
# Copyright (c) 2019 LG Electronics, Inc.
#
# This software contains code licensed as described in LICENSE.
#

import os
import lgsvl

# Connects to the simulator instance at the ip defined by SIMULATOR_HOST, default is localhost or 127.0.0.1
sim = lgsvl.Simulator(os.environ.get("SIMULATOR_HOST", "127.0.0.1"), 8181)

print("Version =", sim.version)
print("Current Time =", sim.current_time)
print("Current Frame =", sim.current_frame)
