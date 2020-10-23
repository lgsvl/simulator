/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor.MapLineDetection
{
    using System.Collections.Generic;
    using System.Linq;
    using Simulator.Map.LineDetection;
    using UnityEngine;

    public class ApproximatedLine
    {
        public List<Line> lines;

        public Color color = Color.green;

        public Vector3 worldSpaceLineStart;
        public Vector3 worldSpaceLineEnd;
        public bool hasWorldSpaceLine;

        public Line BestFitLine { get; private set; }
        
        public float WorstFit { get; private set; }
        public float AverageFit { get; private set; }
        public float OptimizationValue => WorstFit;

        public bool IsValid => lines.Count > 0 && WorstFit < Settings.worstFitThreshold && WorstFit > Settings.minWidthThreshold;

        public LineDetectionSettings Settings { get; private set; }
        
        public ApproximatedLine(LineDetectionSettings settings)
        {
            Settings = settings;
            lines = new List<Line>();
        }
        
        public ApproximatedLine(Line line, LineDetectionSettings settings)
        {
            Settings = settings;
            lines = new List<Line> {line};
        }
        
        public ApproximatedLine(List<Line> lines, LineDetectionSettings settings)
        {
            Settings = settings;
            this.lines = new List<Line>();
            this.lines.AddRange(lines);
        }

        public Vector2[] GetCorners()
        {
            var vec = BestFitLine.Vector;
            var nVec = new Vector2(vec.y, -vec.x).normalized * AverageFit;
            return new[]
            {
                BestFitLine.Start + nVec,
                BestFitLine.Start - nVec,
                BestFitLine.End - nVec,
                BestFitLine.End + nVec
            };
        }

        public Vector3[] GetWorldSpaceCorners()
        {
            if (!hasWorldSpaceLine)
                return null;
            
            var vec = worldSpaceLineEnd - worldSpaceLineStart;
            var nVec = new Vector3(vec.z,0, -vec.x).normalized * AverageFit * (vec.magnitude / BestFitLine.Length);
            return new[]
            {
                worldSpaceLineStart + nVec,
                worldSpaceLineStart - nVec,
                worldSpaceLineEnd - nVec,
                worldSpaceLineEnd + nVec
            };
        }

        public bool TryAddLine(Line line, float distanceThreshold)
        {
            foreach (var l in lines)
            {
                if (LineUtils.LineLineDistance(l, line) < distanceThreshold && LineUtils.LineLineAngle(l, line) < Settings.lineAngleThreshold)
                {
                    lines.Add(line);
                    return true;
                }
            }
            
            return false;
        }
        
        public bool TryAddLine(Line line)
        {
            return TryAddLine(line, Settings.lineDistanceThreshold);
        }

        public bool TryMerge(ApproximatedLine approxLine, float distanceThreshold)
        {
            foreach (var line in lines)
            {
                foreach (var sLine in approxLine.lines)
                {
                    if (LineUtils.LineLineDistance(line, sLine) < distanceThreshold && LineUtils.LineLineAngle(line, sLine) < Settings.lineAngleThreshold)
                    {
                        lines.AddRange(approxLine.lines);
                        return true;
                    }
                }
            }

            return false;
        }
        
        public bool TryMergeIgnoreAngle(ApproximatedLine approxLine)
        {
            foreach (var line in lines)
            {
                foreach (var sLine in approxLine.lines)
                {
                    if (LineUtils.LineLineDistance(line, sLine) < Settings.lineDistanceThreshold)
                    {
                        lines.AddRange(approxLine.lines);
                        return true;
                    }
                }
            }

            return false;
        }
        
        public bool TryMerge(ApproximatedLine approxLine)
        {
            return TryMerge(approxLine, Settings.lineDistanceThreshold);
        }

        public void Recalculate()
        {
            BestFitLine = LineUtils.GenerateLinearBestFit(lines, out var maxDist, out var avgDist);
            WorstFit = maxDist;
            AverageFit = avgDist;
        }

        public bool TrySplit(out ApproximatedLine a, out ApproximatedLine b)
        {
            a = b = null;
            if (lines.Count < 2)
                return false;

            var combinations = Enumerable
                .Range(0, 1 << lines.Count)
                .Select(index => lines
                    .Where((line, i) => (index & (1 << i)) != 0)
                    .ToList());

            var setA = new ApproximatedLine(Settings);
            var setB = new ApproximatedLine(Settings);
            var resA = new ApproximatedLine(Settings) {color = color};
            var resB = new ApproximatedLine(Settings) {color = color};
            var bestVal = float.MaxValue;

            foreach (var combination in combinations)
            {
                if (combination.Count == 0 || combination.Count == lines.Count)
                    continue;

                setA.lines.Clear();
                setB.lines.Clear();
                
                foreach (var line in lines)
                {
                    if (combination.Contains(line))
                        setA.lines.Add(line);
                    else
                        setB.lines.Add(line);
                }
                
                setA.Recalculate();
                setB.Recalculate();

                var val = Mathf.Max(setA.OptimizationValue, setB.OptimizationValue); 

                if (val < bestVal)
                {
                    bestVal = val; 
                    resA.lines.Clear();
                    resA.lines.AddRange(setA.lines);
                    resB.lines.Clear();
                    resB.lines.AddRange(setB.lines);
                }
            }

            if (bestVal > OptimizationValue)
                return false;
            
            resA.Recalculate();
            resB.Recalculate();
            
            a = resA;
            b = resB;

            return true;
        }
    }
}