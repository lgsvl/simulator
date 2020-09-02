/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.Editor
{
    using UnityEngine;
    using UnityEngine.Rendering;

    public static class FbxExportHelper
    {
        private static readonly int LitColor = Shader.PropertyToID("_BaseColor");
        private static readonly int LitMainTex = Shader.PropertyToID("_BaseColorMap");
        private static readonly int LitEmissionColor = Shader.PropertyToID("_EmissiveColor");
        private static readonly int LitEmissionMap = Shader.PropertyToID("_EmissiveColorMap");
        private static readonly int LitBumpScale = Shader.PropertyToID("_NormalScale");
        private static readonly int LitBumpMap = Shader.PropertyToID("_NormalMap");

        private static readonly int FbxColor = Shader.PropertyToID("_Color");
        private static readonly int FbxMainTex = Shader.PropertyToID("_MainTex");
        private static readonly int FbxEmissionColor = Shader.PropertyToID("_EmissionColor");
        private static readonly int FbxEmissionMap = Shader.PropertyToID("_EmissionMap");
        private static readonly int FbxBumpScale = Shader.PropertyToID("_BumpScale");
        private static readonly int FbxBumpMap = Shader.PropertyToID("_BumpMap");
        
        public static void PrepareObject(GameObject obj)
        {
            // FBX exporter includes disabled collider renderers - remove them
            foreach (var col in obj.transform.GetComponentsInChildren<Collider>(true))
            {
                var mr = col.transform.GetComponent<MeshRenderer>();
                if (mr != null)
                    CoreUtils.Destroy(col.gameObject);
            }

            // FBX exporter includes lights, but fails to transfer their properties - remove them
            foreach (var light in obj.transform.GetComponentsInChildren<Light>(true))
            {
                CoreUtils.Destroy(light.gameObject);
            }

            // FBX exporter uses hard-coded property names, incompatible with HDRP - convert all materials
            foreach (var rnd in obj.transform.GetComponentsInChildren<Renderer>())
            {
                var mats = rnd.sharedMaterials;
                for (var i = 0; i < mats.Length; ++i)
                    mats[i] = CreateCompatibleMaterial(mats[i]);
                rnd.sharedMaterials = mats;
            }
        }

        private static Material CreateCompatibleMaterial(Material originalMaterial)
        {
            var mat = new Material(Shader.Find("Hidden/FbxCompatibilityShader"));
            mat.name = originalMaterial.name;

            // HDRP uses linear color space, but almost every external renderer corrects it to sRGB
            // Reverse color space conversion so that model rendered outside will look properly after sRGB correction
            if (originalMaterial.HasProperty(LitColor))
                mat.SetColor(FbxColor, originalMaterial.GetColor(LitColor).linear);
            
            if (originalMaterial.HasProperty(LitMainTex))
                mat.SetTexture(FbxMainTex, originalMaterial.GetTexture(LitMainTex));
            
            if (originalMaterial.HasProperty(LitEmissionColor))
                mat.SetColor(FbxEmissionColor, originalMaterial.GetColor(LitEmissionColor).linear);
            
            if (originalMaterial.HasProperty(LitEmissionMap))
                mat.SetTexture(FbxEmissionMap, originalMaterial.GetTexture(LitEmissionMap));

            if (originalMaterial.HasProperty(LitBumpScale))
                mat.SetFloat(FbxBumpScale, originalMaterial.GetFloat(LitBumpScale));
            
            if (originalMaterial.HasProperty(LitBumpMap))
                mat.SetTexture(FbxBumpMap, originalMaterial.GetTexture(LitBumpMap));

            return mat;
        }
    }
}