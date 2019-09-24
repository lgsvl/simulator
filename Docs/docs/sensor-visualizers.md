# <a name="top"></a>Sensor Visualizers
When in a non-Headless Simulation, sensor visualizers can be toggled from the menu. To visualize a sensor, click the "eye" next to the sensor name.

[![](images/visualizer-menu.png)](images/full_size_images/visualizer-menu.png)

Sensors are identified by the `name` parameter from the JSON configuration. 
For full details on the possible JSON parameters see [Sensor Parameters](sensor-json-options.md)
Not all sensors have visualizations available, only sensors who have will show their visualizations.

<h2>Table of Contents</h2>
[TOC]


## Cameras [[top]] {: #cameras data-toc-label='Cameras'}
When a camera is visualized, the image the sensor see is visualized in a window. 
This window can be resized by clicking-and-dragging the icon in the bottom right corner and can be made full-screen with the box icon in the top right corner. 
The window can be moved by clicking-and-dragging the top bar. To close the window, either click the `X` or click the "eye" again.

### Color Camera [[top]] {: #color-camera data-toc-label='Color Camera'}
Visualized Color camera shows the same things that are visible from the normal follow and free cameras, but from the perpsective defined in the JSON configuration.

[![](images/color-camera-visualized.png)](images/full_size_images/color-camera-visualized.png)

### Depth Camera [[top]] {: #depth-camera data-toc-label='Depth Camera'}
Visualized Depth camera shows objects colored on a grayscale based on the distance between the camera and the object.

[![](images/depth-camera-visualized.png)](images/full_size_images/depth-camera-visualized.png)

### Semantic Camera [[top]] {: #semantic-camera data-toc-label='Semantic Camera'}
Visualized Semantic camera shows objects colored according to the object tag.

|Tag|Color|Hex Value|
|:-:|:-:|:-:|
|Car|Blue|#120E97|
|Road|Purple|#7A3F83|
|Sidewalk|Orange|#BA8350|
|Vegetation|Green|#71C02F|
|Obstacle|White|#FFFFFF|
|TrafficLight|Yellow|#FFFF00|
|Building|Turquoise|#238688|
|Sign|Dark Yellow|#C0C000|
|Shoulder|Pink|#FF00FF|
|Pedestrian|Red|#FF0000|
|Curb|Dark Purple|#4A254F|

[![](images/semantic-visualized.png)](images/full_size_images/semantic-visualized.png)

### 2D Ground Truth [[top]] {: #2d-ground-truth data-toc-label='2D Ground Truth'}
Visualized 2D Ground Truth shows the same things as a color camera except pedestrians are enclosed in a yellow wire box and NPCs are enclosed in a green wire box.

[![](images/2d-ground-truth-visualized.png)](images/full_size_images/2d-ground-truth-visualized.png)

## Lidar [[top]] {: #lidar data-toc-label='Lidar'}
Visualized Lidar shows the point cloud that is detected.

[![](images/lidar-visualized.png)](images/full_size_images/lidar-visualized.png)

## Radar [[top]] {: #radar data-toc-label='Radar'}
Visualized Radar shows the radar cones and creates wireframe boxes enclosing NPCs ni a green box, bicycles in a cyan box, and other EGOs in a magenta box.

[![](images/radar-visualized.png)](images/full_size_images/radar-visualized.png)

## 3D Ground Truth [[top]] {: #3d-ground-truth data-toc-label='3D Ground Truth'}
Visualized 3D Ground Truth creates wireframe boxes enclosing pedestrians in a yellow box and NPCs in a green box.

[![](images/visualizer-menu.png)](images/full_size_images/visualizer-menu.png)
