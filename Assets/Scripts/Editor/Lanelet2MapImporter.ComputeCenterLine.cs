/*
 * Copyright 2019 Autoware Foundation. All rights reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using OsmSharp;

namespace Simulator.Editor
{
    public partial class Lanelet2MapImporter
    {

        void SplitLine(long lineStringId, out List<Vector3> splittedLinePoints, float resolution, int partitions, bool reverse=false)
        {
            long[] nodeIds = ((Way)DataSource["Way" + lineStringId]).Nodes; 
            splittedLinePoints = new List<Vector3>();
            splittedLinePoints.Add(GetVector3FromNode((Node)DataSource["Node" + nodeIds[0]])); // Add first point

            float residue = 0; // Residual length from previous segment

            int last = 0;
            // loop through each segment in boundry line
            for (int i = 1; i < nodeIds.Length; i++)
            {
                if (splittedLinePoints.Count >= partitions) break;

                Vector3 lastPoint = GetVector3FromNode((Node)DataSource["Node" + nodeIds[last]]);
                Vector3 curPoint = GetVector3FromNode((Node)DataSource["Node" + nodeIds[i]]);

                // Continue if no points are made within current segment
                float segmentLength = Vector3.Distance(lastPoint, curPoint);
                if (segmentLength + residue < resolution)
                {
                    residue += segmentLength;
                    last = i;
                    continue;
                }

                Vector3 direction = (curPoint - lastPoint).normalized;
                for (float length = resolution - residue; length <= segmentLength; length += resolution)
                {
                    Vector3 partitionPoint = lastPoint + direction * length;
                    splittedLinePoints.Add(partitionPoint);
                    if (splittedLinePoints.Count >= partitions) break;
                    residue = segmentLength - length;
                }

                if (splittedLinePoints.Count >= partitions) break;
                last = i;
            }

            splittedLinePoints.Add(GetVector3FromNode((Node)DataSource["Node" + nodeIds[nodeIds.Length-1]]));

            if (reverse)
            {
                splittedLinePoints.Reverse();
            }
        }

        List<Vector3> ComputeCenterLine(long leftLineStringId, long rightLineStringId)
        {
            // Check the directions of two boundry lines
            //    if they are not same, reverse one and get a temp centerline. Compare centerline with left line, determine direction of the centerlane
            //    if they are same, compute centerline.
            var sameDirection = true;
            var leftNodeIds = ((Way)DataSource["Way" + leftLineStringId]).Nodes;
            var rightNodeIds = ((Way)DataSource["Way" + rightLineStringId]).Nodes;
            var leftFirstPoint = GetVector3FromNode((Node)DataSource["Node" + leftNodeIds[0]]);
            var leftSecondPoint = GetVector3FromNode((Node)DataSource["Node" + leftNodeIds[1]]);
            var rightFirstPoint = GetVector3FromNode((Node)DataSource["Node" + rightNodeIds[0]]);
            var rightLastPoint = GetVector3FromNode((Node)DataSource["Node" + rightNodeIds[rightNodeIds.Length-1]]);
            var rightSecondToLastPoint = GetVector3FromNode((Node)DataSource["Node" + rightNodeIds[rightNodeIds.Length-2]]);
            var leftDirection = (leftSecondPoint - leftFirstPoint).normalized;
            var rightEndDirection = (rightLastPoint - rightSecondToLastPoint).normalized;

            // Check if right line's first point or last point is closer to left line's first point
            if (Vector3.SqrMagnitude(leftFirstPoint - rightFirstPoint) > Vector3.SqrMagnitude(leftFirstPoint - rightLastPoint))
            {
                if (Vector3.Dot(leftDirection, rightEndDirection) < 0)
                {
                    sameDirection = false;
                }
            }


            float resolution = 10; // 10 meters
            List<Vector3> centerLinePoints = new List<Vector3>();
            List<Vector3> leftLinePoints = new List<Vector3>();
            List<Vector3> rightLinePoints = new List<Vector3>();

            // Get the length of longer boundary line
            float leftLength = RangedLength(leftLineStringId);
            float rightLength = RangedLength(rightLineStringId);
            float longerDistance = (leftLength > rightLength) ? leftLength : rightLength;
            int partitions = (int)Math.Ceiling(longerDistance / resolution);
            if (partitions < 2)
            {
                // For lineStrings whose length is less than resolution
                partitions = 2; // Make sure every line has at least 2 partitions.
            }

            float leftResolution = leftLength / partitions;
            float rightResolution = rightLength / partitions;

            SplitLine(leftLineStringId, out leftLinePoints, leftResolution, partitions);
            // If left and right lines have opposite direction, reverse right line
            if (!sameDirection) SplitLine(rightLineStringId, out rightLinePoints, rightResolution, partitions, true);
            else SplitLine(rightLineStringId, out rightLinePoints, rightResolution, partitions);

            if (leftLinePoints.Count != partitions + 1 || rightLinePoints.Count != partitions + 1)
            {
                Debug.LogError("Something wrong with number of points. (left, right, partitions): (" + leftLinePoints.Count + ", " + rightLinePoints.Count + ", " + partitions);
                return new List<Vector3>();
            }

            for (int i = 0; i < partitions+1; i ++)
            {
                Vector3 centerPoint = (leftLinePoints[i] + rightLinePoints[i]) / 2;
                centerLinePoints.Add(centerPoint);
            }

            // Compare temp centerLine with left line, determine direction
            var centerDirection = (centerLinePoints[1] - centerLinePoints[0]).normalized;
            var centerToLeftDir = (leftFirstPoint - centerLinePoints[0]).normalized;
            if (Vector3.Cross(centerDirection, centerToLeftDir).y > 0)
            {
                // Left line is on right of centerLine, we need to reverse the center points
                centerLinePoints.Reverse();
            }

            return centerLinePoints;
        }
    }
}
