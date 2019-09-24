# <a name="top"></a>Simulation Menu
When in a non-Headless Simulation, a menu can be accessed by clicking on the "hamburger" menu icon in the bottom left.

In an Interactive Simulation, the "Play" and "Pause" buttons are found to the right of the menu icon.

[![](images/simulation-screen.png)](images/full_size_images/simulation-screen.png)

<h2>Table of Contents</h2>
[TOC]

## Info Menu [[top]] {: #info-menu data-toc-label='Info Menu'}
This menu is accessed from the `i` button. 
It lists the build info as well as any errors, logs, or warnings that are created in the current simulation. 
To clear all of these messages, click the "Trash can" icon in the bottom right of the menu.

[![](images/info-menu.png)](images/full_size_images/info-menu.png)

## Controls Menu [[top]] {: #controls-menu data-toc-label='Controls Menu'}
This menu is accessed from the "controller" button. 
It lists all the keyboard commands in the simulation. 
See [Keyboard Shortcuts](keyboard-shortcuts.md) for more details.

[![](images/controls-menu.png)](images/full_size_images/controls-menu.png)

## Interactive Menu [[top]] {: #interactive-menu data-toc-label='Interactive Menu'}
This menu is only available in an Interactive Simulation and is accessed from the "sliders" button. 
It contains tools to change the environment of the Simulation while the Simulation is playing.

[![](images/interactive-menu.png)](images/full_size_images/interactive-menu.png)

## Sensors Menu [[top]] {: #sensors-menu data-toc-label='Sensors Menu'}
This menu is accessed from the "eye" button. 
It lists all sensors on the selected vehicle and allows for the sensors to be visualized. 
See [Sensor Visualization](sensor-visualizers.md) for more details.

[![](images/visualizer-menu.png)](images/full_size_images/visualizer-menu.png)

## Bridge Menu [[top]] {: #bridge-menu data-toc-label='Bridge Menu'}
This menu is accessed from the "plug" button. 
It lists information on the bridge status as well as all published and subscribed topics. 
See [Bridge Topics](bridge-connection-ui.md) for more details.

[![](images/bridge-ui.png)](images/full_size_images/bridge-ui.png)

## Camera Button [[top]] {: #camera-button data-toc-label='Camera Button'}
The camera icon in the bottom right indicates if the view is currently a follow camera or free-roam camera. 

[![Follow Camera](images/locked-camera.png)](images/full_size_images/locked-camera.png)

A follow camera remains centered on the selected vehicle. `W` and `S` zoom and `Mouse RightClick` rotates the view around the center of the car. 
If the view has not been manually rotated, the camera will stay behind the vehicle.

[![Free-roam Camera](images/unlocked-camera.png)](images/full_size_images/unlocked-camera.png)

A free-roam camera can be moved freely around the map using all of the camera controls.

Clicking the camera button toggles between the 2 camera modes. 
When switching back to the follow camera, the camera will automatically be positioned behind the active vehicle.

## Vehicle Selection [[top]] {: #vehicle-selection data-toc-label='Vehicle Selection'}
The vehicle listed in the bottom right is the current active vehicle. 
This vehicle is affected by keyboard input. 
Selecting a different vehicle will change the view to the follow camera of the selected vehicle. 
The number preceding each vehicle corresponds to the number key to select the vehicle.

[![](images/vehicle-selection.png)](images/full_size_images/vehicle-selection.png)