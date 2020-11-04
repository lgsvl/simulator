/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Map.LineDetection
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    [Serializable]
    public class SegmentedLine3D
    {
        public List<Line3D> lines;

        public Vector3 Start => lines[0].Start;

        public Vector3 End => lines[lines.Count - 1].End;

        public Vector3 StartVector => lines[0].Vector;
        
        public Vector2 StartVectorXZ => new Vector2(lines[0].Vector.x, lines[0].Vector.z);

        public Vector3 EndVector => lines[lines.Count - 1].Vector;
        
        public Vector2 EndVectorXZ => new Vector2(lines[lines.Count - 1].Vector.x, lines[lines.Count - 1].Vector.z); 

        public Color color;

        public float Length
        {
            get
            {
                var result = 0f;
                foreach (var line in lines)
                    result += line.Length;

                return result;
            }
        }

        public float Width
        {
            get
            {
                var sum = 0f;
                foreach (var line in lines)
                    sum += line.width;

                return sum / lines.Count;
            }
            set
            {
                foreach (var line in lines)
                    line.width = value;
            }
        }

        public SegmentedLine3D(List<Line3D> lines)
        {
            this.lines = lines;
        }

        public SegmentedLine3D Clone()
        {
            var result = new SegmentedLine3D(new List<Line3D>(lines));
            result.color = color;
            return result;
        }
        
        public void Invert()
        {
            var newLines = new List<Line3D>(lines.Count);
            for (var i = lines.Count - 1; i >= 0; --i)
                newLines.Add(lines[i].Inverted());

            lines = newLines;
        }

        public void SnapSegments()
        {
            for (var i = 1; i < lines.Count; ++i)
            {
                var avg = (lines[i - 1].points[1] + lines[i].points[0]) * 0.5f;
                lines[i - 1].points[1] = avg;
                lines[i].points[0] = avg;
            }
        }

        public bool TryMerge(SegmentedLine3D segment, float distanceThreshold, float angleThreshold)
        {
            if (Vector3.Distance(Start, segment.Start) < distanceThreshold && Vector3.Angle(StartVector, segment.StartVector) < angleThreshold)
            {
                segment.Invert();
                segment.lines.AddRange(lines);
                lines = segment.lines;
                return true;
            }

            if (Vector3.Distance(End, segment.End) < distanceThreshold && Vector3.Angle(EndVector, segment.EndVector) < angleThreshold)
            {
                segment.Invert();
                lines.AddRange(segment.lines);
                return true;
            }

            if (Vector3.Distance(End, segment.Start) < distanceThreshold && Vector3.Angle(EndVector, segment.StartVector) < angleThreshold)
            {
                lines.AddRange(segment.lines);
                return true;
            }

            if (Vector3.Distance(Start, segment.End) < distanceThreshold && Vector3.Angle(Start, segment.End) < angleThreshold)
            {
                segment.lines.AddRange(lines);
                lines = segment.lines;
                return true;
            }

            return false;
        }
    }
}