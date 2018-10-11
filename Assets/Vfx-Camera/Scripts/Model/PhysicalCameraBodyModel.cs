using UnityEngine;

namespace Unity.Vfx.Cameras.Model
{

	[System.Serializable]
	public class PhysicalCameraBodyModel
	{
		public float m_SensorWidth;
		public float m_SensorHeight;

		public PhysicalCameraBodyModel()
		{
			SetupDefaultValues();
		}

		public void SetupDefaultValues()
		{
			m_SensorWidth = 36/1000f;
			m_SensorHeight = 24/1000f;
		}

		public bool IsValid()
		{
			return m_SensorWidth > 0 && m_SensorHeight > 0;
		}

		public float AspectRatio
		{
			get { return m_SensorWidth/m_SensorHeight; }
			set
			{
				if (value != 0f)
					m_SensorHeight = m_SensorWidth / value;
				
			}
		}
	}
}
