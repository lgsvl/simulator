/*
* MIT License
* 
* Copyright (c) 2017 Philip Tibom, Jonathan Jansson, Rickard Laurenius, 
* Tobias Alldén, Martin Chemander, Sherry Davar
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/

#pragma warning disable 0414

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Connector class for connecting the point cloud to the lidar sensor, should be a component in the sensor.
///  @author: Tobias Alldén
/// </summary>
public class Connector : MonoBehaviour {

    public GameObject pointCloudBaseGameObject;
    public GameObject lidarGameObject;
    private PointCloud pointCloud;
    private LidarSensor lidarSensor;


	// Use this for initialization
	void Start () {
        pointCloudBaseGameObject = GameObject.FindGameObjectWithTag("PointCloudBase");
        pointCloud = pointCloudBaseGameObject.GetComponent<PointCloud>();
        lidarGameObject = GameObject.FindGameObjectWithTag("Lidar");
        lidarSensor = lidarGameObject.GetComponent<LidarSensor>();
	}
	
	/// <summary>
    /// Update method runs every iteration, tells the point cloud to update it's points.
    /// </summary>
	void Update () {
    }
}
