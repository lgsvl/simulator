/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor.PointCloud
{
    using UnityEditor;
    using UnityEngine;
    using Simulator.PointCloud;

    [CanEditMultipleObjects]
    [CustomEditor(typeof(PointCloudRenderer))]
    public class PointCloudRendererEditor : UnityEditor.Editor
    {
        private static class Styles
        {
            public static readonly GUIContent CascadeShowPreviewContent =
                new GUIContent("Show Preview", "Visible only in play mode.");

            public static readonly GUIContent CascadeOffsetContent = new GUIContent("Offset");
            public static readonly GUIContent CascadeSizeContent = new GUIContent("Size");

            public static readonly GUIContent FovReprojectionContent =
                new GUIContent("FOV Reprojection", "Visible only in play mode.");

            public static readonly GUIContent DebugSolidBlitLevelContent = new GUIContent("Blit Level",
                "Final blit will display downsampled image on specified mip level.");

            public static readonly GUIContent DebugSolidRemoveHiddenContent =
                new GUIContent("Remove Hidden", "Run hidden point removal kernel.");

            public static readonly GUIContent DebugSolidPullPushContent =
                new GUIContent("Pull-Push", "Run pull-push kernels.");

            public static readonly GUIContent DebugSolidFixedLevelContent = new GUIContent("Remove Hidden Level",
                "Value other than 0 will overwrite cascades in hidden point removal kernel and always use specified mip level.");
            
            public static readonly GUIContent DebugSolidPullParamContent = new GUIContent("Pull Exponent",
                "Filter exponent used in pull kernel."); 
        }
        
        private SerializedProperty PointCloudData;
        private SerializedProperty Colorize;
        private SerializedProperty Render;
        private SerializedProperty Blit;
        private SerializedProperty ConstantSize;
        private SerializedProperty PixelSize;
        private SerializedProperty AbsoluteSize;
        private SerializedProperty MinPixelSize;
        private SerializedProperty DebugSolidBlitLevel;
        private SerializedProperty SolidRemoveHidden;
        private SerializedProperty DebugSolidPullPush;
        private SerializedProperty DebugSolidFixedLevel;
        private SerializedProperty DebugShowRemoveHiddenCascades;
        private SerializedProperty RemoveHiddenCascadeOffset;
        private SerializedProperty RemoveHiddenCascadeSize;
        private SerializedProperty DebugSolidPullParam;
        private SerializedProperty SolidFovReprojection;
        private SerializedProperty ReprojectionRatio;
        private SerializedProperty PreserveTexelSize;
        private SerializedProperty DebugShowSmoothNormalsCascades;
        private SerializedProperty SmoothNormalsCascadeOffset;
        private SerializedProperty SmoothNormalsCascadeSize;

        protected virtual void OnEnable()
        {
            FindSharedProperties();
            PointCloudData = serializedObject.FindProperty(nameof(PointCloudRenderer.Data));
        }

        protected void FindSharedProperties()
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
            DebugShowRemoveHiddenCascades = serializedObject.FindProperty(nameof(PointCloudRenderer.DebugShowRemoveHiddenCascades));
            RemoveHiddenCascadeOffset = serializedObject.FindProperty(nameof(PointCloudRenderer.RemoveHiddenCascadeOffset));
            RemoveHiddenCascadeSize = serializedObject.FindProperty(nameof(PointCloudRenderer.RemoveHiddenCascadeSize));
            DebugSolidPullParam = serializedObject.FindProperty(nameof(PointCloudRenderer.DebugSolidPullParam));
            SolidFovReprojection = serializedObject.FindProperty(nameof(PointCloudRenderer.SolidFovReprojection));
            ReprojectionRatio = serializedObject.FindProperty(nameof(PointCloudRenderer.ReprojectionRatio));
            PreserveTexelSize = serializedObject.FindProperty(nameof(PointCloudRenderer.PreserveTexelSize));
            DebugShowSmoothNormalsCascades = serializedObject.FindProperty(nameof(PointCloudRenderer.DebugShowSmoothNormalsCascades));
            SmoothNormalsCascadeOffset = serializedObject.FindProperty(nameof(PointCloudRenderer.SmoothNormalsCascadeOffset));
            SmoothNormalsCascadeSize = serializedObject.FindProperty(nameof(PointCloudRenderer.SmoothNormalsCascadeSize));
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

            // Use existing SerializedProperty property to remember foldout state
            Render.isExpanded = EditorGUILayout.Foldout(Render.isExpanded, "Settings");
            if (Render.isExpanded)
            {
                EditorGUI.indentLevel++;
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
                    DrawRemoveHiddenCascadesContent();
                    DrawSmoothNormalsCascadesContent();
                    DrawFovReprojectionContent(obj.SolidFovReprojection);

                    // Use existing SerializedProperty property to remember foldout state
                    Blit.isExpanded = EditorGUILayout.Foldout(Blit.isExpanded, "Debug");
                    if (Blit.isExpanded)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(DebugSolidBlitLevel, Styles.DebugSolidBlitLevelContent);
                        EditorGUILayout.PropertyField(SolidRemoveHidden, Styles.DebugSolidRemoveHiddenContent);
                        EditorGUILayout.PropertyField(DebugSolidPullPush, Styles.DebugSolidPullPushContent);
                        EditorGUILayout.PropertyField(DebugSolidFixedLevel, Styles.DebugSolidFixedLevelContent);
                        EditorGUILayout.PropertyField(DebugSolidPullParam, Styles.DebugSolidPullParamContent);
                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUI.indentLevel--;
            }
        }

        private void DrawRemoveHiddenCascadesContent()
        {
            var rect = EditorGUILayout.GetControlRect(false,
                4 * EditorGUIUtility.singleLineHeight + 7 * EditorGUIUtility.standardVerticalSpacing);
            rect = EditorGUI.IndentedRect(rect);
            
            var indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            
            rect.height -= 2 * EditorGUIUtility.standardVerticalSpacing;
            rect.y += EditorGUIUtility.standardVerticalSpacing;
            GUI.Box(rect, GUIContent.none);
            rect.width -= 8;
            rect.x += 4;
            rect.height = EditorGUIUtility.singleLineHeight;
            
            rect.y += EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.LabelField(rect, "Cascades (Remove Hidden)", EditorStyles.boldLabel);
            
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(rect, DebugShowRemoveHiddenCascades, Styles.CascadeShowPreviewContent);
            
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(rect, RemoveHiddenCascadeOffset, Styles.CascadeOffsetContent);
            
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(rect, RemoveHiddenCascadeSize, Styles.CascadeSizeContent);

            EditorGUI.indentLevel = indentLevel;
        }
        
        private void DrawSmoothNormalsCascadesContent()
        {
            var rect = EditorGUILayout.GetControlRect(false,
                4 * EditorGUIUtility.singleLineHeight + 7 * EditorGUIUtility.standardVerticalSpacing);
            rect = EditorGUI.IndentedRect(rect);
            
            var indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            rect.height -= 2 * EditorGUIUtility.standardVerticalSpacing;
            rect.y += EditorGUIUtility.standardVerticalSpacing;
            GUI.Box(rect, GUIContent.none);
            rect.width -= 8;
            rect.x += 4;
            rect.height = EditorGUIUtility.singleLineHeight;
            
            rect.y += EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.LabelField(rect, "Cascades (Smooth Normals)", EditorStyles.boldLabel);
            
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(rect, DebugShowSmoothNormalsCascades, Styles.CascadeShowPreviewContent);
            
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(rect, SmoothNormalsCascadeOffset, Styles.CascadeOffsetContent);
            
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(rect, SmoothNormalsCascadeSize, Styles.CascadeSizeContent);
            
            EditorGUI.indentLevel = indentLevel;
        }

        private void DrawFovReprojectionContent(bool unfold)
        {
            var lineCount = unfold ? 3 : 1;
            var rect = EditorGUILayout.GetControlRect(false,
                lineCount * EditorGUIUtility.singleLineHeight +
                (lineCount + 3) * EditorGUIUtility.standardVerticalSpacing);
            rect = EditorGUI.IndentedRect(rect);
            
            var indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            rect.height -= 2 * EditorGUIUtility.standardVerticalSpacing;
            rect.y += EditorGUIUtility.standardVerticalSpacing;
            GUI.Box(rect, GUIContent.none);
            rect.width -= 8;
            rect.x += 4;
            rect.height = EditorGUIUtility.singleLineHeight;
            
            rect.y += EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(rect, SolidFovReprojection, Styles.FovReprojectionContent);
            
            if (unfold)
            {
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.PropertyField(rect, ReprojectionRatio);
                
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.PropertyField(rect, PreserveTexelSize);
            }

            EditorGUI.indentLevel = indentLevel;
        }
    }
}
