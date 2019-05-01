#!/usr/bin/env python3
#
# Copyright (c) 2019 LG Electronics, Inc.
#
# This software contains code licensed as described in LICENSE.
#

# This script spawns the EGO vehicle in a random position in the San Francisco map
# Then a number of NPC vehicles are randomly spawned in front of the EGO
# Data is saved in the KITTI format. For more information on KITTI please see: http://www.cvlibs.net/datasets/kitti/index.php
# The data format is defined in a readme.txt downloadable from: https://s3.eu-central-1.amazonaws.com/avg-kitti/devkit_object.zip

# Install numpy and PIL before running this script
# SIMULATOR_HOST environment variable also needs to be set before running the script

# 3 command line arguements are required when running this script. The arguements are:
# number of data points to collect (int)
# starting index of kitti filename (int)
# path to save location (str)

import lgsvl
from lgsvl.utils import transform_to_matrix
import os
import math
import time
import random
import numpy as np
from PIL import Image
import sys

numDataPoints = int(sys.argv[1])
startIndex = int(sys.argv[2])

BASE_PATH = sys.argv[3]
CALIB_PATH = os.path.join(BASE_PATH, "calib")
IMAGE_JPG_PATH = os.path.join(BASE_PATH, "image_jpg")
IMAGE_PNG_PATH = os.path.join(BASE_PATH, "image_2")
LIDAR_PCD_PATH = os.path.join(BASE_PATH, "velodyne_pcd")
LIDAR_BIN_PATH = os.path.join(BASE_PATH, "velodyne")
LABEL_PATH = os.path.join(BASE_PATH, "label_2")


class KittiParser:
    def __init__(self, scene_name="SanFrancisco", agent_name="XE_Rigged-lgsvl", start_idx=0):
        self.scene_name = scene_name
        self.agent_name = agent_name
        self.sim = None
        self.ego = None
        self.ego_state = None
        self.sensor_camera = None
        self.sensor_lidar = None
        self.sensor_imu = None
        self.npcs = []
        self.npcs_state = []
        self.idx = start_idx

        # Sensor Calibrations: intrinsic & extrinsic
        self.camera_intrinsics = None
        self.projection_matrix = None
        self.rectification_matrix = None
        self.tr_velo_to_cam = None
        self.tr_imu_to_velo = None

# Sets up the required folder hierarchy and starts the simulator
    def bootstrap(self):
        os.makedirs(CALIB_PATH, exist_ok=True)
        os.makedirs(IMAGE_JPG_PATH, exist_ok=True)
        os.makedirs(IMAGE_PNG_PATH, exist_ok=True)
        os.makedirs(LIDAR_PCD_PATH, exist_ok=True)
        os.makedirs(LIDAR_BIN_PATH, exist_ok=True)
        os.makedirs(LABEL_PATH, exist_ok=True)

        self.sim = lgsvl.Simulator(os.environ.get("SIMULATOR_HOST", "127.0.0.1"), 8181)
        self.load_scene()
        self.sim.reset()
        self.ego = self.sim.add_agent(self.agent_name, lgsvl.AgentType.EGO)
        self.load_sensors()
        self.calibrate()

        print("\nBootstrap success!")

# Loads the scene specified when KittiParser is created. To save time, the scene is loaded only if it has not already be loaded
    def load_scene(self):
        if self.sim.current_scene != self.scene_name:
            print("Loading {} scene...".format(self.scene_name))
            self.sim.load(self.scene_name)
        print("\n{} scene has been loaded!".format(self.scene_name))

# Saves the sensor objects for later use
    def load_sensors(self):
        print("\nAvailable sensors:")
        for sensor in self.ego.get_sensors():
            print("{}: {}".format(sensor.name, sensor.transform))
            if sensor.name == "Main Camera":
                self.sensor_camera = sensor
            if sensor.name == "velodyne":
                self.sensor_lidar = sensor
            if sensor.name == "IMU":
                self.sensor_imu = sensor

# Finds a random point on the map to spawn the EGO
    def get_ego_random_transform(self):
        origin = lgsvl.Transform()
        sx = origin.position.x
        sy = origin.position.y
        sz = origin.position.z

        mindist = 0.0
        maxdist = 700.0
        angle = random.uniform(0.0, 2 * math.pi)
        dist = random.uniform(mindist, maxdist)
        point = lgsvl.Vector(sx + dist * math.cos(angle), sy, sz + dist * math.sin(angle))

        transform = self.sim.map_point_on_lane(point)

        return transform

