# Adding Assets [](#top)

The main repository for the LGSVL Simulator does not contain and environments or vehicles.
Currently there are several open-source examples.

Environments:

- [CubeTown](https://github.com/lgsvl/CubeTown)
- [SingleLaneRoad](https://github.com/lgsvl/SingleLaneRoad)
- [Shalun](https://github.com/lgsvl/Shalun)

Vehicles:

- [Jaguar2015XE](https://github.com/lgsvl/Jaguar2015XE)

<h2>Table of Contents</h2>
[TOC]

## Adding an Asset [[top]] {: #adding-an-asset data-toc-label='Adding an Asset'}
Assets need to be cloned into a specific location in the project:

- `simulator/Assets/External/Environments` for Environments
- `simulator/Assets/External/Vehicles` for Vehicles

Clone the desired asset into the appropriate folder. 
Do not change the name of the folder that the asset is cloned into, it must match the name of the asset.

For environments: `simulator/Assets/External/Environments/Mars` must contain `simulator/Assets/External/Environments/Mars/Mars.unity`

For vehicles: `simulator/Assets/External/Vehicles/Rover` must contain `simulator/Assets/External/Vehicles/Rover/Rover.prefab`

## Building an Asset [[top]] {: #building-an-asset data-toc-label='Building an Asset'}
Assets are built using the same build script as the simulator. Follow the [build instructions](build-instructions.md) through step 17.

**IMPORTANT** Windows and Linux support must be installed with Unity to build assetbundles

## Check Asset Consistency [[top]] {: #check-asset-consistency data-toc-label='Check Asset Consistency'}
There is a tool to check if there are any inconsistencies in assets.

Run `Check...` in `Unity`: `Simulator` -> `Check...`

The script checks if the project structure is correct: assets are named correctly, assets are in the correct location, etc. This will generate a list of warnings and errors.

[![](images/check-script-output.png)](images/check-script-output.png)