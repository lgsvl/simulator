# <a name="top"></a> Running Apollo 3.0 with LGSVL Simulator
This repository is a fork of [Apollo](https://github.com/ApolloAuto/apollo) maintained by the LG Electronics Silicon Valley Lab which has modified and configured to facilitate use with [LG's Automotive Simulator](https://github.com/lgsvl/simulator).

**The software and source code in this repository are intended only for use with LG Automotive Simulator and *should not* be used in a real vehicle.**

[![](images/apollo-sim.png)](images/full_size_images/apollo-sim.png)

<h2> Table of Contents</h2>
[TOC]

## Getting Started <sub><sup>[top](#top)</sup></sub> {: #getting-started data-toc-label='Getting Started'}
The guide outlines the steps required to setup Apollo for use with the LG Automotive Simulator. If you have not already set up the simulator, please do so first by following the instructions [here](https://github.com/lgsvl/simulator).

We use our forked version of the Apollo repository, which can be found [here](https://github.com/lgsvl/apollo).

**The software and source code in this repository are intended only for use with LG Automotive Simulator and *should not* be used in a real vehicle.**

## Prerequisites <sub><sup>[top](#top)</sup></sub> {: #prerequisites data-toc-label='Prerequisites'}
* Linux operating system (preferably Ubuntu 14.04 or later)
* Nvidia graphics card (required for Perception)
    - Nvidia proprietary driver must be installed
    - The current version of Apollo does not support Volta and Turing architectures (this includes Titan V and RTX 2080 GPUs).



## Setup <sub><sup>[top](#top)</sup></sub> {: #setup data-toc-label='Setup'}

### Docker <sub><sup>[top](#top)</sup></sub> {: #docker data-toc-label='Docker'}
Apollo is designed to run out of docker containers. The image will mount this repository as a volume so the image will not need to be rebuilt each time a modification is made.

#### Installing Docker CE <sub><sup>[top](#top)</sup></sub> {: #installing-docker-ce data-toc-label='Installing Docker CE'}
To install Docker CE please refer to the [official documentation](https://docs.docker.com/install/linux/docker-ce/ubuntu/).
We also suggest following through with the [post installation steps](https://docs.docker.com/install/linux/linux-postinstall/).

#### Installing Nvidia Docker <sub><sup>[top](#top)</sup></sub> {: #installing-nvidia-docker data-toc-label='Installing Nvidia Docker'}
Before installing nvidia-docker make sure that you have an appropriate Nvidia driver installed.
To test if nvidia drivers are properly installed enter `nvidia-smi` in a terminal. If the drivers are installed properly an output similar to the following should appear.
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

The installation steps for nvidia-docker are available at the [official repo](https://github.com/NVIDIA/nvidia-docker). 

#### Pulling LGSVL Docker image <sub><sup>[top](#top)</sup></sub> {: #pulling-lgsvl-docker-image data-toc-label='Pulling LGSVL Docker image'}
LGSVL maintains a docker image to be used alongside this repository. The docker image is available [here](https://hub.docker.com/r/lgsvl/apollo/).

To pull the image use the following command:

    docker pull lgsvl/apollo

### Cloning the Repository <sub><sup>[top](#top)</sup></sub> {: #cloning-the-repository data-toc-label='Cloning the Repository'}
This repository includes a couple of submodules for HD Maps and rosbrige. To make sure that the submodules are also cloned use the following command:

    git clone --recurse-submodules https://github.com/lgsvl/apollo.git


### Building Apollo and ROSbridge <sub><sup>[top](#top)</sup></sub> {: #building-apollo-and-rosbridge data-toc-label='Building Apollo and ROSbridge'}
Now everything should be in place to build apollo. Apollo must be built from the container. To launch the container navigate to the directory where the repository was cloned and enter:

    ./docker/scripts/dev_start.sh

This should launch the container and mount a few volumes. It could take a few minutes to pull the latest volumes on the first run.

To get into the container:

    ./docker/scripts/dev_into.sh

Build Apollo:

    ./apollo.sh build_gpu

(optional) to build without gpu:

    ./apollo.sh build

Now build rosbrige:

    cd ros_pkgs
    catkin_make

## Launching Apollo alongside the simulator <sub><sup>[top](#top)</sup></sub> {: #launching-apollo-alongside-the-simulator data-toc-label='Launching Apollo alongside the simulator'}

Here we only describe only a simple case of driving from point A to point B using Apollo and the simulator. 

To launch apollo, first launch and enter a container as described in the previous steps.

* To start Apollo:

        ./scripts/bootstrap.sh

    Note: you may receive errors about dreamview not being build if you do not run the script from the `/apollo` directory.

* Launch rosbridge:

        ./scripts/rosbridge.sh

* Run the LG SVL Simulator (see instructions in the [simulator repository](https://github.com/lgsvl/simulator))
    
- Create a Simulation with the `BorregasAve` map and the `Jaguar2015XE (Apollo 3.0)` vehicle.
    
* Open Apollo dreamview in a browser by navigating to: `localhost:8888`
    - Select the `XE Rigged` vehicle and `BorregasAve` map in the top right corner.
    - Open the **Module Controller** tap (on the left bar).
    - Enable **Localization**, **Perception**, **Planning**, **Prediction**, **Routing**, and **Control**.
    - Navigate to the **Route Editing** tab.
    - Select a destination by clicking on a lane line and clicking **Submit Route**.
    - Watch the vehicle navigate to the destination.
    - To stop the docker container run the `dev_stop.sh` script in `apollo/docker/scripts` in a new terminal (not in the docker container).

### Adding a Vehicle <sub><sup>[top](#top)</sup></sub> {: #adding-a-vehicle data-toc-label='Adding a Vehicle'}
The default vehicles have their calibration files included in the [LGSVL Branch of Apollo 3.0](https://github.com/lgsvl/apollo/).

If not using a default vehicle:

1. Download the appropriate calibration fiels from [here](https://content.lgsvlsimulator.com/vehicles/) if using a vehicle created by LG Silicon Valley Lab.
2. Create a folder in `apollo/modules/calibration/data`. The name of this folder will be the name of the vehicle in Dreamview.
3. Extract the download and place the contents in folder created in the previous step.
4. If Dreamview was already running in the docker, restart it with `bootstrap.sh stop` and then `bootstrap.sh`

### Adding an HD Map <sub><sup>[top](#top)</sup></sub> {: #adding-an-hd-map data-toc-label='Adding an HD Map'}
The default maps have their HD map files included in the [LGSVL Branch of Apollo 5.0](https://github.com/lgsvl/apollo/).

If not using a default vehicle:

1. Download the appropriate HD map from [here](https://content.lgsvlsimulator.com/vehicles/) if using a map created by LG Silicon Valley Lab. The file will be `MAPNAME.tar`
2. In `apollo/modules/map/data` create a folder for the HD map. The name of this folder will be the name of the Map in Dreamview.
3. Extract the download and place the contents in the folder created in the previous step
4. If Dreamview was already running in the docker, restart it with `bootstrap.sh stop` and then `bootstrap.sh`

## Copyright and License <sub><sup>[top](#top)</sup></sub> {: #copyright-and-license data-toc-label='Copyright and License'}

Copyright (c) 2019 LG Electronics, Inc.

This software contains code licensed as described in LICENSE.
