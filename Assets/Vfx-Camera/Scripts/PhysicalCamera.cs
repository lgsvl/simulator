using UnityEngine;
using System.Collections;
using Unity.Vfx.Cameras.Model;

namespace Unity.Vfx.Cameras
{

	public enum PhysicalCameraMode
	{
		Controller,
		Slave
	}

	[AddComponentMenu("Camera/Physical Camera")]
	[ExecuteInEditMode]
	public class PhysicalCamera : MonoBehaviour
	{
		public PhysicalCameraMode m_Mode;

		[SerializeField] private PhysicalCameraModel m_Model;
		public PhysicalCameraModel Model
		{
			get
			{
				return m_Model ?? (m_Model = new PhysicalCameraModel());
			}
		}

		[Tooltip("The render camera component that is linked to this physical camera model.")]
		public Camera m_AssociatedCameraObj;


		public void Update()
		{
			if (m_AssociatedCameraObj == null)
			{
				var camera = transform.GetComponentInParent<Camera>();
				if (camera != null)
					m_AssociatedCameraObj = camera;
				else
					return;
			}

			if (m_Mode == PhysicalCameraMode.Controller)
			{
				if (m_AssociatedCameraObj.orthographic != (Model.m_ProjectionMode == ProjectionMode.Orthographic))
					m_AssociatedCameraObj.orthographic = Model.m_ProjectionMode == ProjectionMode.Orthographic;

				if (m_AssociatedCameraObj.fieldOfView != Model.VerticalFOV)
					m_AssociatedCameraObj.fieldOfView = Model.VerticalFOV;

				if (m_AssociatedCameraObj.nearClipPlane != Model.m_NearClippingPlane)
					m_AssociatedCameraObj.nearClipPlane = Model.m_NearClippingPlane;

				if (m_AssociatedCameraObj.farClipPlane != Model.m_FarClippingPlane)
					m_AssociatedCameraObj.farClipPlane = Model.m_FarClippingPlane;
			}
			else
			{
				if (m_AssociatedCameraObj.orthographic != (Model.m_ProjectionMode == ProjectionMode.Orthographic))
				{
					Model.m_ProjectionMode = m_AssociatedCameraObj.orthographic
						? ProjectionMode.Orthographic
						: ProjectionMode.Perspective;
				}

				if (m_AssociatedCameraObj.fieldOfView != Model.VerticalFOV)
					Model.VerticalFOV = m_AssociatedCameraObj.fieldOfView;

				if (m_AssociatedCameraObj.nearClipPlane != Model.m_NearClippingPlane)
					Model.m_NearClippingPlane = m_AssociatedCameraObj.nearClipPlane;

				if (m_AssociatedCameraObj.farClipPlane != Model.m_FarClippingPlane)
					Model.m_FarClippingPlane = m_AssociatedCameraObj.farClipPlane;
			}
		}
	}
}
