using System;
using UnityEngine;
using UnityEditor;
using Unity.Vfx.Cameras.Model;

namespace Unity.Vfx.Cameras.Editor
{
	[ExecuteInEditMode]
	[CustomEditor(typeof(PhysicalCamera))]
	public class PhysicalCameraEditor : UnityEditor.Editor
	{
		public bool m_ShowCamera = true;
		public bool m_showLens = true;
		public bool m_showBody = true;

		public override void OnInspectorGUI()
		{
			var physCamera = this.serializedObject;
			EditorGUI.BeginChangeCheck();
			physCamera.Update();

			// Start --------------------------------------------------------
			var camObj = serializedObject.targetObject as PhysicalCamera;
			var property = this.serializedObject.FindProperty(() => camObj.m_Mode);
			AddEnumPopup(property, "Mode", "Determines if the Physical Camera controls the attached camera or reads from it.", typeof(PhysicalCameraMode));

			property = this.serializedObject.FindProperty( ()=>camObj.m_AssociatedCameraObj );
			EditorGUILayout.PropertyField(property);

			// models
			var camModel = this.serializedObject.FindProperty("m_Model");
			var lensModel = camModel.FindPropertyRelative("m_Lens");
			var bodyModel = camModel.FindPropertyRelative("m_Body");

			m_ShowCamera = EditorGUILayout.Foldout(m_ShowCamera, "Camera");
			if (m_ShowCamera)
				DrawCameraModel(camModel, lensModel);

			m_showLens = EditorGUILayout.Foldout(m_showLens, "Lens");
			if (m_showLens)
				DrawLensModel(lensModel);

			m_showBody = EditorGUILayout.Foldout(m_showBody, "Body");
			if (m_showBody)
				DrawBodyModel(bodyModel);

			// Done -------------------------------------------------------------
			physCamera.ApplyModifiedProperties();
			EditorGUI.EndChangeCheck();
		}

		private void DrawCameraModel(SerializedProperty camModel, SerializedProperty lensModel)
		{
			var camObj = serializedObject.targetObject as PhysicalCamera;
			var camModelObj = camObj.Model;

			// Projection
			{
				var projProp = camModel.FindPropertyRelative( () => camModelObj.m_ProjectionMode);

				Rect ourRect = EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginProperty(ourRect, GUIContent.none, projProp);

				int value = projProp.intValue;
				string[] enumNamesList = new []
				{
					ProjectionMode.Perspective.ToString(),
					ProjectionMode.Orthographic.ToString(),
				};

				var newValue = EditorGUILayout.Popup("Projection", value, enumNamesList);
				if (newValue != value)
				{
					projProp.intValue = newValue & 1;

				}
				EditorGUI.EndProperty();
				EditorGUILayout.EndHorizontal();

			}

			// Fake property: FOV
			{
				EditorGUILayout.BeginHorizontal();
				GUIContent gc;

				if (camObj.Model.m_ProjectionMode < ProjectionMode.Orthographic)
					gc = new GUIContent("Vertical FOV", "Vertical angular field of view of the camera.");
				else
					gc = new GUIContent("Vertical FOV", "Vertical field of view of the camera.");

				var orgValue = camObj.Model.VerticalFOV;
				var newValue = EditorGUILayout.FloatField(gc, orgValue);
				newValue = camObj.Model.Rules.ClampVerticalFOV(newValue);

				if (orgValue != newValue)
				{
					var lens = camModelObj.Lens;
					var flenProp = lensModel.FindPropertyRelative( () => lens.m_FocalLength);
					camObj.Model.VerticalFOV = newValue;
					flenProp.floatValue = lens.m_FocalLength;
				}

				EditorGUILayout.EndHorizontal();
			}

			var property = camModel.FindPropertyRelative( () => camModelObj.m_NearClippingPlane );
			AddFloatProperty(property, "Near Clipping Plane", "Distance, from camera sensor, to the Near clipping plane.", (oldv, newv) =>
			{
				if (newv < 0.01f)
					newv = 0.01f;

				var far = camModel.FindPropertyRelative( () => camModelObj.m_FarClippingPlane );

				if (far.floatValue - 0.01f < newv)
					far.floatValue = newv + 0.01f;

				return newv;
			});

			property = camModel.FindPropertyRelative( () => camModelObj.m_FarClippingPlane );
			AddFloatProperty(property, "Far Clipping Plane", "Distance, from camera sensor, to the Far clipping plane.", (oldv, newv) => {
				if (newv < camModelObj.m_NearClippingPlane + 0.01f)
					return camModelObj.m_NearClippingPlane + 0.01f;
				else
					return newv;
			});
		}

		private void DrawBodyModel(SerializedProperty bodyModel)
		{
			var camObj = serializedObject.targetObject as PhysicalCamera;
			var bodyModelObj = camObj.Model.Body;

			var sensorWidthProperty = bodyModel.FindPropertyRelative (() => bodyModelObj.m_SensorWidth);
			AddFloatProperty (sensorWidthProperty, "Sensor Width", "Width, in millimeters, of the camera sensor.", (o, n) => n < 0.001f ? 0.001f : n > 0.1f ? 0.1f : n, 1000f);

			var sensorHeightProperty = bodyModel.FindPropertyRelative (() => bodyModelObj.m_SensorHeight);
			AddFloatProperty (sensorHeightProperty, "Sensor Height", "Height, in millimeters, of the camera sensor.", (o, n) => n < 0.001f ? 0.001f : n > 0.1f ? 0.1f : n, 1000f);

			// Fake property: Aspect Ratio
			{
				EditorGUILayout.BeginHorizontal ();
				var orgValue = camObj.Model.Body.AspectRatio;
				var newValue = EditorGUILayout.FloatField (new GUIContent ("Aspect ratio", "Aspect ratio of sensor: width over height"), orgValue);

				if (newValue < camObj.Model.Rules.MaxAspectRatio (camObj.Model))
					newValue = camObj.Model.Rules.MaxAspectRatio (camObj.Model);

				if (newValue > 20f)
					newValue = 20f;

				if (orgValue != newValue)
					camObj.Model.Body.AspectRatio = newValue;

				EditorGUILayout.EndHorizontal();
			}
		}

