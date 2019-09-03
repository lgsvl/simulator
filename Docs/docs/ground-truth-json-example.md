# <a name="top"></a>Example JSON Configuration For Data Collection


### Bridge Type <sub><sup>[top](#top)</sup></sub> {: #bridge-type data-toc-label='Bridge Type'}

`CyberRT`

### Published Topics <sub><sup>[top](#top)</sup></sub> {: #published-topics data-toc-label='Published Topics'}

|Topic|Sensor Name|
|:-:|:-:|
|`/apollo/sensor/camera/front_6mm/image/compressed`|Main Camera|
|`/simulator/ground_truth/3d_detections`|3D Ground Truth|
|`/simulator/ground_truth/2d_detections`|2D Ground Truth|
|`/simulator/depth_camera`|Depth Camera|
|`/simulator/semantic_camera`|Semantic Camera|

### Subscribed Topics <sub><sup>[top](#top)</sup></sub> {: #subscribed-topics data-toc-label='Subscribed Topics'}

|Topic|Sensor Name|
|:-:|:-:|
|`/simulator/ground_truth/3d_visualize`|3D Ground Truth Visualizer|
|`/simulator/ground_truth/2d_visualize`|2D Ground Truth Visualizer|

### JSON Configuration <sub><sup>[top](#top)</sup></sub> {: #json-configuration data-toc-label='JSON Configuration'}

```JSON
[
  {
    "type": "Color Camera",
    "name": "Main Camera",
    "params": {
      "Width": 1920,
      "Height": 1080,
      "Frequency": 15,
      "JpegQuality": 75,
      "FieldOfView": 50,
      "MinDistance": 0.1,
      "MaxDistance": 1000,
      "Topic": "/apollo/sensor/camera/front_6mm/image/compressed"
    },
    "transform": {
      "x": 0,
      "y": 1.7,
      "z": -0.2,
      "pitch": 0,
      "yaw": 0,
      "roll": 0
    }
  },
  {
	"type": "3D Ground Truth",
	"name": "3D Ground Truth",
	"params": {
	  "Frequency": 10,
	  "Topic": "/simulator/ground_truth/3d_detections"
	},
	"transform": {
	  "x": 0,
	  "y": 1.975314,
	  "z": -0.3679201,
	  "pitch": 0,
      "yaw": 0,
      "roll": 0
	}
  },
  {
	"type": "3D Ground Truth Visualizer",
	"name": "3D Ground Truth Visualizer",
	"params": {
	  "Topic": "/simulator/ground_truth/3d_visualize"
	},
	"transform": {
	  "x": 0,
	  "y": 1.975314,
	  "z": -0.3679201,
	  "pitch": 0,
      "yaw": 0,
      "roll": 0
	}
  },
  {
	"type": "2D Ground Truth",
	"name": "2D Ground Truth",
	"params": {
	  "Frequency": 10,
	  "DetectionRange": 100,
	  "Topic": "/simulator/ground_truth/2d_detections"
	},
	"transform": {
	  "x": 0,
	  "y": 1.7,
	  "z": -0.2,
	  "pitch": 0,
      "yaw": 0,
      "roll": 0
	}
  },
  {
    "type": "2D Ground Truth Visualizer",
    "name": "2D Ground Truth Visualizer",
    "params": {
      "Width": 1920,
      "Height": 1080,
      "FieldOfView": 50,
      "MinDistance": 0.1,
      "MaxDistance": 1000,
      "Topic": "/simulator/ground_truth/2d_visualize"
    },
    "transform": {
      "x": 0,
      "y": 1.7,
      "z": -0.2,
      "pitch": 0,
      "yaw": 0,
      "roll": 0
    }
  },
  {
    "type": "Depth Camera",
    "name": "Depth Camera",
    "params": {
      "Width": 1920,
      "Height": 1080,
      "Frequency": 15,
      "JpegQuality": 75,
      "FieldOfView": 50,
      "MinDistance": 0.1,
      "MaxDistance": 1000,
      "Topic": "/simulator/depth_camera"
    },
    "transform": {
      "x": 0,
      "y": 1.7,
      "z": -0.2,
      "pitch": 0,
      "yaw": 0,
      "roll": 0
    }
  },
  {
    "type": "Semantic Camera",
    "name": "Semantic Camera",
    "params": {
      "Width": 1920,
      "Height": 1080,
      "Frequency": 15,
      "FieldOfView": 50,
      "MinDistance": 0.1,
      "MaxDistance": 1000,
      "Topic": "/simulator/semantic_camera"
    },
    "transform": {
      "x": 0,
      "y": 1.7,
      "z": -0.2,
      "pitch": 0,
      "yaw": 0,
      "roll": 0
    }
  },
  {
    "type": "Manual Control",
    "name": "Manual Car Control"
  }
]
```
