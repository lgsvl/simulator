/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;

    public static class MaterialReferencesFix
    {
        private static readonly Dictionary<string, string> ReplacementDict = new Dictionary<string, string>()
        {
            {"Texture2D_FC54B01C", "_BaseColorMap"},
            {"Vector2_E53862F5", "_BaseColorMap_Tiling"},
            {"Texture2D_7510551D", "_MaskMap"},
            {"Vector2_B8BDBAD6", "_MaskMap_Tiling"},
            {"Vector1_18B47B70", "_Smoothness"},
            {"Texture2D_8E1261E7", "_NormalMap"},
            {"Vector2_4A7349FB", "_NormalMap_Tiling"},
            {"Vector1_1C1D4CFA", "_NormalScale"},
            {"Texture2D_AF7C252A", "_EmissiveColorMap"},
            {"Vector1_B011", "_EmissiveIntensity"},
            {"Texture2D_B0FD20D1", "_IntensityMap"},
            {"Vector1_8D5F934C", "_IntensityScale"},
            {"Texture2D_D8044091", "_PuddleMap"}
        };

        //[MenuItem("Simulator/Fix material references")]
        public static void DoFix()
        {
            var confirm = EditorUtility.DisplayDialog("Material naming update",
                "This process will fix material property references to match new naming. All materials present in the project and using environment shader will be affected. Do you wish to continue?",
                "Yes", "No");

            if (!confirm)
                return;

            try
            {
                AssetDatabase.StartAssetEditing();
                EditorUtility.DisplayProgressBar("Material naming update", "Filtering materials for update...", 0f);

                var dataPath = Application.dataPath;
                dataPath = dataPath.Remove(dataPath.Length - 7);

                var shader = Shader.Find("Shader Graphs/EnvironmentSimulation");

                var materialPaths = AssetDatabase.GetAllAssetPaths()
                    .Where(x => x.EndsWith(".mat"))
                    .Where(x => AssetDatabase.LoadAssetAtPath<Material>(x)?.shader == shader).ToList();

                for (var i = 0; i < materialPaths.Count; ++i)
                {
                    if (EditorUtility.DisplayCancelableProgressBar("Material naming update",
                        $"Updating material {i + 1}/{materialPaths.Count}", (float) i / materialPaths.Count))
                        break;

                    var fullPath = Path.Combine(dataPath, materialPaths[i]);
                    var lines = File.ReadAllLines(fullPath).ToList();

                    foreach (var kvp in ReplacementDict)
                    {
                        // Look for lines with both old and new property names
                        var originalIndex = -1;
                        var newIndex = -1;

                        var fullOriginal = $"- {kvp.Key}:";
                        var fullNew = $"- {kvp.Value}:";

                        for (var j = 0; j < lines.Count; ++j)
                        {
                            if (originalIndex < 0 && lines[j].Contains(fullOriginal))
                                originalIndex = j;

                            if (newIndex < 0 && lines[j].Contains(fullNew))
                                newIndex = j;

                            if (originalIndex >= 0 && newIndex >= 0)
                                break;
                        }

                        // Property with old name not present - nothing to rename
                        if (originalIndex < 0)
                            continue;

                        // Change name of old property to new one
                        lines[originalIndex] = lines[originalIndex].Replace(kvp.Key, kvp.Value);

                        // If new name was already serialized with default values, remove it to avoid duplicate
                        // Might be multiline - remove all nested YAML fields
                        if (newIndex >= 0)
                        {
                            var indentSize = IndentSize(lines[newIndex]);
                            lines.RemoveAt(newIndex);
                            while (lines.Count > newIndex && IndentSize(lines[newIndex]) > indentSize)
                                lines.RemoveAt(newIndex);
                        }
                    }

                    File.WriteAllLines(fullPath, lines);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
        }

        private static int IndentSize(string str)
        {
            for (var i = 0; i < str.Length; ++i)
            {
                if (!char.IsWhiteSpace(str[i]))
                    return i;
            }

            return -1;
        }
    }
}