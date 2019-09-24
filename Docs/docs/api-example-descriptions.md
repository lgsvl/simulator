# Python API Use Case Examples [](#top)

The LGSVL Simulator teams has created sample Python scripts that use the LGSVL Simulator Python API to test specific scenarios or perform certain tasks. These example scripts can be found on our Github [here](https://github.com/lgsvl/PythonAPI/tree/master/Api/examples).

Please [contact](mailto:contact@lgsvlsimulator.com) us if you would like to [contribute](contributing.md) examples that you are using, or submit a [pull request](https://github.com/lgsvl/PythonAPI/pulls).



## Scenarios [[top]] {: #scenarios data-toc-label='Scenarios'}

We have created basic sample scenarios using the Python API. See [here](api-how-to-run-scenario.md) for a step-by-step guide on how to run one of these scenarios. Several are based on the sample Test Cases from [NHTSA](https://www.nhtsa.gov/sites/nhtsa.dot.gov/files/documents/13882-automateddrivingsystems_092618_v1a_tag.pdf).

The below scenarios assume that the simulator can be connected to an instance of Apollo 5.0. See the guide for getting connected with Apollo 5.0 [here](apollo5-0-instructions.md). The Apollo modules that need to be started are shown below (localization, perception, planning, prediction, routing, traffic light, transform, control):

[![](images/apollo3-5.png)](images/apollo3-5.png)

It is recommended to start Apollo and the modules before running a scenario. Apollo's destination can be set after Localization and Routing have been started.

### Vehicle Following [[top]] {: #vehicle-following data-toc-label='Vehicle Following'}
* Scripts: [Perform Vehicle Following](https://github.com/lgsvl/PythonAPI/blob/master/Api/examples/NHTSA-sample-tests/Vehicle-Following)
* This scenario simulates the EGO vehicle approaching a slower NPC from behind. The EGO is expected to accelerate up to the speed limit and catch up to the NPC. 

[![](images/scenario-VFStart.png)](images/full_size_images/scenario-VFStart.png)

For this scenario, the destination is the end of the lane.

[![](images/scenario-SLRDestination.png)](images/scenario-SLRDestination.png)

### Encroaching Oncoming Vehicle [[top]] {: #encroaching-oncoming-vehicle data-toc-label='Encroaching Oncoming Vehicle'}
* Scripts: [Detect and Respond to Encroaching Oncoming Vehicle](https://github.com/lgsvl/PythonAPI/blob/master/Api/examples/NHTSA-sample-tests/Encroacing-Oncoming-Vehicles)
* This scenario simulates the EGO vehicle approaching an oncoming NPC that is half in the EGO's lane making a collision imminent. The EGO is expected to avoid a collision.
* Here the NPC uses the waypoint system to define its path. With waypoints, the NPC ignores other traffic and does not attempt to avoid collisions.

[![](images/scenario-EOVStart.png)](images/full_size_images/scenario-EOVStart.png)

For this scenario, the destinaion is the end of the lane. The same destination can be used as the Vehicle Following scripts.


## Other Uses [[top]] {: #other-uses data-toc-label='Other Uses'}

### Collecting data in KITTI format [[top]] {: #collecting-data-in-kitti-format data-toc-label='Collecting data in KITTI format'}

* Script: [kitti_parser.py](https://github.com/lgsvl/PythonAPI/blob/master/Api/examples/kitti_parser.py)
* This script shows an example of collecting data in the KITTI format. This data can be used to train for detecting vehicles in images. 
* This script spawns the ego vehicle in a random position in the San Francisco map. Then a number of NPC vehicles are randomly spawned in front of the ego vehicle. Camera and ground truth data is saved in the KITTI format. This data can be used to train for detecting vehicles in images. 
* For more information on KITTI please see: [http://www.cvlibs.net/datasets/kitti/index.php](http://www.cvlibs.net/datasets/kitti/index.php) The data format is defined in a README file downloadable from: [https://s3.eu-central-1.amazonaws.com/avg-kitti/devkit_object.zip](https://s3.eu-central-1.amazonaws.com/avg-kitti/devkit_object.zip)

### Automated Driving System Test Cases [[top]] {: #automated-driving-system-test-cases data-toc-label='Automated Driving System Test Cases'}
* The United States National Highway Traffic Safety Administration released a report describing a framework for establishing sample preliminary tests. The report is available online: [A Framework for Automated Driving System Testable Cases and Scenarios](https://www.nhtsa.gov/sites/nhtsa.dot.gov/files/documents/13882-automateddrivingsystems_092618_v1a_tag.pdf)
* We created several of the described tests available here: [NHTSA-sample-tests](https://github.com/lgsvl/PythonAPI/blob/master/Api/examples/NHTSA-sample-tests/)
* These tests run the ADS at different speeds. To accomplish this with Apollo, the speed limit in the HD map of the appropriate lanes needs to be adjusted and the planning configuration should also be changed to limit Apollo's top speed.
* The ADS destination is described in the report. For our implementation of the Perform Lane Change tests, the same destination as the above Overtaker and Traffic Jam scenarios is used.
