# Python API Use Case Examples

The LGSVL Simulator teams has created sample Python scripts that use the LGSVL Simulator Python API to test specific scenarios or perform certain tasks. These example scripts can be found on our Github [here](https://github.com/lgsvl/simulator/tree/master/Api/examples).

Please [contact](mailto:contact@lgsvlsimulator.com) us if you would like to [contribute](contributing.md) examples that you are using, or submit a [pull request](https://github.com/lgsvl/simulator/pulls).



## Scenarios

We have created basic sample scenarios using the Python API. See [here](api-how-to-run-scenario.md) for a step-by-step guide on how to run one of these scenarios. Several are based on the scenario specifications from [OpenScenario](http://www.openscenario.org/download.html).

The below scenarios assume that the simulator can be connected to an instance of Apollo 3.5. See the guide for getting connected with Apollo 3.5 [here](apollo3-5-instructions.md). The Apollo modules that need to be started are shown below (localization, perception, planning, prediction, routing, traffic light, transform, control):

[![](images/apollo3-5.png)](images/apollo3-5.png)

It is recommended to start Apollo and the modules before running a scenario. Apollo's destination can be set after the scenario is started, but before it is run. 



### Lane Change

* Script: [scenario-lane-change.py](https://github.com/lgsvl/simulator/blob/master/Api/examples/scenario-npc-lane-change.py)
* This scenario simulates an NPC suddenly changing lanes in front of the ego vehicle. Here the NPC uses the waypoint system to define its path. With waypoints, the NPC ignores other traffic and does not attempt to avoid collisions. This ensures that it changes lanes on command. The start of the scenario looks like this:

[![](images/lane-change-start.jpg)](images/full_size_images/lane-change-start.png)



For this scenario, Apollo's destination can simply be the intersection in front of the ego vehicle:

[![](images/lane-change-destination.png)](images/lane-change-destination.png)



### Overtaker

* Script: [scenario-overtaker.py](https://github.com/lgsvl/simulator/blob/master/Api/examples/scenario-overtaker.py)
*  This scenario simulates an NPC approaching the ego vehicle from behind at a faster speed. The NPC will change lanes to the left to pass the ego vehicle and will change lanes back once it has passed the ego vehicle. The way the ego vehicle reacts to an approaching NPC will affect when the NPC changes lanes so in this scenario the NPC follows the lanes in the HD map. The start of the scenario looks ike this:

[![](images/overtaker-start.jpg)](images/full_size_images/overtaker-start.png)



### Traffic Jam

* Script: [scenario-trafficjam.py](https://github.com/lgsvl/simulator/blob/master/Api/examples/scenario-trafficjam.py)
* This scenario simulates the ego vehicle approaching stopped traffic. The traffic does not move and the ego vehicle is expected to stop at a safe distance. The start of the scenario looks like this:  

[![](images/trafficjam-start.jpg)](images/full_size_images/trafficjam-start.png)



For both Overtaker and Traffic Jam, the same destination can be given to Apollo. It is the end of the highway in the same lane the the ego vehicle is spawned in. This is quite far away and should ensure that the NPC in overtaker is able to pass the ego vehicle.

[![](images/highway-scenario-destination.png)](images/highway-scenario-destination.png)



## Other Uses

### Collecting data in KITTI format

* Script: [kitti_parser.py](https://github.com/lgsvl/simulator/blob/master/Api/examples/kitti_parser.py)
* This script shows an example of collecting data in the KITTI format. This data can be used to train for detecting vehicles in images. 
* This script spawns the ego vehicle in a random position in the San Francisco map. Then a number of NPC vehicles are randomly spawned in front of the ego vehicle. Camera and ground truth data is saved in the KITTI format. This data can be used to train for detecting vehicles in images. 
* For more information on KITTI please see: [http://www.cvlibs.net/datasets/kitti/index.php](http://www.cvlibs.net/datasets/kitti/index.php) The data format is defined in a README file downloadable from: [https://s3.eu-central-1.amazonaws.com/avg-kitti/devkit_object.zip](https://s3.eu-central-1.amazonaws.com/avg-kitti/devkit_object.zip)

### Automated Driving System Test Cases
* The United States National Highway Traffic Safety Administration released a report describing a framework for establishing sample preliminary tests. The report is available online: [A Framework for Automated Driving System Testable Cases and Scenarios](https://www.nhtsa.gov/document/framework-automated-driving-system-testable-cases-and-scenarios)
* We created several of the described tests available here: [NHTSA-sample-tests](https://github.com/lgsvl/simulator/blob/master/Api/examples/NHTSA-sample-tests/)
* These tests run the ADS at different speeds. To accomplish this with Apollo, the speed limit in the HD map of the appropriate lanes needs to be adjusted and the planning configuration should also be changed to limit Apollo's top speed.
* The ADS destination is described in the report. For our implementation of the Perform Lane Change tests, the same destination as the above Overtaker and Traffic Jam scenarios is used.