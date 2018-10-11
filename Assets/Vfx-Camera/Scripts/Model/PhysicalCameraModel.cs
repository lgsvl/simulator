using System;
using UnityEngine;

namespace Unity.Vfx.Cameras.Model
{
	public enum ProjectionMode
	{
		Perspective,
		Orthographic,
	}

	[System.Serializable]
	public class PhysicalCameraModel
	{
		private MathematicalModel m_Rules;

		public ProjectionMode m_ProjectionMode;
		public float m_NearClippingPlane;
		public float m_FarClippingPlane;

		[SerializeField] private PhysicalCameraBodyModel m_Body;
		[SerializeField] private PhysicalCameraLensModel m_Lens;

		public PhysicalCameraBodyModel Body {
			get { return m_Body ?? (m_Body = new PhysicalCameraBodyModel()); }
		}
		public PhysicalCameraLensModel Lens
		{
			get { return m_Lens ?? (m_Lens = new PhysicalCameraLensModel()); }
		}

		public MathematicalModel Rules
		{
			get { return m_Rules ?? (m_Rules = new MathematicalModel()); } 
		}

		public float VerticalFOV
		{
			get { return Rules.VerticalFOV( this ); }
			set
			{
				Rules.ApplyVerticalFOV(value, this);
			}
		}

		public void SetDefaultValues()
		{
			m_ProjectionMode = ProjectionMode.Perspective;
			m_NearClippingPlane = 0.03f;
			m_FarClippingPlane = 1000f;

			Body.SetupDefaultValues();
			Lens.SetupDefaultValues();
		}

		public bool IsValid()
		{
			return m_Body.IsValid() && m_Lens.IsValid();
		}

		public PhysicalCameraModel()
		{
			SetDefaultValues();
		}
	}
}
