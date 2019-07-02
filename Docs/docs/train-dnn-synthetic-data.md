# Training Deep Neural Networks with Synthetic Data using LGSVL Simulator

This documentation describes the whole pipeline for training 3D object detection deep networks with synthetic data collected from [LGSVL Simulator](https://www.lgsvlsimulator.com/).

In this project, we are going to use LGSVL Simulator Python APIs to randomly generate multiple scenes including environments and cars and to collect numerous synthetic data in KITTI format. With this data, we will train a state-of-the-art neural network using open-source implementation for KITTI object detection and evaluate its performance. Finally, we are going to deploy a final model and perform a real-time detection using ROS.

> This project is mostly based on a state-of-the-art neural network using open-source implementations with some modifications: [SECOND V1.5 for KITTI object detection](https://github.com/lgsvl/second.pytorch)

## Table of Contents

- [Prerequisites](#prerequisites)
- [Setup](#setup)
    - [Installing Docker](#installing-docker)
    - [Pulling Docker Image](#pulling-docker-image)
      - [What's inside Docker Image](#whats-inside-docker-image)
    - [Cloning the Repository](#cloning-the-repository)
- [Getting Started](#getting-started)
- [How to Collect Data](#how-to-collect-data)
  - [Collecting Synthetic Data](#collecting-synthetic-data)
  - [Preprocessing Data](#preprocessing-data)
  - [Config](#config)
- [How to Train SECOND Model](#how-to-train-second-model)
  - [Data Visualization](#data-visualization)
  - [Training Deep Neural Network](#training-deep-neural-network)
- [How to Deploy a Model](#how-to-deploy-a-model)
  - [Real-time Detection with ROS](#real-time-detection-with-ros)
- [References](#references)

## Prerequisites

- Docker CE
- NVIDIA Docker
- NVIDIA graphics card (required for training/inference with GPU)

## Setup

### Installing Docker

Please refer to [Lane Following Model](https://www.lgsvlsimulator.com/docs/lane-following/#setup) to install Docker CE and NVIDIA Docker.

### Pulling Docker Image

Docker image is provided to be used alongside this repository. The docker image is available [here](https://hub.docker.com/r/lgsvl/second-ros/).

To pull the image use the following command:

```
docker pull lgsvl/second-ros
```

#### What's inside Docker Image

- Ubuntu 16.04
- CUDA 9.0
- cuDNN 7.5.1.10
- Python 3.6
- PyTorch 1.0.0
- SpConv 1.0
- ROS Kinetic

### Cloning the Repository

This repository includes a submodule for SECOND network. To make sure that the submodule is also cloned use the following command:

```
git clone --recurse-submodules https://github.com/lgsvl/second-ros.git
```

## Getting Started

We have provided a pretrained model located in `model/hybrid_v4/voxelnet-817104.tckpt` and a sample ROSBAG with LiDAR point clouds. Before collecting your own data and starting to train a new model, you can run following command to start a demo:

```
docker-compose up second-ros
```

After a few seconds the pretrained model should be loaded and detect NPC vehicles from the ROSBAG. Rviz visualization window will open up as below:

[![rviz](images/train-rviz.png)](images/full_size_images/train-rviz.png)

To get into the container:

```
docker-compose run second-ros bash
```

## How to Collect Data

### Collecting Synthetic Data in KITTI Format

We use LGSVL Simulator's Python APIs for collecting synthetic data. We will use the script named `kitti_parser.py`, and the simulator must be up and running before we start the script.

The script will first load a `SanFrancisco` scene and spawn the ego vehicle with some NPC vehicles around in random positions in the scene. For each frame, it will save a camera image as a PNG file and LiDAR point clouds as a binary along with labels for NPCs captured by LiDAR. We parse the ground truth data into KITTI format.

To run the script and start collecting data:

```
python kitti_parser.py --start-index 0 --num-data 100 --dataset kitti --training
```

The script takes 4 arguments as below:
- start-index: Starting index of kitti filename
- num-data: Number of data points to collect
- dataset: Name of dataset
- training: Whether it's training dataset or testing

You can also get the sensor information for camera and LiDAR such as a camera projection matirx or a transformation matrix between camera and LiDAR. We use this information for projecting 3D bounding boxes into 2D bounding boxes since KITTI requires 3D and 2D bounding boxes as well.

You can take this code as an example for collecting ground truth data in KITTI format. You are very welcome to write your own script to collect data in your own way in any other formats (e.g., NuScenes).

### Preprocessing Data

`kitti_parser.py` will create a directory structure under `/root/data` as below and save synthetic data into it:

```
└── YOUR_DATASET
        ├── training  # Training set
        |   ├── image_2
        |   ├── calib
        |   ├── velodyne
        |   ├── velodyne_reduced
        |   └── label_2
        └── testing  # Validation set
            ├── image_2
            ├── calib
            ├── velodyne
            ├── velodyne_reduced
            └── label_2
```

For using SECOND network and its visualization tool, we first need to prepare pickles as below:

- To create KITTI image infos:

```
python second.pytorch/second/create_data.py create_kitti_info_file --data_path=KITTI_DATASET_ROOT 
```

- To create reduced point cloud:

```
python second.pytorch/second/create_data.py create_reduced_point_cloud --data_path=KITTI_DATASET_ROOT
```

- To create ground truth database:

```
python second.pytorch/second/create_data.py create_groundtruth_database --data_path=KITTI_DATASET_ROOT
```

### Config

Make sure that you have correct paths for your data in a config file (e.g., `/root/second.pytorch/second/configs/car.fhd.config`)

```
train_input_reader: {
  ...
  database_sampler {
    database_info_path: "/root/data/kitti/training/kitti_dbinfos_train.pkl"
    ...
  }
  kitti_info_path: "/root/data/kitti/training/kitti_infos_train.pkl"
  kitti_root_path: "/root/data/kitti"
}
...
eval_input_reader: {
  ...
  kitti_info_path: "/root/data/kitti/testing/kitti_infos_val.pkl"
  kitti_root_path: "/root/data/kitti"
}
```

## How to Train SECOND Network

### Data Visualization

It's best practice to verify your collected data before training a model. You can visualize your data in a web viewer and check LiDAR point clouds, camera images, and bounding boxes. You can also try your model and test a model inference.

To launch the visualization tool:

1. In Docker container, run `python ./second.pytorch/second/kittiviewer/backend.py main`
2. In your host machine, run `cd ./second.pytorch/second/kittiviewer/frontend && python -m http.server`
3. Open your browser and enter your frontend URL (e.g., http://127.0.0.1:8000)
4. Put backend URL into **backend** (e.g., http://127.0.0.1:16666)
5. Put kitti root path into **rootPath** (e.g., /root/data/kitti)
6. Put kitti info path into **infoPath** (e.g., /root/data/kitti/training/kitti_infos_train.pkl)
7. Click **load** to load your data
8. Put a data index into a blue box at the bottom center of a screen and press **Enter**

[![KITTI viewer](images/train-kittiviewer.png)](images/full_size_images/train-kittiviewer.png)

For inference step:

1. Put model checkpoint path into **checkpointPath** (e.g., /root/model/hybrid_v4/voxelnet-817104.tckpt)
2. Put model config path into **configPath** (e.g., /root/model/hybrid_v4/pipeline.config)
3. Click **buildNet** to build your model
4. Click **inference** to detect objects

[![Inference](images/train-inference.png)](images/full_size_images/train-inference.png)

### Training Deep Neural Network

After collecting enough amount of data, you can start training your own model using SECOND network. Please cd into `second.pytorch/second` and run below commands.

To train your own model:

```
python ./pytorch/train.py train --config_path=./configs/car.fhd.config --model_dir=/path/to/model_dir
```

To evaluate a model:

```
python ./pytorch/train.py evaluate --config_path=./configs/car.fhd.config --model_dir=/path/to/model_dir --measure_time=True --batch_size=1
```

> We provide a pretrained model located in `model/hybrid_v4/voxelnet-817104.tckpt`.

## How to Deploy a Model

### Real-time Detection with ROS

We implemented a ROS node `catkin_ws/src/second_ros/scripts/second_ros.py` to deploy a trained model and perform a real-time 3D object detection using LiDAR point clouds. You can launch this node either as a standalone ROS node or using the provided launch file. 

To launch as a stansalone ROS node:

```
rosrun second_ros second_ros.py
```

To launch with a launch file:

```
roslaunch second_ros second_ros
```

The launch file will run the `second-ros` node, play a sample rosbag with LiDAR point clouds, and open up rviz for visualizations.

The SECOND ROS node subscribes to a topic named `/kitti/velo/pointcloud` for LiDAR point cloud, pre-process the point cloud data, feed into the deployed model for prediction, and finally publish detected 3D bounding boxes into a topic `/detections` as a bounding box array. In our machine, the performance was about 20 Hz with 64 channels LiDAR.

## References

- [LGSVL Simulator](https://www.lgsvlsimulator.com/)
- [SECOND for KITTI object detection](https://github.com/traveller59/second.pytorch)
- [SpConv: PyTorch Spatially Sparse Convolution Library](https://github.com/traveller59/spconv)