# Finds a random point near the EGO on the map. 
# Once that is done, randomly find another nearby point so that the NPCs are spawned on different lanes.
    def get_npc_random_transform(self):
        ego_transform = self.ego_state.transform
        sx = ego_transform.position.x
        sy = ego_transform.position.y
        sz = ego_transform.position.z
        ry = ego_transform.rotation.y
        if ry < 0:
            ry = 360 + ry

        hfov = self.camera_intrinsics["horizontal_fov"]

        mindist = 0.0
        maxdist = 100.0
        dist = random.uniform(mindist, maxdist)
        angle = random.uniform(math.radians(ry - hfov / 2), math.radians(ry + hfov / 2))
        point = lgsvl.Vector(sx + dist * math.sin(angle), sy, sz + dist * math.cos(angle))

        transform = self.sim.map_point_on_lane(point)
        sx = transform.position.x
        sy = transform.position.y
        sz = transform.position.z

        mindist = 0.0
        maxdist = 10.0
        dist = random.uniform(mindist, maxdist)
        angle = math.radians(transform.rotation.y)
        point = lgsvl.Vector(sx - dist * math.cos(angle), sy, sz + dist * math.sin(angle))
        transform = self.sim.map_point_on_lane(point)

        return transform

# Removes all spawned NPCs
    def reset_npcs(self):
        for npc in self.npcs:
            self.sim.remove_agent(npc)
        self.npcs = []
        self.npcs_state = []

# Moves the EGO to the given transform
    def position_ego(self, transform):
        ego_state = self.ego.state
        ego_state.transform = transform
        self.ego.state = ego_state
        # cache the state for later queries
        self.ego_state = ego_state

# Creates a random number of NPCs
# Each NPC is randomly placed as long as the random position passes some checks
# This will timeout after 9 seconds
    def setup_npcs(self):
        self.reset_npcs()
        num_npcs = random.randint(1, 15)
        print("Placing {} NPCs...".format(num_npcs))
        t0 = time.time()
        while len(self.npcs) < num_npcs:
            if time.time() - t0 > 9:
                print("Timeout! Stop placing NPCs")
                break
            npc_transform = self.get_npc_random_transform()

            if not self.is_npc_in_fov(npc_transform):
                continue

            if self.is_npc_obscured(npc_transform):
                continue

            if self.is_npc_too_close(npc_transform):
                continue

            self.position_npc(npc_transform)
        print("Done placing {} NPCs ({:.3f} s)".format(len(self.npcs), time.time() - t0))

# Creates a random NPC type at the given location
    def position_npc(self, transform):
        npc_state = lgsvl.AgentState()
        npc_state.transform = transform
        available_npcs = ['Sedan', 'SUV', 'Jeep', 'HatchBack']  # 'SchoolBus', 'DeliveryTruck'
        npc_type = available_npcs[random.randint(0, len(available_npcs) - 1)]
        npc = self.sim.add_agent(npc_type, lgsvl.AgentType.NPC, npc_state)
        self.npcs.append(npc)
        self.npcs_state.append(npc_state)

# Checks if the given position is too close to the EGO
    def is_npc_too_close(self, npc_transform):
        for agent, agent_state in zip([self.ego] + self.npcs, [self.ego_state] + self.npcs_state):
            if abs(npc_transform.position.x - agent_state.transform.position.x) < 5 and abs(npc_transform.position.z - agent_state.transform.position.z) < 5:
                return True

        return False

# Checks if anything between the EGO and given position gets in the way of the camera
    def is_npc_obscured(self, npc_transform):
        lidar_mat = np.dot(transform_to_matrix(self.sensor_lidar.transform), transform_to_matrix(self.ego_state.transform))
        start = lgsvl.Vector(
            lidar_mat[3][0],
            lidar_mat[3][1],
            lidar_mat[3][2],
        )
        end = npc_transform.position
        direction = lgsvl.Vector(
            end.x - start.x,
            end.y - start.y,
            end.z - start.z,
        )
        distance = np.linalg.norm(np.array([direction.x, direction.y, direction.z]))
        layer_mask = 0
        for bit in [0]:
            layer_mask |= 1 << bit

        hit = self.sim.raycast(start, direction, layer_mask, distance)
        if hit:
            return True

        return False

