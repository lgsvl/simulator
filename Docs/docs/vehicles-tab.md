# <a name="top"></a>Web UI Vehicles Tab Explanation

A `Vehicle` can be in the following states. 

- A `Vehicle` with a local URL or if it has already been downloaded will have a `Valid` status.
- If the URL to the `Vehicle` assetbundle is not local and the assetbundle is not in the local database, then the assetbundle needs to be downloaded.
Currently only 1 assetbundle is downloaded at a time. 
	- If an assetbundle is downloading, the `Vehicle` will show a GREY dot and the status will be `Downloading` with the download percentage.
	- If another assetbundle is downloading, the icon will be ORANGE and the status will be `Downloading` without a percentage.
	- A downloading `Vehicle` can be interrupted by pressing the stop button.
- If the `Vehicle` is not usable in a Simulation it will have an `Invalid` status. This can be because the local assetbundle is not usable or the download was interrupted.

[![](images/web-vehicle-states.png)](images/full_size_images/web-vehicle-states.png)


### Where to find Vehicles [[top]] {: #where-to-find-vehicles data-toc-label='Where to find Vehicles'}
Vehicle assetbundles are available from our [content website](https://content.lgsvlsimulator.com/vehicles/).
When adding a vehicle, the link to the appropriate assetbundle can be entered as the URL or the assetbundle can be downloaded manually and the local path can be entered.

The calibration files for the vehicles are available in the same page. Please see the relevant doc for instructions on how to add a vehicle to an AD Stack:

- [Apollo 5.0](apollo5-0-instructions.md)
- [Apollo 3.0](apollo-instructions.md)
- [Autoware](autoware-instructions.md)

Example JSON configurations can be found on these pages:

- [Apollo 3.0 JSON](#apollo-json-example.md)
- [Apollo 5.0 JSON](#apollo5-0-json-example.md)
- [Autoware JSON](#autoware-json-example.md)

### How to add a Vehicle <sub><sup>[top](#top)</sup></sub> {: #how-to-add-a-vehicle data-toc-label='How to add a Vehicle'}
1. Click the `Add new` button
2. In the dialogue that opens, enter the name of the vehicle and the URL to the assetbundle. This can be a URL to a location in the cloud or to a location on a local drive.
3. If the URL is not local, the assetbundle will be downloaded to the local database.

[![](images/web-add-vehicle.png)](images/full_size_images/web-add-vehicle.png)

### How to Edit a Vehicle <sub><sup>[top](#top)</sup></sub> {: #how-to-edit-a-vehicle data-toc-label='How to Edit a Vehicle'}

1. Click the pencil icon
2. In the dialogue that opens, the name of the vehicle can be changed and the URL to the assetbundle.
3. If the URL is changed, the assetbundle in the database will be updated (downloaded if necessary)

[![](images/web-edit-vehicle.png)](images/full_size_images/web-edit-vehicle.png)

### How to Change the Configuration of a Vehicle <sub><sup>[top](#top)</sup></sub> {: #how-to-change-the-configuration-of-a-vehicle data-toc-label='How to Change the Configuration of a Vehicle'}

1. Click the wrench icon
2. In the dialogue that opens, the bridge type of the vehicle and the JSON configuration of the vehicle can be entered
3. A JSON beautifier is recommended to make the configuration more readable
4. The bridge type determines how the sensor data will be formatted and sent to an AD stack.
- All bridge types other than `No bridge` will require a `Bridge Connection String` when adding a vehicle to a simulation. 
This string includes the IP of the AD Stack and the open port (ex. `192.168.1.100:9090`)
5. The JSON determines what sensors are on the vehicle, where they are located, what topic they will publish data under, and what control inputs the vehicle accepts
- See below for an [example JSON configuration](#example-json)
6. Sample configuration JSON files are available on the [content website](https://content.lgsvlsimulator.com/vehicles/) to go with the provided assetbundles.


See [Sensor Parameters](sensor-json-options.md) for full defintions of all availble sensors and how to add them to a vehicle.

[![](images/web-configure-vehicle.png)](images/full_size_images/web-configure-vehicle.png)

### Bridge Types <sub><sup>[top](#top)</sup></sub> {: #bridge-types data-toc-label='Bridge Types'}
- `No bridge`: This is bridge available by default. Does not require any additional information while setting up Simulation. 
Used when there is no need to connect to an AD Stack.
- `ROS`: This bridge allows connecting to ROS1 based AV stacks. (like Autoware). 
ROS1 Bridge requires IP address and port number while setting up Simulation Configuration.
- `ROS Apollo`: This bridge allows connecting to ROS1 based AV stacks which requires protobuf message format. (like Apollo 3.0). 
ROS1 Apollo Bridge requires IP address and port number while setting up Simulation Configuration.
- `ROS2`: This bridge allows connecting to ROS2 based AV stacks. ROS2 Bridge requires IP address, port number while setting up Simulation Configuration.
- `CyberRT`: This bridge allows connections to Apollo 5.0. CyberRT Bridge requires IP address, port number while setting up Simulation Configuration.

### Example JSON <sub><sup>[top](#top)</sup></sub> {: #example-json data-toc-label='Example JSON'}
This is a shortened version of the JSON configuration on the `Jaguar2015XE (Autoware)` default vehicle. It uses a `ROS` bridge type.

The JSON includes a GPS sensor in the center of the vehicle that publishes data on the "/nmea_sentence" topic, 
a LIDAR sensor 2.312m above the center of the vehicle that publishes data on the "/points_raw" topic,
a Manual Control input which allows the keyboard input to control the car,
and a Vehicle Control input which subscribes to the Autoware AD Stack control commands.
```JSON
[
  {
    "type": "GPS Device",
    "name": "GPS",
    "params": {
      "Frequency": 12.5,
      "Topic": "/nmea_sentence",
      "Frame": "gps",
      "IgnoreMapOrigin": true
    },
    "transform": {
      "x": 0,
      "y": 0,
      "z": 0,
      "pitch": 0,
      "yaw": 0,
      "roll": 0
    }
  },
  {
    "type": "Lidar",
    "name": "Lidar",
    "params": {
      "LaserCount": 32,
      "MinDistance": 0.5,
      "MaxDistance": 100,
      "RotationFrequency": 10,
      "MeasurementsPerRotation": 360,
      "FieldOfView": 41.33,
      "CenterAngle": 10,
      "Compensated": true,
      "PointColor": "#ff000000",
      "Topic": "/points_raw",
      "Frame": "velodyne"
    },
    "transform": {
      "x": 0,
      "y": 2.312,
      "z": -0.3679201,
      "pitch": 0,
      "yaw": 0,
      "roll": 0
    }
  },
  {
    "type": "Manual Control",
    "name": "Manual Car Control"
  },
  {
    "type": "Vehicle Control",
    "name": "Autoware Car Control",
    "params": {
      "Topic": "/vehicle_cmd"
    }
  }
]
```
