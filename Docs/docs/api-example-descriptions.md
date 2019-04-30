# Python API Example Script Descriptions

## Scenarios
The below scenarios assume that the simulator can be connected to an instance of Apollo 3.5. The Apollo modules that need to be started are shown below:
[![](images/apollo3-5.png)](images/apollo3-5.png)
It is recommended to start Apollo and the modules before running a scenario. Apollo's destination can be set after the scenario is started, but before it is run.

* [scenario-lane-change.py](../Api/examples/scenario-lane-change.py): This scenario simulates an NPC suddenly changing lanes in front of the EGO. Here the NPC uses the waypoint system to define its path. With waypoints, the NPC ignores other traffic and does not attempt to avoid collisions. This ensures that it changes lanes on command. The start of the scenario looks like this:
[![](images/lane-change-start.jpg)](images/full_size_images/lane-change-start.png)

   For this scenario, Apollo's destination can simply be the intersection in front of the EGO:
[![](images/lane-change-destination.png)](images/lane-change-destination.png)

* [scenario-overtaker.py](../Api/examples/scenario-overtaker.py): This scenario simulates an NPC approaching the EGO from behind at a faster speed. The NPC will change lanes to the left to pass the EGO and will change lanes back once it has passed the EGO. The way the EGO reacts to an approaching NPC will affect when the NPC changes lanes so in this scenario the NPC follows the lanes in the HD map. The start of the scenario looks ike this:
[![](images/overtaker-start.jpg)](images/full_size_images/overtaker-start.png)

* [scenario-trafficjam.py](../Api/examples/scenario-trafficjam.py): This scenario simulates the EGO approaching stopped traffic. The traffic does not move and the EGO is expected to stop at a safe distance. The start of the scenario looks like this: 
[![](images/trafficjam-start.jpg)](images/full_size_images/trafficjam-start.png)


   For both overtaker and trafficjam, the same destination can be given to Apollo. It is the end of the highway in the same lane the the EGO is spawned in. This is quite far away and should ensure that the NPC in overtaker is able to pass the EGO.
   [![](images/highway-scenario-destination.png)](images/highway-scenario-destination.png)

## Other Uses
* [kitti-parser.py](../Api/examples/kitti-parser.py): This script collects data in the KITTI format. This data can be used to train for detecting vehicles in images.