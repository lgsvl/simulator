# Changelog
All notable changes and release notes for LGSVL Simulator will be documented in this file.

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
 - Improved point cloud generation from 3D environment  in Editor

### Removed
 - Support for Duckiebot, EP_rigged, SF_rigged, Tugbot vehicles
 - Support for SimpleLoop, SimpleMap, SimpleRoom, Duckietown, DuckieDownTown, SanFrancisco, Shalun maps


## [2019.05 and older]
Please see release notes for previous versions on our Github [releases](https://github.com/lgsvl/simulator/releases) page.