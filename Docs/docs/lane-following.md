# ROS2 End-to-End Lane Following Model with LGSVL Simulator

This documentation describes applying a deep learning neural network for lane following in [LGSVL Simulator](https://www.lgsvlsimulator.com/). In this project, we use LGSVL Simulator for customizing sensors (one main camera and two side cameras) for a car, collect data for training, and deploying and testing a trained model.

> This project was inspired by [NVIDIA's End-to-End Deep Learning Model for Self-Driving Cars](https://devblogs.nvidia.com/deep-learning-self-driving-cars/)

## Video

<div class="video-container">
<iframe style="display:block;margin:auto;" width="560" height="340" src="https://www.youtube.com/embed/uMfA1-wTB7I" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>
</div>
</br>

## Table of Contents

- [Getting Started](#getting-started)
- [Prerequisites](#prerequisites)
- [Setup](#setup)
    - [Installing Docker CE](#installing-docker-ce)
    - [Installing NVIDIA Docker](#installing-nvidia-docker)
    - [Pulling Docker Image](#pulling-docker-image)
    - [What's inside Docker Image](#whats-inside-docker-image)
- [Features](#features)
- [Training Details](#training-details)
    - [Network Architecture](#network-architecture)
    - [Hyperparameters](#hyperparameters)
    - [Dataset](#dataset)
- [How to Collect Data and Train Your Own Model with LGSVL Simulator](#how-to-collect-data-and-train-your-own-model-with-lgsvl-simulator)
    - [Collect data from LGSVL Simulator](#collect-data-from-lgsvl-simulator)
    - [Data preprocessing](#data-preprocessing)
    - [Train a model](#train-a-model)
    - [Drive with your trained model in LGSVL Simulator](#drive-with-your-trained-model-in-lgsvl-simulator)
- [Future Works and Contributing](#future-works-and-contributing)
- [References](#references)

## Getting Started

First, clone this repository:

```
git clone --recurse-submodules https://github.com/lgsvl/lanefollowing.git
```

Next, pull the latest Docker image:

```
docker pull lgsvl/lanefollowing:latest
```

To build ROS2 packages:

```
docker-compose up build
```

Now, launch the lane following model:

```
docker-compose up drive
```

(Optional) If you want visualizations, run drive_visual instead of drive:
```
docker-compose up drive_visual
```

That's it! Now, the lane following ROS2 node and the rosbridge should be up and running, waiting for LGSVL Simulator to connect.


## Prerequisites

- Docker CE
- NVIDIA Docker
- NVIDIA graphics card (required for training/inference with GPU)


## Setup

### Installing Docker CE

To install Docker CE please refer to the [official documentation](https://docs.docker.com/install/linux/docker-ce/ubuntu/). We also suggest following through with the [post installation steps](https://docs.docker.com/install/linux/linux-postinstall/).

### Installing NVIDIA Docker

Before installing nvidia-docker make sure that you have an appropriate NVIDIA driver installed.
To test if NVIDIA drivers are properly installed enter `nvidia-smi` in a terminal. If the drivers are installed properly an output similar to the following should appear.
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

### Pulling Docker Image

```
docker pull lgsvl/lanefollowing:latest
```

### What's inside Docker Image

- Ubuntu 18.04
- CUDA 9.2
- cuDNN 7.1.4.18
- Python 3.6
- TensorFlow 1.8
- Keras 2.2.4
- ROS2 Crystal + rosbridge
- Jupyter Notebook

## Features

- Training mode: Manually drive the vehicle and collect data
- Autonomous Mode: The vehicle drives itself based on Lane Following model trained from the collected data
- ROS2-based
    - Time synchronous data collection node
    - deploying a trained model in a node
- Data preprocessing for training
    - Data normalization
    - Data augmentation
    - Splitting data into training set and test set
    - Writing/Reading data in HDF5 format
- Deep Learning model training: Train a model using Keras with TensorFlow backend

## Training Details

### Network Architecture

The network has 559,419 parameters and consists of 9 layers, including 5 convolutional layers, 3 fully connected layers, and an output layer.

| Layer (type) | Output Shape | Param # |
|:------------:|:------------:|:-------:|
| lambda_1 (Lambda) | (None, 70, 320, 3) | 0 |
| conv2d_1 (Conv2D) | (None, 33, 158, 24) | 1824 |
| conv2d_2 (Conv2D) | (None, 15, 77, 36) | 21636 |
| conv2d_3 (Conv2D) | (None, 6, 37, 48) | 43248 |
| conv2d_4 (Conv2D) | (None, 4, 35, 64) | 27712 |
| conv2d_5 (Conv2D) | (None, 2, 33, 64) | 36928 |
| dropout_1 (Dropout) | (None, 2, 33, 64) | 0 |
| flatten_1 (Flatten) | (None, 4224) | 0 |
| dense_1 (Dense) | (None, 100) | 422500 |
| dense_2 (Dense) | (None, 50) | 5050 |
| dense_3 (Dense) | (None, 10) | 510 |
| dense_4 (Dense) | (None, 1) | 11 |

### Hyperparameters

- Learning rate: 1e-04
- Learning rate decay: None
- Dropout rate: 0.5
- Mini-batch size: 128
- Epochs: 30
- Optimization algorithm: Adam
- Loss function: Mean squared error
- Training/Test set ratio: 8:2

### Dataset

- Number of training data: 48,624 labeled images
- Number of validation data: 12,156 labeled images

#### Center Image
![](images/lanefollowing-center_image.jpg)

#### Left Image
![](images/lanefollowing-left_image.jpg)

#### Right Image
![](images/lanefollowing-right_image.jpg)

#### Original Image
![](images/lanefollowing-original.png)

#### Cropped Image
![](images/lanefollowing-cropped.png)

#### Data Distribution
![](images/lanefollowing-data_distribution.png)

## How to Collect Data and Train Your Own Model with LGSVL Simulator

### Collect data from LGSVL Simulator

To collect camera images as well as corresponding steering commands for training, we provide a ROS2 *collect* node which subscribes to three camera image topics and a control command topic, approximately synchronizes time stamps of those messages, and then saves them as csv and jpg files. The topic names and types are as below:
- Center camera: /simulator/sensor/camera/center/compressed (sensor_msgs/CompressedImage)
- Left camera: /simulator/sensor/camera/left/compressed (sensor_msgs/CompressedImage)
- Right camera: /simulator/sensor/camera/right/compressed (sensor_msgs/CompressedImage)
- Control command: /simulator/control/command (geometry_msgs/TwistStamped)

To launch *rosbridge* and *collect* ROS2 node in a terminal:
```
docker-compose up collect
```

To drive a car and publish messages over rosbridge in training mode:
- Launch **LGSVL Simulator**
- Click **Free Roaming** mode
- Select **San Francisco** map and **XE_Rigged-lgsvl** vehicle
- Make sure the simulator establishes connection with rosbridge
- Click **Run** to begin
- Enable **Main Camera**, **Left Camera**, **Right Camera**, and check **Publish Control Command**

The node will start collecting data as you drive the car around. You should be able to check log messages in the terminal where the *collect* node is running. The final data is saved in `lanefollowing/ros2_ws/src/lane_following/train/data/` as csv and jpg files.

### Data preprocessing

Before start training your model with the data you collected, data preprocessing is required. This task includes:
- Resize image resolutions from 1920 x 1080 to 200 x 112
- Crop top portion of images as we are mostly interested in road part of an image
- Data augmentation by adding artificial bias to side camera images or flipping images
- Data normalization to help the model converge faster
- Split data into training and testing dataset

To run data preprocessing and obtain datasets for training:
```
docker-compose up preprocess
```

This will preprocess your data and write outputs in `lanefollowing/ros2_ws/src/lane_following/train/data/hdf5/` into HDF5 format for better I/O performance for training.

### Train a model

We use Keras with TensorFlow backend for training our model as an example. The hyperparameters such as learning rate, batch size, or number of epochs were chosen empirically. You can train a model as is but you are also welcome to modify the model architecture or any hyperparameters as you like in the code.

To start training:
```
docker-compose up train
```

After training is done, your final trained model will be in `lanefollowing/ros2_ws/src/lane_following/train/model/{current-date-and-time-in-utc}.h5` and your model is ready to drive autonomously.

### Drive with your trained model in LGSVL Simulator

Now, it's time to deploy your trained model and test drive with it using LGSVL Simulator. You can replace your trained model with an existing one in `lanefollowing/ros2_ws/src/lane_following/model/model.h5` as this is the path for deployment.

To launch *rosbridge* and *drive* ROS2 node in a terminal:
```
docker-compose up drive
```

Or, if you want visualizations as well, run drive_visual instead:
```
docker-compose up drive_visual
```

To drive a car in autonomous mode:
- Launch **LGSVL Simulator**
- Click **Free Roaming** mode
- Select **San Francisco** map and **XE_Rigged-lgsvl** vehicle
- Make sure the simulator establishes connection with rosbridge
- Click **Run** to begin
- Enable **Main Camera** (we don't need side cameras for inference)

Your car will start driving autonomously and try to mimic your driving behavior when training the model.

## Future Works and Contributing

Though the network can successfully drive and follow lanes on the bridge, there's still a lot of room for future improvements (i.e., biased to drive straight, afraid of shadows, few training data, and etc).
- To improve model robustness collect more training data by driving in a wide variety of environments
    - Changing weather and lighting effects (rain, fog, road wetness, time of day)
    - Adding more road layouts and road textures
    - Adding more shadows on roads
    - Adding NPC cars around the ego vehicle
- Predict the car throttle along with the steering angle
- Take into accounts time series analysis using RNN (Recurrent Neural Network)

## References

- [Lane Following Github Repository](https://github.com/lgsvl/lanefollowing)
- [LGSVL Simulator](https://www.lgsvlsimulator.com/)
- [NVIDIA's End-to-End Deep Learning Model for Self-Driving Cars](https://devblogs.nvidia.com/deep-learning-self-driving-cars/)

## Copyright and License

Copyright (c) 2018 LG Electronics, Inc.

This software contains code licensed as described in LICENSE.