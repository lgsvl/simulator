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
    using System.Linq;
    using UnityEngine;

    public class LaneLineOverride : MonoBehaviour
    {
        [SerializeField]
        private List<MapTrafficLane> keys;

        [SerializeField]
        private List<LaneLineOverrideData> values;

        private Dictionary<MapTrafficLane, LaneLineOverrideData> dict;

        private void Start()
        {
            dict = new Dictionary<MapTrafficLane, LaneLineOverrideData>();

            for (var i = 0; i < keys.Count; ++i)
                dict.Add(keys[i], values[i]);
        }

        public void SetData(Dictionary<MapTrafficLane, LaneLineOverrideData> data)
        {
            dict = data;
            keys = data.Keys.ToList();
            values = data.Values.ToList();
        }

        public List<Vector3> GetLaneLineData(MapTrafficLane lane, bool isLeft)
        {
            if (dict == null)
                return null;

            if (dict.ContainsKey(lane))
                return isLeft ? dict[lane].leftLineWorldPositions : dict[lane].rightLineWorldPositions;

            return null;
        }

        // This will render stored lines - use for debugging
        /*
        public void OnDrawGizmosSelected()
        {
            if (dict == null)
            {
                dict = new Dictionary<MapLane, LaneLineOverrideData>();

                for (var i = 0; i < keys.Count; ++i)
                    dict.Add(keys[i], values[i]);
            }
            
            var orgCol = Gizmos.color;
            Gizmos.color = Color.green;

            foreach (var kvp in dict)
            {
                var lines = kvp.Value;
                for (var i = 1; i < lines.rightLineWorldPositions.Count; ++i)
                {
                    if (i == 1)
                        Gizmos.DrawSphere(lines.rightLineWorldPositions[i - 1], 0.15f);
                    Gizmos.DrawSphere(lines.rightLineWorldPositions[i], 0.15f);
                    Gizmos.DrawLine(lines.rightLineWorldPositions[i - 1], lines.rightLineWorldPositions[i]);
                }

                for (var i = 1; i < lines.leftLineWorldPositions.Count; ++i)
                {
                    if (i == 1)
                        Gizmos.DrawSphere(lines.leftLineWorldPositions[i - 1], 0.15f);
                    Gizmos.DrawSphere(lines.leftLineWorldPositions[i], 0.15f);
                    Gizmos.DrawLine(lines.leftLineWorldPositions[i - 1], lines.leftLineWorldPositions[i]);
                }
            }
            
            Gizmos.color = orgCol;
        }
        */
    }
}