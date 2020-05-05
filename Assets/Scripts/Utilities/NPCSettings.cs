/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
using UnityEngine;

namespace Simulator.Utilities
{
    [CreateAssetMenu(fileName = "NPCSettings", menuName = "Simulator/NPCSettings")]
    public class NPCSettings : ScriptableObject
    {
        public static NPCSettings Load()
        {
            return Resources.Load<NPCSettings>("NPC/NPCSettings");
        }

        public List<GameObject> NPCPrefabs = new List<GameObject>();
    }
}
