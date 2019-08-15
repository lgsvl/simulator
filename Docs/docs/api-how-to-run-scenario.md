# How To Run a Scenario or Test Case
The following steps detail how to run the Vehicle Following scenario. This scenario and other example scenarios can be found on our [examples page](api-example-descriptions.md).


1. Install Simulator Python API by navigating to the Api submodule directory under the simulator repository:
	```
	pip3 install --user .
	```
2. Start the simulator.

3. Set environment variables SIMULATOR_HOST and BRIDGE_HOST
    * SIMULATOR_HOST is where the simulator will be run. The default value for this is "localhost" and does not need to be set if the simulator is running on the same machine that the python script will be run from.
    * BRIDGE_HOST is where the AD stack will be run. This is relative to where the simulator is run. The default value is "localhost" which is the same machine as the simulator.
    * For example, if computer A will run the simulator and computer B will run the AD stack
        * SIMULATOR_HOST should be set to the IP of computer A
        * BRIDGE_HOST should be set to the IP of computer B
	* To set the variables for the current terminal window use
	```
	export SIMULATOR_HOST=192.168.1.100
	```

6. Start your AD stack. The example scripts are written for Apollo 5.0. See below for how to edit the scripts to work with other AD stacks.
  
    * Select the MKZ as the vehicle SingleLaneRoad for the map
    * Start all modules and the bridge (if relevant)

5. Run the script

    ```
    ./VF_S_25.py
    ```
6. Set the destination for the AD stack. For this scenario, the destination is the end of the current lane.

    [![](images/scenario-SLRDestination.png)](images/scenario-SLRDestination.png)

8. The AV should start driving forward towards the NPC. It should avoid crashing into the NPC.


## How to Edit the EGO vehicle
In each of the example scenarios and test cases, there is a section that setups up the EGO vehicle: [![](images/python-ego-setup.png)](images/python-ego-setup.png)

If using a different AD stack, the vehicle type needs to be changed.

* Change the agent name (orange string) to what is desired.
    * e.g. For Autoware it might be "Lexus2016RXHybrid (Autoware)"
