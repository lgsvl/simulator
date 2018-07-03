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

using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controlls all aspects of the lidar mimic
/// 
/// @author: Jonathan Jansson
/// </summary>
public class PreviewLidarRays : MonoBehaviour {

	public GameObject lineDrawerPrefab;
    public Slider numberOfLasersSlider;

	private int numberOfLasers;
    private float upperFOV;
    private float lowerFOV;
    private float offsetCm;
    private float upperNormal;
    private float lowerNormal;

	private int maxLasers = 64;
	private List<LaserMimic> lasersMimics = new List<LaserMimic>();

    public static Action<bool> tellLidarMenuInitialized;
    /// <summary>
    /// Adds the method UpdateLidarValues to the event in the lidarMenu script to listen on parameter changes in the GUI
    /// </summary>
    void Awake()
    {
        LidarMenu.OnPassValuesToLaserMimic += UpdateLidarValues;
    }
    void OnDestroy()
    {
        LidarMenu.OnPassValuesToLaserMimic -= UpdateLidarValues;
    }

    void Start()
    {
        maxLasers = (int)numberOfLasersSlider.maxValue;
        InitializeLaserMimicList();
        try
        {
            tellLidarMenuInitialized(true);
        }
        catch (NullReferenceException e)
        {
            Debug.Log("Action not initialized: " + e);
        }
    }

	/// <summary>
    /// Initializing the lidar mimic with the maximum number of lasers set in the slider settings for number of lasers
    /// </summary>
	public void InitializeLaserMimicList()
    {
		for (int i = 0; i < maxLasers; i++)
		{
			GameObject lineDrawer = Instantiate(lineDrawerPrefab);
			lineDrawer.transform.parent = gameObject.transform;
			lineDrawer.transform.position = transform.position;
			lineDrawer.transform.rotation = transform.rotation;
			lasersMimics.Add(new LaserMimic(0, 0, lineDrawer, false));
		}
	}
    

    /// <summary>
    /// Updates all parameters of the lidar mimic
    /// </summary>
    /// <param name="numberOfLasers"></param>
    /// <param name="upperFOV"></param>
    /// <param name="lowerFOV"></param>
    /// <param name="offset"></param>
    /// <param name="upperNormal"></param>
    /// <param name="lowerNormal"></param>
    public void UpdateLidarValues(int numberOfLasers, float upperFOV, float lowerFOV, float offset, float upperNormal, float lowerNormal)
    {
		this.numberOfLasers = numberOfLasers;
		this.upperFOV = upperFOV;
		this.lowerFOV = lowerFOV;
		this.offsetCm = offset;
		this.upperNormal = upperNormal;
		this.lowerNormal = lowerNormal;

		UpdateLines ();
	}
		
    /// <summary>
    /// Updates the parameters of all laser mimics to new correct settings and disables all not used mimics
    /// </summary>
	public void UpdateLines()
    {
		float upperTotalAngle = upperFOV / 2;
		float lowerTotalAngle = lowerFOV / 2;
		float upperAngle = upperFOV / (numberOfLasers / 2);
		float lowerAngle = lowerFOV / (numberOfLasers / 2);
        float offset = (offsetCm / 100) / 2; // Converts offset FROM cm

		for (int i = 0; i < numberOfLasers; i++)
		{
			if (i < numberOfLasers/2)
			{
				lasersMimics[i].SetRayParameters(lowerTotalAngle + lowerNormal, -offset, transform);
				lasersMimics [i].SetActive(true);
				lowerTotalAngle -= lowerAngle;
			}
			else
			{
				lasersMimics[i].SetRayParameters(upperTotalAngle - upperNormal, offset, transform);
				lasersMimics [i].SetActive(true);
				upperTotalAngle -= upperAngle;
			}
		}

		for(int i = numberOfLasers; i < lasersMimics.Count; i++)
        {
			lasersMimics [i].SetActive (false);
		}

		DrawRays ();
	}

    /// <summary>
    /// Updates the visually drawn lines
    /// </summary>
	public void DrawRays()
    {
		foreach(LaserMimic lm in lasersMimics)
        {
            lm.DrawRay();
		}
	}

    /// <summary>
    /// A class which mimics the parameters of the actual lidar sensor and draws lines to represent the lasers of the lidar
    /// </summary>
	class LaserMimic {
        private GameObject lineDrawer;
		private RenderLine lineRenderer;
		private bool rayOn;
		private float rayDistance = 5f;
		

        /// <summary>
        /// Initializes all parameters of the laser mimic as correct values
        /// </summary>
        /// <param name="verticalAngle"></param>
        /// <param name="offset"></param>
        /// <param name="lineDrawer"></param>
        /// <param name="rayOn"></param>
		public LaserMimic(float verticalAngle, float offset, GameObject lineDrawer, bool rayOn)
        {
			this.lineDrawer = lineDrawer;
			lineRenderer = lineDrawer.GetComponent<RenderLine>();
			lineDrawer.transform.position = lineDrawer.transform.position + (lineDrawer.transform.up * offset);

			this.rayOn = rayOn;
		}

        /// <summary>
        /// Updates all parameters of the laser mimic
        /// </summary>
        /// <param name="verticalAngle"></param>
        /// <param name="offset"></param>
        /// <param name="baseTransform"></param>
		public void SetRayParameters(float verticalAngle, float offset, Transform baseTransform)
        {
			lineDrawer.transform.position = baseTransform.position + (baseTransform.up * offset);
			lineDrawer.transform.rotation = baseTransform.rotation;
			lineDrawer.transform.Rotate (new Vector3 (verticalAngle, 0, 0));
		}

        /// <summary>
        /// Draws visual lines according to the parameters of the laser mimic
        /// </summary>
		public void DrawRay()
        {
			if (rayOn)
            {
				lineRenderer.DrawLine (lineDrawer.transform.forward * rayDistance + lineRenderer.transform.position);
			}
            else
            {
				lineRenderer.DrawLine (lineDrawer.transform.position);
			}
		}

        public void SetActive(bool state)
        {
            rayOn = state;
        }

        public GameObject GetLineDrawerGameObject()
		{
			return lineDrawer;
		}	
	}
}
