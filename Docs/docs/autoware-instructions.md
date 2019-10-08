# <a name="top"></a> Autoware.AI 1.12.0 with LGSVL Simulator

**The software and source code in this repository are intended only for use with LG Automotive Simulator and *should not* be used in a real vehicle.**

[![](images/autoware-sim.png)](images/full_size_images/autoware-sim.png)

<h2> Table of Contents</h2>
[TOC]

## General <sub><sup>[top](#top)</sup></sub> {: #general data-toc-label='General'}

This guide goes through how to run Autoware.AI with the LG SVL Simulator.

In order to run Autoware with the LGSVL simulator, it is easiest to pull an official Autoware docker image (see [official guide](https://gitlab.com/autowarefoundation/autoware.ai/autoware/wikis/Generic-x86-Docker)), but it is also possible to [build autoware from source](https://gitlab.com/autowarefoundation/autoware.ai/autoware/wikis/Source-Build).

Autoware communicates with the simulator using the rosbridge_suite, which provides JSON interfacing with ROS publishers/subscribers. The official autoware docker containers have rosbridge_suite included.

## Setup <sub><sup>[top](#top)</sup></sub> {: #setup data-toc-label='Setup'}

### Requirements <sub><sup>[top](#top)</sup></sub> {: #requirements data-toc-label='Requirements'}

- Linux operating system
- Nvidia graphics card

#### Installing Docker CE <sub><sup>[top](#top)</sup></sub> {: #installing-docker-ce data-toc-label='Installing Docker CE'}

To install Docker CE please refer to the [official documentation](https://docs.docker.com/install/linux/docker-ce/ubuntu/). We also suggest following through with the [post installation steps](https://docs.docker.com/install/linux/linux-postinstall/).

#### Installing Nvidia Docker <sub><sup>[top](#top)</sup></sub> {: #installing-nvidia-docker data-toc-label='Installing Nvidia Docker'}

Before installing nvidia-docker make sure that you have an appropriate Nvidia driver installed. To test if nvidia drivers are properly installed enter `nvidia-smi` in a terminal. If the drivers are installed properly an output similar to the following should appear.

```
    +-----------------------------------------------------------------------------+
    | NVIDIA-SMI 390.87                 Driver Version: 390.87                    |
    |-------------------------------+----------------------+----------------------+
    | GPU  Name        Persistence-M| Bus-Id        Disp.A | Volatile Uncorr. ECC |
    | Fan  Temp  Perf  Pwr:Usage/Cap|         Memory-Usage | GPU-Util  Compute M. |
    |===============================+======================+======================|
    |   0  GeForce GTX 108...  Off  | 00000000:65:00.0  On |                  N/A |
    |  0%   59C    P5    22W / 250W |   1490MiB / 11175MiB |      4%      Default |
    +-------------------------------+----------------------+----------------------+

    +-----------------------------------------------------------------------------+
    | Processes:                                                       GPU Memory |
    |  GPU       PID   Type   Process name                             Usage      |
    |=============================================================================|
    |    0      1187      G   /usr/lib/xorg/Xorg                           863MiB |
    |    0      3816      G   /usr/bin/gnome-shell                         305MiB |
    |    0      4161      G   ...-token=7171B24E50C2F2C595566F55F1E4D257    68MiB |
    |    0      4480      G   ...quest-channel-token=3330599186510203656   147MiB |
    |    0     17936      G   ...-token=5299D28BAAD9F3087B25687A764851BB   103MiB |
    +-----------------------------------------------------------------------------+
```

### Simulator installation <sub><sup>[top](#top)</sup></sub> {: #simulator-installation data-toc-label='Simulator Installation'}

Follow the instructions on our simulator Github page [here](https://github.com/lgsvl/simulator).


## Launching Autoware alongside LGSVL Simulator <sub><sup>[top](#top)</sup></sub> {: #launching-autoware-alongside-lgsvl-simulator data-toc-label='Launching Autoware alongside LGSVL Simulator'}

Before launching, you need to create a directory called `shared_dir` in the home directory to hold maps and launch files for the simulator. The autoware docker container will mount this folder:
```
$ mkdir ~/shared_dir
$ cd ~/shared_dir
$ git clone https://github.com/lgsvl/autoware-data.git
```

To launch Autoware, first bring up the Docker container following these steps ([see [official guide](https://gitlab.com/autowarefoundation/autoware.ai/autoware/wikis/Generic-x86-Docker#case-1-using-pre-built-autoware-docker-images) for more details]):

- Clone the `docker` repository from `autoware.ai`:
```
$ git https://gitlab.com/autowarefoundation/autoware.ai/docker.git
```
- Navigate to:
```
$ cd docker/generic
```
- Pull the image and run (for release 1.12.0):
```
$ ./run.sh -t 1.12.0
```

Once inside the container, launch the runtime manager:

```
autoware@[MY_DESKTOP]:~$ roslaunch runtime_manager runtime_manager.launch
```

A few terminals will open, as well as a GUI for the runtime manager. In the runtime manager, click on the 'Quick Start' tab and load the following launch files from `~/shared_dir/autoware-data/BorregasAve/` by clicking "Ref" to the right of each text box:

- `my_map.launch`
- `my_sensing_simulator.launch`
- `my_localization.launch`
- `my_detection.launch`
- `my_mission_planning.launch`

[![](images/autoware-runtime-manager.png)](images/autoware-runtime-manager.png)

Click "Map" to load the launch file pertaining to the HD maps. An "Ok" should appear to the right of the "Ref" button when successfully loaded. Then click "Sensing" which also launches rosbridge.

- Run the LG SVL simulator
- Create a Simulation choosing `BorregasAve` map and `Jaguar2015XE (Autoware)` or another Autoware compatible vehicle.
- Enter `localhost:9090` for the Bridge Connection String.
- Run the created Simulation

A vehicle should appear in Borregas Ave in Sunnyvale, CA.

In the Autoware Runtime Manager, continue loading the other launch files - click "Localization" and wait for the time to display to the right of "Ref".

Then click "Rviz" to launch Rviz - the vector map and location of the vehicle in the map should show.

The vehicle may be mis-localized as the initial pose is important for NDT matching. To fix this, click "2D Pose Estimate" in Rviz, then click an approximate position for the vehicle on the map and drag in the direction it is facing before releasing the mouse button. This should allow NDT matching to find the vehicle pose (it may take a few tries). Note that the point cloud will not show up in rviz until ndt matching starts publishing a pose.

[![](images/autoware-ndt-matching.png)](images/autoware-ndt-matching.png)


An alternative would be to use GNSS for an inital pose or for localization but the current Autoware release (1.12.0) does not support GNSS coordinates outside of Japan. Fix for this is available in following pull requests: [utilities#27](https://gitlab.com/autowarefoundation/autoware.ai/utilities/merge_requests/27), [common#20](https://gitlab.com/autowarefoundation/autoware.ai/common/merge_requests/20), [core_perception#26](https://gitlab.com/autowarefoundation/autoware.ai/core_perception/merge_requests/26) These are not yet merged in Autoware master.

### Driving by following vector map:
To drive following the HD map follow these steps:
- in rviz, mark a destination by clicking '2D Nav Goal' and clicking at the destination and dragging along the road direction. Make sure to only choose a route that looks valid along the lane centerlines that are marked with orange lines in rviz. If an invalid destination is selected nothing will change in rviz, and you will need to relaunch the `Mission Planning` launch file in the `Quick Launch` tab to try another destination.
After choosing a valid destination the route will be highlighted in blue in rviz.

[![](images/autoware-valid-route.png)](images/autoware-valid-route.png)

To follow the selected route launch these nodes in the `Computing` tab of the Runtime Manager:

- Enable `waypoint_loader` and select the `lane.csv` file in the `~/shared_dir/autoware-data/BorregasAve/data/map/vector_map/` directory.
- Enable `lane_rule`, `lane_stop`, and `lane_select` to follow traffic rules based on the vector map.
- Enable `astar_avoid` and `velocity_set`.
- Enable `pure_pursuit` and `twist_filter` to start driving.

### Driving by following prerecorded waypoints:
A basic functionality of Autoware is to follow a prerecorded map while obeying traffic rules. To do this you will need to record a route first. Switch to the `Computing` tab and check the box for `waypoint_saver`. Make sure to select an appropriate location and file name by clicking on the `app` button.

Now you can drive around the map using the keyboard. Once you are satisfied with your route, uncheck the box for `waypoint_saver` to end the route.

To drive the route using autoware:
- Enable `waypoint_loader` while making sure the correct route file is selected in the `app` settings.
- Enable `lane_rule`, `lane_stop`, and `lane_select` to follow traffic rules based on the vector map.
- Enable `astar_avoid` and `velocity_set`.
- Enable `pure_pursuit` and `twist_filter` to start driving.

The ego vehicle should try to follow the waypoints at the velocity which they were originally recorded at. You can modify this velocity by manually editing the values csv file.

### Adding a Vehicle <sub><sup>[top](#top)</sup></sub> {: #adding-a-vehicle data-toc-label='Adding a Vehicle'}
The default vehicles have the calibration files included in the [LGSVL Autoware Data](https://github.com/lgsvl/autoware-data) Github repository.

If not using a default vehicle:

1. Download the appropriate calibration files from [here](https://content.lgsvlsimulator.com/vehicles/) if using a vehicle created by LG Silicon Valley Lab
2. Extract the contents and place them in the `shared_dir` folder that was created when installing Autoware
3. Run Autoware and from the Runtime Manager, click the `Ref` button next to `Localization` and `Detection` to browse and select a `.launch` file
4. Select the `.launch` files that were inclued in the `.tar`

### Adding an HD Map <sub><sup>[top](#top)</sup></sub> {: #adding-an-hd-map data-toc-label='Adding an HD Map'}
The default maps have the Vector map files included in the [LGSVL Autoware Data](https://github.com/lgsvl/autoware-data) Github repository.

If not using a default vehicle:

1. Download the appropriate HD map from [here](https://content.lgsvlsimulator.com/vehicles/) if using a map created by LG Silicon Valley Lab. The folder will be `MAPNAME.tar`
2. Extract the contents and place them in the `shared_dir` folder that was created when installing Autoware
3. Run Autoware and from the Runtime Manager, click the `Ref` button next to `Map` to browse to select a `.launch` file
4. Select the `.launch` file that was included in the `.tar`

## Copyright and License <sub><sup>[top](#top)</sup></sub> {: #copyright-and-license data-toc-label='Copyright and License'}

Copyright (c) 2019 LG Electronics, Inc.

This software contains code licensed as described in LICENSE.
