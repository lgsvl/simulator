<span id=vlp-16>
### Velodyne VLP-16[](#top)

```JSON
[
	{
		"type": "Lidar",
		"name": "Velodyne VLP-16",
		"params": {
		  "LaserCount": 16,
		  "MinDistance": 0.5,
		  "MaxDistance": 100,
		  "RotationFrequency": 10,
		  "MeasurementsPerRotation": 1800,
		  "FieldOfView": 30,
		  "CenterAngle": 0,
		  "Compensated": true,
		  "PointColor": "#ff000000",
		  "Topic": "/point_cloud",
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
	}
]
```

<span id=vlp-32c>
### Velodyne VLP-32C [](#top)

```JSON
[
	{
		"type": "Lidar",
		"name": "Velodyne VLP-32C",
		"params": {
		  "VerticalRayAngles": [
			-25.0,   -1.0,    -1.667,  -15.639,
			-11.31,   0.0,    -0.667,   -8.843,
			-7.254,  0.333,  -0.333,   -6.148,
			-5.333,  1.333,   0.667,   -4.0,
			-4.667,  1.667,   1.0,     -3.667,
			-3.333,  3.333,   2.333,   -2.667,
			-3.0,    7.0,     4.667,   -2.333,
			-2.0,   15.0,    10.333,   -1.333
		  ],
		  "MinDistance": 0.5,
		  "MaxDistance": 100,
		  "RotationFrequency": 10,
		  "MeasurementsPerRotation": 1800,
		  "Compensated": true,
		  "PointColor": "#ff000000",
		  "Topic": "/point_cloud",
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
	}
]
```

<span id=vls-128>
### Velodyne VLS-128 [](#top)

```JSON
[
	{
		"type": "Lidar",
		"name": "Velodyne VLS-128",
		"params": {
		  "VerticalRayAngles": [
            -11.742, -1.99, 3.4, -5.29, -0.78, 4.61, -4.08, 1.31,
            -6.5, -1.11, 4.28, -4.41, 0.1, 6.48, -3.2, 2.19,
            -3.86, 1.53, -9.244, -1.77, 2.74, -5.95, -0.56, 4.83,
            -2.98, 2.41, -6.28, -0.89, 3.62, -5.07, 0.32, 7.58,
            -0.34, 5.18, -3.64, 1.75, -25, -2.43, 2.96, -5.73,
            0.54, 9.7, -2.76, 2.63, -7.65, -1.55, 3.84, -4.85,
            3.188, -5.51, -0.12, 5.73, -4.3, 1.09, -16.042, -2.21,
            4.06, -4.63, 0.76, 15, -3.42, 1.97, -6.85, -1.33,
            -5.62, -0.23, 5.43, -3.53, 0.98, -19.582, -2.32, 3.07,
            -4.74, 0.65, 11.75, -2.65, 1.86, -7.15, -1.44, 3.95,
            -2.1, 3.29, -5.4, -0.01, 4.5, -4.19, 1.2, -13.565,
            -1.22, 4.17, -4.52, 0.87, 6.08, -3.31, 2.08, -6.65,
            1.42, -10.346, -1.88, 3.51, -6.06, -0.67, 4.72, -3.97,
            2.3, -6.39, -1, 4.39, -5.18, 0.21, 6.98, -3.09,
            4.98, -3.75, 1.64, -8.352, -2.54, 2.85, -5.84, -0.45,
            8.43, -2.87, 2.52, -6.17, -1.66, 3.73, -4.96, 0.43
		  ],
		  "MinDistance": 0.5,
		  "MaxDistance": 100,
		  "RotationFrequency": 10,
		  "MeasurementsPerRotation": 1800,
		  "Compensated": true,
		  "PointColor": "#ff000000",
		  "Topic": "/point_cloud",
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
	}
]
```