# Checks if the given position is in the view of the EGO camera
    def is_npc_in_fov(self, npc_transform):
        v0 = [0, 0, 1]
        v1 = [
            npc_transform.position.x - self.ego_state.transform.position.x,
            0,
            npc_transform.position.z - self.ego_state.transform.position.z,
        ]
        cos_ang = np.dot(v0, v1)
        sin_ang = np.linalg.norm(np.cross(v0, v1))
        theta = math.degrees(np.arctan2(sin_ang, cos_ang))
        if v1[0] < 0:
            theta = 360 - theta
        ry = self.ego_state.transform.rotation.y
        if ry < 0:
            ry = 360 + ry
        hfov = self.camera_intrinsics["horizontal_fov"]
        angle = abs(ry - theta)
        if angle < hfov / 2:
            return True

        return False

# Saves camera, lidar, ground truth, and calibration data
    def capture_data(self):
        if len(self.npcs) == 0:
            print("No NPCs! Skip frame.")
            return

        self.save_camera_image()
        self.save_lidar_point()
        self.save_calibration()
        self.save_ground_truth()

        self.idx += 1

# Saves a camera image from the EGO main camera as a png
    def save_camera_image(self):
        if self.sensor_camera:
            t0 = time.time()
            out_file = os.path.join(IMAGE_JPG_PATH, self.get_filename("jpg"))
            self.sensor_camera.save(out_file, quality=100)
            im = Image.open(out_file)
            png_file = os.path.join(IMAGE_PNG_PATH, self.get_filename("png"))
            im.save(png_file)
            print("{} ({:.3f} s)".format(out_file, time.time() - t0))
        else:
            print("Warn: Camera sensor is not available")

# Saves a LIDAR scan from the EGO as a bin
    def save_lidar_point(self):
        if self.sensor_lidar:
            t0 = time.time()
            pcd_file = os.path.join(LIDAR_PCD_PATH, self.get_filename("pcd"))
            self.sensor_lidar.save(pcd_file)
            with open(pcd_file, "rb") as f:
                pc = self.parse_pcd_file(f)
            bin_file = os.path.join(LIDAR_BIN_PATH, self.get_filename("bin"))
            pc.tofile(bin_file)
            print("{} ({:.3f} s)".format(bin_file, time.time() - t0))
        else:
            print("Warn: Lidar sensor is not available")

# Converts the lidar PCD to binary which is required for KITTI
    def parse_pcd_file(self, pcd_file):
        header = {}
        while True:
            ln = pcd_file.readline().strip()
            field = ln.decode('ascii').split(' ', 1)
            header[field[0]] = field[1]
            if ln.startswith(b"DATA"):
                break

        dtype = np.dtype([
            ('x', np.float32),
            ('y', np.float32),
            ('z', np.float32),
            ('intensity', np.uint8),
        ])
        size = int(header['POINTS']) * dtype.itemsize
        buf = pcd_file.read(size)
        lst = np.frombuffer(buf, dtype).tolist()
        out = []
        for row in lst:
            out.append((row[0], row[1], row[2], row[3] / 255))
        pc = np.array(out).astype(np.float32)

        return pc

# Calculates the calibration values between various sensors
    def calibrate(self):
        if self.sensor_camera and self.sensor_lidar:
            self.camera_intrinsics, self.projection_matrix, self.rectification_matrix = self.get_camera_intrinsics(self.sensor_camera)

            # Coordinate systems
            # - Unity:    x: right,   y: up,    z: forward (left-handed)
            # - Kitti: (right-handed)
            #   - Camera:   x: right,   y: down,  z: forward
            #   - Velodyne: x: forward, y: left,  z: up
            #   - GPS/IMU:  x: forward, y: left,  z: up

            # Velodyne to Camera
            diff_x = self.sensor_lidar.transform.position.x - self.sensor_camera.transform.position.x
            diff_y = -(self.sensor_lidar.transform.position.y - self.sensor_camera.transform.position.y)
            diff_z = self.sensor_lidar.transform.position.z - self.sensor_camera.transform.position.z
            self.tr_velo_to_cam = np.array([0, -1, 0, diff_x, 0, 0, -1, diff_y, 1, 0, 0, diff_z])  # Rotation: x: 90, y: 0, z: 90

            # IMU to Camera
            diff_x = self.sensor_imu.transform.position.z - self.sensor_lidar.transform.position.z
            diff_y = -(self.sensor_imu.transform.position.x - self.sensor_lidar.transform.position.x)
            diff_z = self.sensor_imu.transform.position.y - self.sensor_lidar.transform.position.y
            self.tr_imu_to_velo = np.array([1, 0, 0, diff_x, 0, 1, 0, diff_y, 0, 0, 1, diff_z])  # Rotation: x: 0, y: 0, z: 0
        else:
            print("Warn: Sensors for calibration are not available!")

