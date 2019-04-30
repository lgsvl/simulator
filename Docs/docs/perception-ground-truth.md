# Ground Truth Obstacles

## Overview

You can use the LGSVL Simulator to view, subscribe to, and compare  ground truth obstacle information. The simulator allows visualization of 2D or 3D bounding boxes of vehicles, pedestrians, and unknown objects, and publishes detailed information (currently in a custom ROS message format) about the ground truth obstacles.

## View ground truth obstacles in Simulator

Ground truth obstacles for traffic can be viewed in the simulator with both 3D bounding boxes as well as 2D bounding boxes in the camera.

To view 3D Bounding boxes in the simulator:

 - Start the simulator in the San Francisco map and desired vehicle
 - Check "Sensor Effects"
 - Check "Enable Traffic"
 - Check "Enable Ground Truth 3D"

You should see 3D boxes in green highlighting NPC vehicles in the simulator main view.

![ground-truth-3d-boxes](images/ground-truth-3d-boxes.jpg)

To view 2D bounding boxes:

 - Start the simulator in the San Francisco map with desired vehicle
 - Check "Ground Truth 2D"

You should see 2D boxes highlighting NPC vehicles in the "Ground Truth 2D Camera" camera view.

![ground-truth-2d](images/ground-truth-2d-boxes.jpg)

### Bounding box colors

- Green: Vehicles
- Yellow: Pedestrians
- Purple: Unknown




## Subscribe to ground truth ROS messages

LGSVL Simulator also publishes custom ROS messages describing the ground truth data of non-ego vehicles.

In order to subscribe to the ground truth messages, you will need the ROS package [lgsvl_msgs](https://github.com/lgsvl/lgsvl_msgs). It contains custom ROS message types for 2D and 3D bounding boxes. You will also need to be running rosbridge.

### Install the lgsvl_msgs ROS package

#### Use LGSVL Apollo or Autoware repository

If you are using LGSVL's forks of [Apollo](https://github.com/lgsvl/Apollo) or [Autoware](https://github.com/lgsvl/Autoware), the package is already included as a submodule in the respective workspace:

- LGSVL Autoware: `autoware -> ros -> src -> msgs -> lgsvl_msgs`
- LGSVL Apollo: `apollo -> ros_pkgs -> src -> lgsvl_msgs`

Following the instructions to build the ROS workspace will build the `lgsvl_msgs` package as well.

If you are not running LGSVL's Apollo or Autoware forks, you can directly clone our `lgsvl_msgs` package into your ROS workspace and build.

#### Manually install lgsvl_msgs

1. Clone `lgsvl_msgs` to your ROS workspace or `msgs` directory:

   ```
   $ git clone https://github.com/lgsvl/lgsvl_msgs {MY_ROS_WS}
   ```

2. Build the ROS workspace:

   ```
   $ catkin_make
   ```



### Subscribe to ground truth messages from Simulator

You can subscribe to ground truth messages published as ROS messages (when 2D/3D ground truth are enabled)

- Topic: /simulator/ground_truth/2d_detections
- Message type: lgsvl_msgs/Detection2DArray: [Link](https://github.com/lgsvl/lgsvl_msgs/blob/master/msg/Detection2DArray.msg)



- Topic: /simulator/ground_truth/3d_detections
- Message type: lgsvl_msgs/Detection3DArray: [Link](https://github.com/lgsvl/lgsvl_msgs/blob/master/msg/Detection3DArray.msg)




## View estimated detections in Simulator

If you are running Autoware with LGSVL Simulator, you can also visualize Autoware object detection outputs in the simulator for both Lidar-based and Camera-based detections. Make sure that Autoware perception module is running and detection output topics have output messages.

(You can also publish to the below topics even if you are not using Autoware)

Required ROS topics:
- For Lidar detections: /detection/lidar_objects
- For Camera detections: /detection/vision_objects

To view Lidar detections:

- Start the simulator in the San Francisco map with XE_Rigged-autoware vehicle
- Check "Sensor Effects"
- Check "Enable LIDAR"
- Check "Enable Lidar Prediction"

You should see 3D bounding boxes highlighting Autoware Lidar detections in the simulator main view.

To view Camera detections:

- Start the simulator in the San Francisco map with XE_Rigged-autoware vehicle
- Check "Toggle Main Camera"
- Check "Enable Camera Prediction"

You should see 2D bounding boxes highlighting Autoware Camera detections in the simulator main camera view.

![ground-truth-visualizations](images/ground-truth-visuals.jpg)