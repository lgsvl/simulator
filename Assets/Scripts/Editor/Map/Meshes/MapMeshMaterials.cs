/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor.MapMeshes
{
    using System.Collections.Generic;
    using UnityEngine;

    public class MapMeshMaterials : ScriptableObject
    {
        private static readonly int ColorId = Shader.PropertyToID("_BaseColor");
        
        public Material road;
        public Material lineSolid;
        public Material lineDotted;
        public Material lineDouble;

        private readonly Dictionary<Color, Material> roadMaterialInstances = new Dictionary<Color, Material>();
        private readonly Dictionary<Color, Material> lineSolidMaterialInstances = new Dictionary<Color, Material>();
        private readonly Dictionary<Color, Material> lineDottedMaterialInstances = new Dictionary<Color, Material>();
        private readonly Dictionary<Color, Material> lineDoubleMaterialInstances = new Dictionary<Color, Material>();

        public Material GetRoadMaterial(Color color)
        {
            if (!roadMaterialInstances.ContainsKey(color))
            {
                var instance = new Material(road);
                instance.SetColor(ColorId, color);
                roadMaterialInstances[color] = instance;
            }

            return roadMaterialInstances[color];
        }
        
        public Material GetSolidLineMaterial(Color color)
        {
            if (!lineSolidMaterialInstances.ContainsKey(color))
            {
                var instance = new Material(lineSolid);
                instance.SetColor(ColorId, color);
                lineSolidMaterialInstances[color] = instance;
            }

            return lineSolidMaterialInstances[color];
        }
        
        public Material GetDottedLineMaterial(Color color)
        {
            if (!lineDottedMaterialInstances.ContainsKey(color))
            {
                var instance = new Material(lineDotted);
                instance.SetColor(ColorId, color);
                lineDottedMaterialInstances[color] = instance;
            }

            return lineDottedMaterialInstances[color];
        }
        
        public Material GetDoubleLineMaterial(Color color)
        {
            if (!lineDoubleMaterialInstances.ContainsKey(color))
            {
                var instance = new Material(lineDouble);
                instance.SetColor(ColorId, color);
                lineDoubleMaterialInstances[color] = instance;
            }

            return lineDoubleMaterialInstances[color];
        }
    }
}