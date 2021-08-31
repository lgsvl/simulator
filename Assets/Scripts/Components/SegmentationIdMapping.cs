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
        private Dictionary<int, Bounds> boundsDict = new Dictionary<int, Bounds>();

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
            if (!gtidDict.ContainsKey(gtid))
                return;

            var index = gtidDict[gtid].segmentationId;
            gtidDict.Remove(gtid);
            segIdDict.Remove(index);
            boundsDict.Remove(index);
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

        public bool TryGetEntityLocalBoundingBox(int segmentationId, out Bounds bounds)
        {
            bounds = new Bounds();

            if (!boundsDict.TryGetValue(segmentationId, out bounds))
            {
                if (TryGetEntityGameObject(segmentationId, out var go, out var type))
                {
                    bounds = GetLocalBoundsTight(go);
                    boundsDict[segmentationId] = bounds;
                    return true;
                }
                return false;
            }

            return true;
        }

        private Bounds GetLocalBoundsTight(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var skinnedMeshRenderer = go.GetComponentInChildren<SkinnedMeshRenderer>();

            if (skinnedMeshRenderer != null)
            {
                return TransformBounds(skinnedMeshRenderer.localBounds, skinnedMeshRenderer.rootBone, skinnedMeshRenderer.transform);
            }

            if (renderers.Length > 0)
            {
                Bounds bounds = new Bounds();

                foreach (var renderer in renderers)
                {
                    var mf = renderer.GetComponent<MeshFilter>();
                    if (mf == null)
                        continue;

                    var meshBounds = TransformBounds(mf.sharedMesh.bounds, mf.transform, go.transform);
                    bounds.Encapsulate(meshBounds);

                    return bounds;
                }
            }

            return new Bounds();
        }

        private Bounds TransformBounds(Bounds bounds, Transform from, Transform to)
        {
            var result = new Bounds();

            var c = bounds.center;
            var e = bounds.extents;
            result.Encapsulate(to.InverseTransformPoint(from.TransformPoint(new Vector3(c.x + e.x, c.y + e.y, c.z + e.z))));
            result.Encapsulate(to.InverseTransformPoint(from.TransformPoint(new Vector3(c.x + e.x, c.y + e.y, c.z - e.z))));
            result.Encapsulate(to.InverseTransformPoint(from.TransformPoint(new Vector3(c.x + e.x, c.y - e.y, c.z + e.z))));
            result.Encapsulate(to.InverseTransformPoint(from.TransformPoint(new Vector3(c.x + e.x, c.y - e.y, c.z - e.z))));
            result.Encapsulate(to.InverseTransformPoint(from.TransformPoint(new Vector3(c.x - e.x, c.y + e.y, c.z + e.z))));
            result.Encapsulate(to.InverseTransformPoint(from.TransformPoint(new Vector3(c.x - e.x, c.y + e.y, c.z - e.z))));
            result.Encapsulate(to.InverseTransformPoint(from.TransformPoint(new Vector3(c.x - e.x, c.y - e.y, c.z + e.z))));
            result.Encapsulate(to.InverseTransformPoint(from.TransformPoint(new Vector3(c.x - e.x, c.y - e.y, c.z - e.z))));

            return result;
        }
    }
}