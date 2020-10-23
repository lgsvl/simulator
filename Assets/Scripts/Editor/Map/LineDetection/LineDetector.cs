/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor.MapLineDetection
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Simulator.Map.LineDetection;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Rendering;
    using Debug = UnityEngine.Debug;
    using Object = UnityEngine.Object;
    using Random = UnityEngine.Random;

    public static class LineDetector
    {
        private static readonly int AlbedoTexProp = Shader.PropertyToID("Texture2D_B0FD20D1");

        public static void Execute(Transform meshParent, LineDetectionSettings settings)
        {
            var roadRenderers = GetRoadRenderers();

            foreach (var road in roadRenderers)
            {
                var mf = road.GetComponent<MeshFilter>();
                
                if (!road.sharedMaterial.HasProperty(AlbedoTexProp))
                    continue;
                
                var tex = road.sharedMaterial.GetTexture(AlbedoTexProp);
                var imagePath = Path.Combine(Application.dataPath, "..", AssetDatabase.GetAssetPath(tex));
                var uvToWorldScale = LineUtils.EstimateUvToWorldScale(mf, tex);
                var lines = DetectLines(imagePath, uvToWorldScale);

                var approximatedLines = ProcessUvLines(lines, settings);

                var worldSpaceSegments = UvToWorldMapper.CalculateWorldSpaceNew(approximatedLines, mf, uvToWorldScale);
                ProcessWorldLines(worldSpaceSegments, settings);
                BuildWorldObject(worldSpaceSegments, road.transform, meshParent);
            }
        }

        private static List<MeshRenderer> GetRoadRenderers()
        {
            var renderers = Object.FindObjectsOfType<MeshRenderer>().Where(x => x.name.Contains("road") || x.name.Contains("Road")).ToList();
            return renderers;
        }
        
        private static List<ApproximatedLine> ProcessUvLines(List<Line> lines, LineDetectionSettings settings)
        {
            var approximatedLines = new List<ApproximatedLine>();

            foreach (var line in lines)
            {
                var assigned = false;
                foreach (var approxLine in approximatedLines)
                {
                    if (approxLine.TryAddLine(line))
                    {
                        assigned = true;
                        break;
                    }
                }

                if (!assigned)
                {
                    approximatedLines.Add(new ApproximatedLine(line, settings));
                }
            }

            var changed = true;
            var safety = 0;
            while (changed)
            {
                if (safety++ > 20000)
                {
                    throw new Exception("Broken infinite loop");
                }
                changed = false;
                for (var i = 0; i < approximatedLines.Count - 1; ++i)
                {
                    for (var j = i + 1; j < approximatedLines.Count; ++j)
                    {
                        if (approximatedLines[i].TryMerge(approximatedLines[j]))
                        {
                            approximatedLines.Remove(approximatedLines[j]);
                            changed = true;
                            break;
                        }
                    }
                    if (changed)
                        break;
                }
            }

            for (var i = 0; i < approximatedLines.Count - 1; ++i)
            {
                for (var j = i + 1; j < approximatedLines.Count; ++j)
                {
                    if (approximatedLines[j].lines.Count > 1)
                        continue;

                    if (approximatedLines[i].TryMergeIgnoreAngle(approximatedLines[j]))
                        approximatedLines.Remove(approximatedLines[j]);
                }
            }

            for (var i = 0; i < approximatedLines.Count; ++i)
            {
                approximatedLines[i].Recalculate();
                if (approximatedLines[i].BestFitLine.Length > settings.maxLineSegmentLength)
                {
                    LineUtils.SplitByLength(approximatedLines[i], out var a, out var b);
                    approximatedLines.RemoveAt(i--);
                    approximatedLines.Add(a);
                    approximatedLines.Add(b);
                }
            }

            for (var i = 0; i < approximatedLines.Count; ++i)
            {
                var line = approximatedLines[i];
                if (!line.IsValid)
                    LineUtils.RemovePastThresholdLines(line);
                if (!line.IsValid)
                    approximatedLines.RemoveAt(i--);
            }

            return approximatedLines;
        }

        private static void ProcessWorldLines(List<SegmentedLine3D> segments, LineDetectionSettings settings)
        {
            var changed = true;
            var safety = 0;

            var dist = settings.worldSpaceSnapDistance;
            var angle = settings.worldSpaceSnapAngle;
            
            while (changed)
            {
                if (safety++ > 20000)
                {
                    throw new Exception("Broken infinite loop");
                }
                changed = false;
                for (var i = 0; i < segments.Count - 1; ++i)
                {
                    for (var j = i + 1; j < segments.Count; ++j)
                    {
                        if (segments[i].TryMerge(segments[j], dist, angle))
                        {
                            segments.Remove(segments[j]);
                            changed = true;
                            break;
                        }
                    }
                    if (changed)
                        break;
                }
            }

            foreach (var segment in segments)
            {
                var color = Random.ColorHSV(0, 1, 0.9f, 1, 0.7f, 1.0f);
                segment.color = color;
                segment.SnapSegments();
            }
        }

        private static void BuildWorldObject(List<SegmentedLine3D> segments, Transform roadTransform, Transform parent)
        {
            var meshGo = new GameObject(roadTransform.name + "_lines");
            meshGo.transform.SetParent(parent);
            meshGo.transform.tag = "LaneLine";
            meshGo.transform.localScale = roadTransform.localScale;
            meshGo.transform.rotation = roadTransform.rotation;
            meshGo.transform.position = roadTransform.position;
            var mr = meshGo.AddComponent<MeshRenderer>();
            var mf = meshGo.AddComponent<MeshFilter>();
            var holder = meshGo.AddComponent<LineDataHolder>();
            holder.segments = new List<SegmentedLine3D>();
            holder.segments.AddRange(segments);

            var verts = new List<Vector3>();
            var indices = new List<int>();

            var corners = new Vector3[4];

            foreach (var segment in segments)
            {
                foreach (var line in segment.lines)
                {
                    var count = verts.Count;
                    var vec = line.End - line.Start;
                    var nVec = new Vector3(vec.z, 0, -vec.x).normalized * segment.Width * 0.5f;
                    corners[0] = line.Start + nVec;
                    corners[1] = line.Start - nVec;
                    corners[2] = line.End - nVec;
                    corners[3] = line.End + nVec;

                    foreach (var corner in corners)
                        verts.Add(roadTransform.InverseTransformPoint(corner) + new Vector3(0f, 0.02f, 0f));

                    indices.Add(count);
                    indices.Add(count + 1);
                    indices.Add(count + 2);
                    indices.Add(count);
                    indices.Add(count + 2);
                    indices.Add(count + 3);
                }
            }
            
            var mesh = new Mesh();
            mesh.SetVertices(verts);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.Optimize();

            mf.sharedMesh = mesh;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.sharedMaterial = new Material(Shader.Find("Simulator/SegmentationLine"));
        }

        private static Line ParseLine(string line, float uvToWorldScale)
        {
            var noBrackets = line.Substring(1, line.Length - 2);
            var tokens = Regex.Split(noBrackets, ", ");
            var result = Vector4.zero;
            var currentItem = 0;
            for (var i = 0; i < tokens.Length; ++i)
            {
                if (tokens[i].Length == 0)
                    continue;
                
                result[currentItem++] = float.Parse(tokens[i]);
            }

            result.y = 1.0f - result.y;
            result.w = 1.0f - result.w;
            result *= uvToWorldScale;
            return new Line(new Vector2(result.x, result.y), new Vector2(result.z, result.w));
        }

        public static List<Line> DetectLines(string inputImagePath, float uvToWorldScale)
        {
            string path;
            switch (SystemInfo.operatingSystemFamily)
            {
                case OperatingSystemFamily.Windows:
                    path = Path.Combine(Application.dataPath, "Plugins/LineSegmentDetector/windows_x64/LineSegmentDetector.exe");
                    break;
                case OperatingSystemFamily.Linux:
                    path = Path.Combine(Application.dataPath, "Plugins/LineSegmentDetector/linux_x64/LineSegmentDetector");
                    break;
                default:
                    throw new Exception("This feature is only supported on Windows and Linux.");
            }

            var start = new ProcessStartInfo
            {
                FileName = path,
                Arguments = inputImagePath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            string consoleOutput = string.Empty;
            using (var process = Process.Start(start))
            {
                if (process != null)
                {
                    using (var reader = process.StandardOutput)
                    {
                        consoleOutput = reader.ReadToEnd();
                    }
                }
            }

            var lines = Regex.Split(consoleOutput, "\r\n|\r|\n");
            List<Line> result;
            try
            {
                result = (from line in lines where line.Length != 0 select ParseLine(line, uvToWorldScale)).ToList();
            }
            catch
            {
                Debug.LogError("Failed parsing process output:\n" + consoleOutput);
                throw;
            }

            return result;
        }
    }
}