# Changelog
All notable changes and release notes for LGSVL Simulator will be documented in this file.

## [2019.11] - 2019-11-19

### Added
 - OpenDrive 1.4 HD map import.
 - Rain drops on ground when it is raining.
 - Separated Apollo HD map export between version 3.0 and 5.0 to support 5.0 specific features.
 - Cache vehicle prefabs when loading same vehicle multiple times into simulation.
 - Ability to login to account via command-line.
 - Ability for vehicle to have multiple interior lights. Fixes #474.
 - Allow Color, Depth and Semantic cameras to have higher capture & publish rate.


### Changed
 - Fixed traffic lights signal colors on Shalun map.
 - Fixed exceptions when NPCs are despawned while still in intersection.
 - Fixed errors when adding pedestrian to map without NavMesh.
 - Fixed Lanelet2 boundary import and export.
 - Fixed wrong raycast layers in 2D Ground Truth sensor to detect if NPC is visible.
 - Fixed missing timestamp when publishing ROS/Cyber messages from 2D Ground Truth sensor.


## [2019.10] - 2019-10-28

### Added
 - Apollo HD map import
 - Accurate sun position in sky based of map location. Including time of sunrise and sunset.
 - Control calibration sensor to help calibrating AD stack control.
 - Ported Shalun map from previous non-HDRP simulator version.
 - Ground Truth sensor for traffic light.
 - Python API method to get controllable object by position.
 - Python API method to convert multiple map coordinates in single call.
 - Python API method to perform multiple ray-casts in single call.
 - Sensor for controlling vehicle with steering wheel (Logitech G920).
 - Platform independent asset bundles (Windows and Linux).
 - Allow to set custom data path for database and downloaded bundles.
 - Visualize data values for non-image senors (GPS, IMU, etc).
 - Populate scene with required objects when new scene is created in Unity Editor.


### Changed
 - Fixed exceptions in ROS Bridge where if it receives message on topic that it has not subscribed.
 - Fixed 3D Ground Truth sensor to report correct NPC orientation angles.
 - Fixed Radar sensor to visualize pedestrians.
 - Fixed Color camera to render mountains in BorregaAve.
 - Fixed EGO vehicle collision callback to Python API.
 - Fixed WebUI redirect loop that happens if you are logged out.
 - Fixed reported NPC vehicle speed. Fixes #347 and #317.
 - Fixed gear shifting for EGO vehicle control. Fixes #389.
 - Fixed NPC waypoint following where NPCs stopped if assigned speed is too low.
 - Fixed semantic segmentation for vehicles and pedestrians added with Python API.
 - Fixed ROS2 message publishing (seq field was missing in Header). Fixes #413.
 - Fixed issue with database on some non-English locales. Fixes #381.
 - Fixed point cloud generation in Unity Editor.
 - Fixed browser loosing cookie when session ends in WebUI.
 - Fixed slowness in Python API when running without access to Internet.
 - Fixed issue when multiple users could not use same map url.
 - Improved error messages when simulation fails to start.


## [2019.09] - 2019-09-06

### Added
 - Sensor visualization UI
 - HD map export to OpenDrive 1.4 format
 - ROS service support for ROS bridge
 - Python API to support more robust waypoints for NPC vehicles
 - Python API with ability to control traffic lights on map
 - Hyundai Nexo vehicle

### Changed
 - Improved NPC movement and right turns on red traffic light
 - Fixed NPC vehicle despawning logic so they don't get stuck in intersections
 - Change NPC vehicles colliders from box to mesh to improves collision precision
 - Updated generated protobuf message classes for latest Apollo 5.0
 - Fixed 3D Ground Truth message type for ROS
 - Fixed 3D and 2D Ground Truth bounding box locations


## [2019.07] - 2019-08-09

### Added
 - Separate Asset Bundles for environments and vehicles
 - Fully deterministic physics simulation
 - Faster-than-real-time capability with Python API
 - Lanelet2 HD map format import/export
 - Ability to edit sensor configuration dynamically
 - Multi-user support - account login allows different users to login to one machine running simulator
 - BorregasAve 3D environment as a default provided map
 - AutonomouStuff 3D environment as a default provided map of parking lot
 - SingleLaneRoad 3D environment as a default provided map
 - CubeTown 3D environment as a default provided map
 - Lexus RX and Lincoln MKZ vehicle support
 - LiDAR outputs intensity value based on reflectivity of material (instead of color)
 - Support for Apollo 5.0
 - Support for Autoware 1.12
 - Light reflections on road from wetness
 - Better sky rendering, including clouds
 - Ability to import point cloud files for visualization

### Changed
 - User interface - use web UI for main user interaction
 - Render pipeline - Unity High Definition Render Pipeline
 - Significantly improved LiDAR simulation performance using multithreading
 - Improved map annotation for easier use in Editor
 - Improved point cloud generation from 3D environment in Editor

### Removed
 - Support for Duckiebot, EP_rigged, SF_rigged, Tugbot vehicles
 - Support for SimpleLoop, SimpleMap, SimpleRoom, Duckietown, DuckieDownTown, SanFrancisco, Shalun maps


## [2019.05 and older]
Please see release notes for previous versions on our Github [releases](https://github.com/lgsvl/simulator/releases) page.