/**
 * Copyright (c) 2021 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Components
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public class SegmentationIdMapping
    {
        public enum SegmentationEntityType
        {
            None,
            Agent,
            NPC,
            Pedestrian
        }

        private class EntityData
        {
            // public string guid;
            public uint gtid;
            public int segmentationId;
            public GameObject gameObject;
            public SegmentationEntityType type;
        }

        private Dictionary<uint, EntityData> gtidDict = new Dictionary<uint, EntityData>();
        private Dictionary<int, EntityData> segIdDict = new Dictionary<int, EntityData>();

        public int AddSegmentationId(GameObject obj, uint gtid)
        {
            if (gtidDict.ContainsKey(gtid))
            {
                throw new Exception($"This GUID ({gtid}) has segmentation ID already assigned.");
            }

            var newIndex = 255; // 0 is reserved for clear color, 255 for non-agent segmentation
            for (var i = 1; i < 255; ++i)
            {
                if (segIdDict.ContainsKey(i))
                    continue;

                newIndex = i;
                break;
            }

            if (newIndex == 255)
            {
                Debug.LogError("NPC and pedestrians number exceeded maximum amount trackable by perception sensor (254).");
                return -1;
            }

            var npcController = obj.GetComponent<NPCController>();
            var agentController = obj.GetComponent<IAgentController>();
            var pedController = obj.GetComponent<PedestrianController>();

            var type = SegmentationEntityType.None;
            if (agentController != null)
            {
                type = SegmentationEntityType.Agent;
            }
            else if (npcController != null)
            {
                type = SegmentationEntityType.NPC;
            }
            else if (pedController != null)
            {
                type = SegmentationEntityType.Pedestrian;
            }

            var data = new EntityData()
            {
                gtid = gtid,
                segmentationId = newIndex,
                gameObject = obj,
                type = type
            };

            segIdDict[newIndex] = data;
            gtidDict[gtid] = data;
            return newIndex;
        }

        public void RemoveSegmentationId(uint gtid)
        {
            var index = gtidDict[gtid].segmentationId;
            gtidDict.Remove(gtid);
            segIdDict.Remove(index);
        }

        public bool TryGetEntityGameObject(int segmentationId, out GameObject go, out SegmentationEntityType type)
        {
            go = null;
            type = SegmentationEntityType.None;

            if (!segIdDict.TryGetValue(segmentationId, out var data))
                return false;

            go = data.gameObject;
            type = data.type;
            return true;
        }
    }
}