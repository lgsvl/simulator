/**
* Copyright (c) 2019 LG Electronics, Inc.
*
* This software contains code licensed as described in LICENSE.
*
*/

using SimpleJSON;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace Api.Commands
{
    static class BoundsHelper
    {
        public static IEnumerable<Vector3> GetCorners(this Bounds obj)
        {
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        yield return obj.center + Vector3.Scale(obj.size / 2, new Vector3(x, y, z));
                    }
                }
            }
        }
    }

    class AgentGetBoundingBox : ICommand
    {
        public string Name { get { return "agent/get_bounding_box"; } }

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;

            GameObject obj;
            if (ApiManager.Instance.Agents.TryGetValue(uid, out obj))
            {
                int[] goodLayers =
                {
                    LayerMask.NameToLayer("Default"),
                    LayerMask.NameToLayer("Duckiebot"),
                    LayerMask.NameToLayer("NPC"),
                    LayerMask.NameToLayer("Pedestrian"),
                };

                var bounds = new Bounds();
                foreach (var filter in obj.GetComponentsInChildren<MeshFilter>())
                {
                    if (filter.mesh != null && Array.IndexOf(goodLayers, filter.gameObject.layer) != -1)
                    {
                        foreach (var corner in filter.mesh.bounds.GetCorners())
                        {
                            var pt = filter.transform.TransformPoint(corner);
                            pt = obj.transform.InverseTransformPoint(pt);
                            bounds.Encapsulate(pt);
                        }
                    }
                }

                foreach (var sk in obj.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    if (sk.sharedMesh != null && Array.IndexOf(goodLayers, sk.gameObject.layer) != -1)
                    {
                        foreach (var corner in sk.sharedMesh.bounds.GetCorners())
                        {
                            var pt = sk.transform.TransformPoint(corner);
                            pt = obj.transform.InverseTransformPoint(pt);
                            bounds.Encapsulate(pt);
                        }
                    }
                }

                var result = new JSONObject();
                result.Add("min", bounds.min);
                result.Add("max", bounds.max);
                ApiManager.Instance.SendResult(result);
           }
            else
            {
                ApiManager.Instance.SendError($"Agent '{uid}' not found");
            }
        }
    }
}
