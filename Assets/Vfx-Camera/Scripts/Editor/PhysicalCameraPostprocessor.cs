using UnityEngine;
using UnityEditor;
using Unity.Vfx.Cameras;
using Unity.Vfx.Cameras.Model;

public class PhysicalCameraPostprocessor : AssetPostprocessor
{
	void OnPreprocessModel()
	{
		var modelImporter = assetImporter as ModelImporter;
		if (modelImporter != null)
		{
			modelImporter.importCameras = true;
			modelImporter.extraUserProperties = new string[]
			{
				"FocalLength",
				"FilmWidth", "FilmHeight",
				"NearPlane", "FarPlane",
				"CameraProjectionType"
			};
		}
	}

	void OnPostprocessGameObjectWithUserProperties(GameObject go, string[] propNames, object[] propValues)
	{
		var camera = go.GetComponent<Camera>() as Camera;
		if (camera == null)
			return;

		var physicalCamera = go.GetComponent<PhysicalCamera>() as PhysicalCamera ??
		                     go.AddComponent<PhysicalCamera>() as PhysicalCamera;

		for (int i = 0; i < propNames.Length; i++)
		{
			switch (propNames[i])
			{
				case "FocalLength":
					// convert mm to m
					physicalCamera.Model.Lens.m_FocalLength = (float) propValues[i] / 1000f;
					break;
				case "FilmWidth":
					// convert inches to m
					physicalCamera.Model.Body.m_SensorWidth = (float) propValues[i] / 39.3701f;
					break;
				case "FilmHeight":
					// convert inches to m
					physicalCamera.Model.Body.m_SensorHeight = (float) propValues[i] / 39.3701f;
					break;
				case "NearPlane":
					// convert mm to m
					physicalCamera.Model.m_NearClippingPlane = (float) propValues[i] / 1000f;
					break;
				case "FarPlane":
					// convert mm to m
					physicalCamera.Model.m_FarClippingPlane = (float) propValues[i] / 1000f;
					break;
				case "CameraProjectionType":
					physicalCamera.Model.m_ProjectionMode = (int)propValues[i] == 1 ? ProjectionMode.Orthographic : ProjectionMode.Perspective;
					physicalCamera.Model.VerticalFOV = camera.fieldOfView;
					break;
			}
		}

		physicalCamera.Update();
	}
}
