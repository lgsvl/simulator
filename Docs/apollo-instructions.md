# Running Apollo with LG SVL Simulator


## Table of Contents

1. [Getting Started](#getting-started)
2. [Prerequisites](#prerequisites)
3. [Setup](#setup)
    - [Docker](#docker)
    - [Cloning the Repository](#cloning-the-repository)
    - [Building Apollo and Rosbridge](#building-apollo-and-rosbridge)
4. [Launching Apollo Alongside the Simulator](#launching-apollo-alongside-the-simulator)

## Getting Started
The guide outlines the steps required to setup Apollo for use with the LG Automotive Simulator. If you have not already set up the simulator, please do so first by following the instructions [here](https://github.com/lgsvl/simulator).

We use our forked version of the Apollo repository, which can be found [here](https://github.com/lgsvl/apollo).

**The software and source code in this repository are intended only for use with LG Automotive Simulator and *should not* be used in a real vehicle.**

## Prerequisites
* Linux operating system (preferably Ubuntu 14.04 or later)
* Nvidia graphics card (required for Perception)
    - Nvidia proprietary driver must be installed
    - The current version of Apollo does not support Volta and Turing architectures (this includes Titan V and RTX 2080 GPUs).



## Setup

### Docker
Apollo is designed to run out of docker containers. The image will mount this repository as a volume so the image will not need to be rebuilt each time a modification is made.

#### Installing Docker CE
To install Docker CE please refer to the [official documentation](https://docs.docker.com/install/linux/docker-ce/ubuntu/).
We also suggest following through with the [post installation steps](https://docs.docker.com/install/linux/linux-postinstall/).

#### Installing Nvidia Docker
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

#### Pulling LGSVL Docker image
LGSVL maintains a docker image to be used alongside this repository. The docker image is available [here](https://hub.docker.com/r/lgsvl/apollo/).

To pull the image use the following command:

    docker pull lgsvl/apollo

### Cloning the Repository
This repository includes a couple of submodules for HD Maps and rosbrige. To make sure that the submodules are also cloned use the following command:

    git clone --recurse-submodules git@github.com:lgsvl/apollo.git


### Building Apollo and rosbridge
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

## Launching Apollo alongside the simulator

Here we only describe only a simple case of driving from point A to point B using Apollo and the simulator. 

To launch apollo, first launch and enter a container as described in the previous steps.

* To start Apollo:

        ./scripts/bootstrap.sh

    Note: you may receive errors about dreamview not being build if you do not run the script from the `/apollo` directory.

* Launch rosbridge:

        ./scripts/rosbridge.sh

* Run the LG SVL Simulator (see instructions in the [simulator repository](https://github.com/lgsvl/simulator))
    - Select the `San Francisco` map and the `XE-Rigged-apollo` vehicle.
    - Enable GPS, IMU, LIDAR, HD Mode, and Telephota Camera.
    - (optional) Enable traffic.

* Open Apollo dreamview in a browser by navigating to: `localhost:8888`
    - Select the `LG SVL 1080` vehicle and `San Francisco` map in the top right corner.
    - Open the **Module Controller** tap (on the left bar).
    - Enable **Localization**, **Perception**, **Planning**, **Prediction**, **Routing**, and **Control**.
    - Navigate to the **Route Editing** tab.
    - Select a destination by clicking on a lane line and clicking **Submit Route**.
    - Watch the vehicle navigate to the destination.
    - To stop the docker container run the `dev_stop.sh` script in `apollo/docker/scripts` in a new terminal (not in the docker container).
