using System;
using UnityEngine;
using System.Collections;
using Unity.Vfx.Cameras.Model;

namespace Unity.Vfx.Cameras.Model
{

	/// <summary>
	/// Thin lense assumptions!
	/// </summary>
	public class MathematicalModel
	{
		public virtual float MaxVFOV { get { return 179f; } }
		public virtual float MinVFOV { get { return 1f; } }

		public virtual float MaxAspectRatio(PhysicalCameraModel camera)
		{
			return camera.Body.m_SensorWidth/(ToRad(MaxVFOV)*camera.Lens.m_FocalLength);
		}

		public float ClampVerticalFOV( float fov )
		{
			return fov < MinVFOV ? MinVFOV : fov > MaxVFOV ? MaxVFOV : fov;
		}


		public virtual void ApplyVerticalFOV(float afov, PhysicalCameraModel camera)
		{
			camera.Lens.m_FocalLength = camera.Body.m_SensorHeight / ToRad(afov);
		}

		public float VerticalFOV(PhysicalCameraModel camera )
		{
			return ToDeg(camera.Body.m_SensorHeight / camera.Lens.m_FocalLength);
		}

		private float ToRad(float rads)
		{
			return (float)(Math.PI * rads / 180.0);
		}

		private float ToDeg(float degrees)
		{
			return (float)(degrees * 180 / Math.PI);
		}



	}

}
