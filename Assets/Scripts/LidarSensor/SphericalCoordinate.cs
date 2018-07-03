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

using System;
using UnityEngine;

#pragma warning disable 0114

/// <summary>
/// A class representing spherical coordinates. These are created by the lidar sensor.
/// @author: Tobias Alldén
/// </summary>
public class SphericalCoordinate
{
    private Vector3 globalWorldCoordinate; // Useful for some things. The other coordinates are local.
    private int laserId;
    private float radius;
    private float inclination;
    private float azimuth;

    public SphericalCoordinate(float radius, float inclination, float azimuth, Vector3 globalWorldCoordinate, int laserId)
    {
        this.radius = radius;
        this.inclination = (90 + inclination)*(2*Mathf.PI/360);
        this.azimuth = azimuth * (2 * Mathf.PI / 360);
        this.globalWorldCoordinate = globalWorldCoordinate;
        this.laserId = laserId;
    }


    public SphericalCoordinate(float radius, float inclination, float azimuth)
    {
        this.radius = radius;
        this.inclination = (90+inclination)*(2*Mathf.PI/360);
        this.azimuth = azimuth/(2*Mathf.PI/360);
    }

    // Constructor based on cartesian coordinates
	/// <summary>
	/// Initializes a new instance of the <see cref="SphericalCoordinate"/> class using cartesian coordinates.
	/// </summary>
	/// <param name="coordinates">Coordinates.</param>
    public SphericalCoordinate(Vector3 coordinates)
    {
        globalWorldCoordinate = coordinates;

    }

    /// <summary>
    /// Converts a spherical coordinate to a cartesian equivalent. 
    /// </summary>
    /// <returns></returns>
    public Vector3 ToCartesian()
    {
        Vector3 cartesian = new Vector3();
        cartesian.z = radius * Mathf.Sin(inclination) * Mathf.Cos(azimuth);
        cartesian.x = radius * Mathf.Sin(inclination) * Mathf.Sin(azimuth);
        cartesian.y = radius * Mathf.Cos(inclination);
        return cartesian;
    }

	/// <summary>
	/// Gets the radius.
	/// </summary>
	/// <returns>The radius.</returns>
    public float GetRadius()
    {
        return this.radius;
    }
    /// <summary>
    /// Gets the inclination.
    /// </summary>
    /// <returns>The inclination.</returns>
    public float GetInclination()
    {
        return this.radius;
    }
    /// <summary>
    /// Gets the azimuth.
    /// </summary>
    /// <returns>The azimuth.</returns>
    public float GetAzimuth()
    {
        return this.azimuth;
    }

    public Vector3 GetWorldCoordinate()
    {
        return this.globalWorldCoordinate;
    }

    public int GetLaserId()
    {
        return this.laserId;
    }



    /// <summary>
    /// Clones this instance of the class
    /// </summary>
    /// <returns></returns>
    public SphericalCoordinate Clone()
    {
        return new SphericalCoordinate(this.radius, this.inclination, this.azimuth, 
            new Vector3(globalWorldCoordinate.x, globalWorldCoordinate.y, globalWorldCoordinate.z), this.laserId);
    }

    /// <summary>
    /// Overriding the equals method to be able to avoid float pooint errors.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object obj)
    {
        double eps = 0.01;
        SphericalCoordinate other = (SphericalCoordinate)obj;
        return (Math.Abs(this.azimuth - other.azimuth) < eps
            && Math.Abs(this.inclination - other.inclination) < eps
            && Math.Abs(this.radius - other.radius) < eps);
    }

    /// <summary>
    /// Override hash code
    /// </summary>
    /// <returns></returns>
	/*public String ToString() {
		return radius.ToString () + ";" + inclination.ToString () + ";" + azimuth.ToString () + ":::";
	}*/
	
    public override int GetHashCode()
    {
        return  (int)Math.Floor(azimuth * 3 + inclination * 13 + radius * 11);
    }

	public String ToString() {
		return "Radius: " + radius.ToString () + " Inclination: " + inclination.ToString () + " Azimuth: " + azimuth.ToString () +
		" World coordinates: " + globalWorldCoordinate.ToString () + " LaserID: " + laserId.ToString () + " ::: ";
	}
}