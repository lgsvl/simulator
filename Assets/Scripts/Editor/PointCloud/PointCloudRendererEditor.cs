/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEditor;
using Simulator.PointCloud;

namespace Simulator.Editor.PointCloud
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(PointCloudRenderer))]
    public class PointCloudRendererEditor : UnityEditor.Editor
    {
        private SerializedProperty PointCloudData;
        protected SerializedProperty Colorize;
        protected SerializedProperty Render;
        protected SerializedProperty Blit;
        protected SerializedProperty ConstantSize;
        protected SerializedProperty PixelSize;
        protected SerializedProperty AbsoluteSize;
        protected SerializedProperty MinPixelSize;
        protected SerializedProperty DebugSolidBlitLevel;
        protected SerializedProperty SolidRemoveHidden;
        protected SerializedProperty DebugSolidPullPush;
        protected SerializedProperty DebugSolidFixedLevel;
        protected SerializedProperty DebugSolidMetric;
        protected SerializedProperty DebugSolidMetric2;
        protected SerializedProperty DebugSolidPullParam;
        protected SerializedProperty SolidFovReprojection;
        protected SerializedProperty ReprojectionRatio;
        protected SerializedProperty PreserveTexelSize;
        protected SerializedProperty DebugVec;

        protected virtual void OnEnable()
        {
            FindProtectedProperties();
            PointCloudData = serializedObject.FindProperty(nameof(PointCloudRenderer.Data));
        }

        protected void FindProtectedProperties()
        {
            Colorize = serializedObject.FindProperty(nameof(PointCloudRenderer.Colorize));
            Render = serializedObject.FindProperty(nameof(PointCloudRenderer.Render));
            Blit = serializedObject.FindProperty(nameof(PointCloudRenderer.Blit));
            ConstantSize = serializedObject.FindProperty(nameof(PointCloudRenderer.ConstantSize));
            PixelSize = serializedObject.FindProperty(nameof(PointCloudRenderer.PixelSize));
            AbsoluteSize = serializedObject.FindProperty(nameof(PointCloudRenderer.AbsoluteSize));
            MinPixelSize = serializedObject.FindProperty(nameof(PointCloudRenderer.MinPixelSize));
            DebugSolidBlitLevel = serializedObject.FindProperty(nameof(PointCloudRenderer.DebugSolidBlitLevel));
            SolidRemoveHidden = serializedObject.FindProperty(nameof(PointCloudRenderer.SolidRemoveHidden));
            DebugSolidPullPush = serializedObject.FindProperty(nameof(PointCloudRenderer.DebugSolidPullPush));
            DebugSolidFixedLevel = serializedObject.FindProperty(nameof(PointCloudRenderer.DebugSolidFixedLevel));
            DebugSolidMetric = serializedObject.FindProperty(nameof(PointCloudRenderer.DebugSolidMetric));
            DebugSolidMetric2 = serializedObject.FindProperty(nameof(PointCloudRenderer.DebugSolidMetric2));
            DebugSolidPullParam = serializedObject.FindProperty(nameof(PointCloudRenderer.DebugSolidPullParam));
            SolidFovReprojection = serializedObject.FindProperty(nameof(PointCloudRenderer.SolidFovReprojection));
            ReprojectionRatio = serializedObject.FindProperty(nameof(PointCloudRenderer.ReprojectionRatio));
            PreserveTexelSize = serializedObject.FindProperty(nameof(PointCloudRenderer.PreserveTexelSize));
            DebugVec = serializedObject.FindProperty(nameof(PointCloudRenderer.DebugVec));
        }

        public sealed override void OnInspectorGUI()
        {
            var obj = target as PointCloudRenderer;

            serializedObject.Update();

            DrawInspector(obj);

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void DrawInspector(PointCloudRenderer obj)
        {
            EditorGUILayout.PropertyField(PointCloudData);
            DrawProtectedProperties(obj);
        }

        protected void DrawProtectedProperties(PointCloudRenderer obj)
        {
            EditorGUILayout.PropertyField(Colorize);
            EditorGUILayout.PropertyField(Render);

            if (obj.Render == PointCloudRenderer.RenderType.Points)
            {
                EditorGUILayout.PropertyField(ConstantSize);
                if (obj.ConstantSize)
                {
                    EditorGUILayout.PropertyField(PixelSize);
                }
                else
                {
                    EditorGUILayout.PropertyField(AbsoluteSize);
                    EditorGUILayout.PropertyField(MinPixelSize);
                }
            }
            else if (obj.Render == PointCloudRenderer.RenderType.Solid)
            {
                EditorGUILayout.PropertyField(Blit);
                EditorGUILayout.PropertyField(DebugSolidBlitLevel);
                EditorGUILayout.PropertyField(SolidRemoveHidden);
                EditorGUILayout.PropertyField(DebugSolidPullPush);
                EditorGUILayout.PropertyField(DebugSolidFixedLevel);
                EditorGUILayout.PropertyField(DebugSolidMetric);
                EditorGUILayout.PropertyField(DebugSolidMetric2);
                EditorGUILayout.PropertyField(DebugSolidPullParam);
                EditorGUILayout.PropertyField(DebugVec);
                EditorGUILayout.PropertyField(SolidFovReprojection);
                if (obj.SolidFovReprojection)
                {
                    EditorGUILayout.PropertyField(ReprojectionRatio);
                    EditorGUILayout.PropertyField(PreserveTexelSize);
                }
            }
        }
    }
}