# Calculates various camera properties
    def get_camera_intrinsics(self, sensor_camera):
        image_width = sensor_camera.width
        image_height = sensor_camera.height
        aspect_ratio = image_width / image_height
        vertical_fov = sensor_camera.fov
        horizon_fov = 2 * math.degrees(math.atan(math.tan(math.radians(vertical_fov) / 2) * aspect_ratio))
        fx = image_width / (2 * math.tan(0.5 * math.radians(horizon_fov)))
        fy = image_height / (2 * math.tan(0.5 * math.radians(vertical_fov)))
        cx = image_width / 2
        cy = image_height / 2

        camera_info = {}
        camera_info["image_width"] = image_width
        camera_info["image_height"] = image_height
        camera_info["aspect_ratio"] = aspect_ratio
        camera_info["vertical_fov"] = vertical_fov
        camera_info["horizontal_fov"] = horizon_fov
        camera_info["fx"] = fx
        camera_info["fy"] = fy
        camera_info["cx"] = cx
        camera_info["cy"] = cy

        projection_matrix = [
            camera_info["fx"], 0.0, camera_info["cx"], 0.0,
            0.0, camera_info["fy"], camera_info["cy"], 0.0,
            0.0, 0.0, 1.0, 0.0,
        ]

        rectification_matrix = [
            1.0, 0.0, 0.0,
            0.0, 1.0, 0.0,
            0.0, 0.0, 1.0,
        ]

        return camera_info, projection_matrix, rectification_matrix

    def get_transform(self, parent_tf, child_tf):
        tf = np.dot(transform_to_matrix(child_tf), np.linalg.inv(transform_to_matrix(parent_tf)))
        tf = np.array(tf)
        tf[:, 3] = tf[3, :]
        tf = tf[:3, :]
        tf_flatten = tf.flatten()

        return tf_flatten

# Saves the sensor calibration data
    def save_calibration(self):
        t0 = time.time()
        if self.camera_intrinsics is None or self.tr_velo_to_cam is None or self.tr_imu_to_velo is None:
            self.calibrate()
        txt_file = os.path.join(CALIB_PATH, self.get_filename("txt"))
        with open(txt_file, "w") as f:
            f.write("P0: {}\n".format(" ".join(str(e) for e in self.projection_matrix)))
            f.write("P1: {}\n".format(" ".join(str(e) for e in self.projection_matrix)))
            f.write("P2: {}\n".format(" ".join(str(e) for e in self.projection_matrix)))
            f.write("P3: {}\n".format(" ".join(str(e) for e in self.projection_matrix)))
            f.write("R0_rect: {}\n".format(" ".join(str(e) for e in self.rectification_matrix)))
            f.write("Tr_velo_to_cam: {}\n".format(" ".join(str(e) for e in self.tr_velo_to_cam)))
            f.write("Tr_imu_to_velo: {}\n".format(" ".join(str(e) for e in self.tr_imu_to_velo)))
            print("{} ({:.3f} s)".format(txt_file, time.time() - t0))

# Returns the current filename given an extension
    def get_filename(self, ext):
        return "{:06d}.{}".format(self.idx, ext)

# Converts the world space position of the NPC into the EGO camera space
    def get_npc_tf_in_cam_space(self, npc_transform, tf_mat):
        npc_tf = np.dot(transform_to_matrix(npc_transform), tf_mat)

        return npc_tf

# Returns a vector from the EGO camera to the NPC of the input transform
    def get_location(self, transform):
        location = (transform[3][0], -transform[3][1], transform[3][2])

        return location

# Returns the rotation along the y axis (up) of an NPC in the camera space. 0 is when the NPC is facing to the right
    def get_rotation_y(self, transform):
        rotation_y = np.arctan2(transform[2][0], transform[0][0]) - (np.pi / 2)
        if rotation_y < -np.pi:
            rotation_y += 2 * np.pi

        return rotation_y

# Alpha takes into account the relative position of the NPC and it's rotation to calculate a different kind of rotation
# KITTI expects alpha and rotation_y separately. See KITTI readme.txt for a more detailed explanation
    def get_alpha(self, location, rotation_y):
        v0 = [0, 0, 1]
        v1 = [location[0], 0, location[2]]
        cos_ang = np.dot(v0, v1)
        sin_ang = np.linalg.norm(np.cross(v0, v1))
        theta = np.arctan2(sin_ang, cos_ang)
        if location[0] > 0:
            theta *= -1
        alpha = rotation_y + theta

        return alpha

# Returns the dimensions of the given bounding box
    def get_dimension(self, bbox):
        dimension = bbox.size
        height = dimension.y
        width = dimension.x
        length = dimension.z

        return height, width, length