		private void DrawLensModel(SerializedProperty lensModel)
		{
			var camObj = serializedObject.targetObject as PhysicalCamera;
			var lensModelObj = camObj.Model.Lens;

			GUI.enabled = camObj.Model.m_ProjectionMode == ProjectionMode.Perspective;

			var property = lensModel.FindPropertyRelative( ()=> lensModelObj.m_FocalLength);
			AddFloatProperty(property, "Focal Length", "Focal length of the lens in millimeters.", (o, n) =>
			{
				if (n < 0.001f) n = 0.001f;
				return n;
			}, 1000f);

			GUI.enabled = true;
		}

		private delegate T OnValueChangedDelegate<T>(T oldValue, T newValue);

		void AddEnumPopup(SerializedProperty property, string text, string tooltip, Type typeOfEnum, OnValueChangedDelegate<int> onChange = null)
		{
			Rect ourRect = EditorGUILayout.BeginHorizontal();
			EditorGUI.BeginProperty(ourRect, GUIContent.none, property);

			int selectionFromInspector = property.intValue;

			string[] enumNamesList = System.Enum.GetNames(typeOfEnum);

			var actualSelected = EditorGUILayout.Popup(text, selectionFromInspector, enumNamesList);
			if (onChange != null && actualSelected != property.intValue)
				actualSelected = onChange(property.intValue, actualSelected);

			property.intValue = actualSelected;

			EditorGUI.EndProperty();
			EditorGUILayout.EndHorizontal();
		}

		void AddFloatProperty(SerializedProperty property, string text, string tooltip, OnValueChangedDelegate<float> onChange = null, float factor = 1f)
		{
			Rect ourRect = EditorGUILayout.BeginHorizontal();
			EditorGUI.BeginProperty(ourRect, GUIContent.none, property);

			var orgValue = property.floatValue * factor;
			var newValue = EditorGUILayout.FloatField(new GUIContent(text, tooltip), orgValue) / factor;
			
			if (onChange != null && orgValue != newValue)
				newValue = onChange(orgValue, newValue);
			
			property.floatValue = newValue;

			EditorGUI.EndProperty();
			EditorGUILayout.EndHorizontal();
		}

		void AddIntProperty(SerializedProperty property, string text, string tooltip, OnValueChangedDelegate<int> onChange = null)
		{
			Rect ourRect = EditorGUILayout.BeginHorizontal();
			EditorGUI.BeginProperty(ourRect, GUIContent.none, property);

			var orgValue = property.intValue;
			var newValue = EditorGUILayout.IntField(new GUIContent(text, tooltip), orgValue);
			
			if (onChange != null && orgValue != newValue)
				newValue = onChange(orgValue, newValue);
			
			property.intValue = newValue;

			EditorGUI.EndProperty();
			EditorGUILayout.EndHorizontal();
		}

		void AddFloatSlider(SerializedProperty property, string text, string tooltip, OnValueChangedDelegate<float> onChange = null, float factor = 1f, float min = 0, float max = float.MaxValue)
		{
			Rect ourRect = EditorGUILayout.BeginHorizontal();
			EditorGUI.BeginProperty(ourRect, GUIContent.none, property);

			var orgValue = property.floatValue * factor;
			var newValue = EditorGUILayout.Slider(new GUIContent(text, tooltip), orgValue, min * factor, max * factor) / factor;

			if (onChange != null && orgValue != newValue)
				newValue = onChange(orgValue, newValue);

			property.floatValue = newValue;

			EditorGUI.EndProperty();
			EditorGUILayout.EndHorizontal();
		}

		void AddIntSlider(SerializedProperty property, string text, string tooltip, OnValueChangedDelegate<int> onChange = null, int min = 0, int max = int.MaxValue)
		{
			Rect ourRect = EditorGUILayout.BeginHorizontal();
			EditorGUI.BeginProperty(ourRect, GUIContent.none, property);

			var orgValue = property.intValue;
			var newValue = EditorGUILayout.IntSlider(new GUIContent(text, tooltip), orgValue, min, max);

			if (onChange != null && orgValue != newValue)
				newValue = onChange(orgValue, newValue);

			property.intValue = newValue;

			EditorGUI.EndProperty();
			EditorGUILayout.EndHorizontal();
		}

		void AddBoolProperty(SerializedProperty property, string text, string tooltip, OnValueChangedDelegate<bool> onChange = null)
		{
			Rect ourRect = EditorGUILayout.BeginHorizontal();
			EditorGUI.BeginProperty(ourRect, GUIContent.none, property);

			var orgValue = property.boolValue;
			var newValue = EditorGUILayout.Toggle(new GUIContent(text, tooltip), orgValue);

			if (onChange != null && orgValue != newValue)
				newValue = onChange(orgValue, newValue);

			property.boolValue = newValue;

			EditorGUI.EndProperty();
			EditorGUILayout.EndHorizontal();
		}

	}
}
