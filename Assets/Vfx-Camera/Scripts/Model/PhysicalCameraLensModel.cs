using UnityEngine;

namespace Unity.Vfx.Cameras.Model
{

	[System.Serializable]
	public class PhysicalCameraLensModel
	{
		public float m_FocalLength;

		public void SetupDefaultValues()
		{
			m_FocalLength = 24/1000f;
		}

		public bool IsValid()
		{
			return m_FocalLength > 0;
		}

		public PhysicalCameraLensModel()
		{
			SetupDefaultValues();
		}
	}
}