# Returns a bounding box around an NPC in the camera space
    def get_corners_3D(self, location, rotation_y, dimension):
        h, w, l = dimension[0], dimension[1], dimension[2]
        x_corners = [l/2, l/2, -l/2, -l/2, l/2, l/2, -l/2, -l/2]
        y_corners = [0, 0, 0, 0, -h, -h, -h, -h]
        z_corners = [w/2, -w/2, -w/2, w/2, w/2, -w/2, -w/2, w/2]

        rot_mat = [
            [math.cos(rotation_y), 0, math.sin(rotation_y)],
            [0, 1, 0],
            [-math.sin(rotation_y), 0, math.cos(rotation_y)],
        ]

        corners_3D = np.dot(rot_mat, [x_corners, y_corners, z_corners])
        corners_3D[0, :] += location[0]
        corners_3D[1, :] += location[1]
        corners_3D[2, :] += location[2]

        return corners_3D

# Projects the 3D bounding box to the 2D camera image
    def project_3D_to_2D(self, corners_3D):
        proj_mat = np.array(self.projection_matrix).reshape((3, 4))

        rect_3x3 = np.array(self.rectification_matrix).reshape((3, 3))
        rect_mat = np.zeros([4, 4], dtype=rect_3x3.dtype)
        rect_mat[3, 3] = 1
        rect_mat[:3, :3] = rect_3x3

        corners_2D = np.dot(rect_mat, np.vstack((corners_3D, np.ones([1, 8]))))
        corners_2D = np.dot(proj_mat, corners_2D)
        corners_2D[0, :] = corners_2D[0, :] / corners_2D[2, :]
        corners_2D[1, :] = corners_2D[1, :] / corners_2D[2, :]
        corners_2D = np.delete(corners_2D, (2), axis=0)

        return corners_2D

# Saves the ground truth data as a txt
    def save_ground_truth(self):
        t0 = time.time()
        labels = self.parse_ground_truth()
        txt_file = os.path.join(LABEL_PATH, self.get_filename("txt"))
        with open(txt_file, "w") as f:
            for label in labels:
                f.write("{}\n".format(label))
            print("{} ({:.3f} s)".format(txt_file, time.time() - t0))

# Iterates over every NPC and converts the ground truth box in KITTI format
    def parse_ground_truth(self):
        camera_mat = transform_to_matrix(self.sensor_camera.transform)
        ego_mat = transform_to_matrix(self.ego_state.transform)
        tf_mat = np.dot(np.linalg.inv(ego_mat), np.linalg.inv(camera_mat))

        labels = []
        for npc, npc_state in zip(self.npcs, self.npcs_state):
            npc_tf = self.get_npc_tf_in_cam_space(npc_state.transform, tf_mat)

            location = self.get_location(npc_tf)
            rotation_y = self.get_rotation_y(npc_tf)
            height, width, length = self.get_dimension(npc.bounding_box)
            alpha = self.get_alpha(location, rotation_y)

            corners_3D = self.get_corners_3D(location, rotation_y, (height, width, length))
            corners_2D = self.project_3D_to_2D(corners_3D)

            p_min, p_max = corners_2D[:, 0], corners_2D[:, 0]
            for i in range(corners_2D.shape[1]):
                p_min = np.minimum(p_min, corners_2D[:, i])
                p_max = np.maximum(p_max, corners_2D[:, i])

            left, top = p_min
            right, bottom = p_max

            label = "Car -1 -1 {:.2f} {:.2f} {:.2f} {:.2f} {:.2f} {:.2f} {:.2f} {:.2f} {:.2f} {:.2f} {:.2f} {:.2f}".format(alpha, left, top, right, bottom, height, width, length, location[0], location[1], location[2], rotation_y)
            labels.append(label)

        return labels


if __name__ == "__main__":
    if len(sys.argv) != 4:
        print("incorrect number of arguments")
        sys.exit()

# This can be editted to load whichever map and vehicle
    kitti = KittiParser("SanFrancisco", "XE_Rigged-lgsvl", startIndex)
    kitti.bootstrap()

    t00 = time.time()
    num_spawns = numDataPoints
    for i in range(num_spawns):
        t0 = time.time()
        print("\n{} / {}".format(i + 1, num_spawns))
        ego_transform = kitti.get_ego_random_transform()
        kitti.position_ego(ego_transform)
        kitti.setup_npcs()
        kitti.capture_data()

        print("Elapsed time per frame: {:.3f} s".format(time.time() - t0))
    print("\nTotal elapsed time for {} data points: {:.3f} s".format(num_spawns, time.time() - t00))
