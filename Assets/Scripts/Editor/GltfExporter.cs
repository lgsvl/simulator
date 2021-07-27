namespace Simulator.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.Formats.Fbx.Exporter;
    using UnityEngine;
    using UnityEngine.Rendering;
    using Utilities;
    using Debug = UnityEngine.Debug;

    /// <summary>
    /// Class used to export meshes to glTF format. 
    /// </summary>
    public class GltfExporter : IDisposable
    {
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");
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

        private static readonly string[] IncompatibleExtensions = {".tif", ".tga"};

        private readonly GameObject tempObject;

        private readonly string glbFilename;
        private readonly string fbxObjectPath;
        private readonly string tmpObjectPath;

        private readonly List<string> temporaryFiles = new List<string>();

        public void PrepareObject(GameObject obj)
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

        private Material CreateCompatibleMaterial(Material originalMaterial)
        {
            var mat = new Material(Shader.Find("Hidden/FbxCompatibilityShader"));
            mat.name = originalMaterial.name;

            // HDRP uses linear color space, but almost every external renderer corrects it to sRGB
            // Reverse color space conversion so that model rendered outside will look properly after sRGB correction
            if (originalMaterial.HasProperty(LitColor))
                mat.SetColor(FbxColor, originalMaterial.GetColor(LitColor).linear);

            if (originalMaterial.HasProperty(LitMainTex))
                mat.SetTexture(FbxMainTex, GetCompatibleTexture(originalMaterial.GetTexture(LitMainTex)));

            if (originalMaterial.HasProperty(LitEmissionColor))
                mat.SetColor(FbxEmissionColor, originalMaterial.GetColor(LitEmissionColor).linear);

            if (originalMaterial.HasProperty(LitEmissionMap))
                mat.SetTexture(FbxEmissionMap, GetCompatibleTexture(originalMaterial.GetTexture(LitEmissionMap)));

            if (originalMaterial.HasProperty(LitBumpScale))
                mat.SetFloat(FbxBumpScale, originalMaterial.GetFloat(LitBumpScale));

            if (originalMaterial.HasProperty(LitBumpMap))
                mat.SetTexture(FbxBumpMap, GetCompatibleTexture(originalMaterial.GetTexture(LitBumpMap)));

            return mat;
        }

        private Texture GetCompatibleTexture(Texture tex)
        {
            if (tex == null)
                return tex;

            var texPath = AssetDatabase.GetAssetPath(tex);
            var incompatible = IncompatibleExtensions.Any(extension => texPath.EndsWith(extension, StringComparison.InvariantCultureIgnoreCase));

            // No need to convert texture if it uses glTF compatible format.
            // Note that the unsupported extensions are based on WISE implementation, not format standard.
            if (!incompatible)
                return tex;

            var origImporter = (TextureImporter) AssetImporter.GetAtPath(texPath);

            // Standard blit won't work for normal textures due to Unity's way of storing data.
            // GltfBlit shader will convert Unity's imported normal format to proper pre-import RGB values.
            var mat = new Material(Shader.Find("Hidden/GltfBlit"));
            mat.SetKeyword("BLIT_NORMAL", origImporter.textureType == TextureImporterType.NormalMap);

            // Original texture might not be readable - use blit instead of copy or read/write pixels.
            var readWriteFormat = origImporter.sRGBTexture ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear;
            var tmp = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.Default, readWriteFormat);
            Graphics.Blit(tex, tmp, mat);
            var prevTarget = RenderTexture.active;
            var newTex = new Texture2D(tex.width, tex.height);
            newTex.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            newTex.Apply();
            RenderTexture.active = prevTarget;

            var bytes = newTex.EncodeToPNG();
            var fullPngPath = Path.Combine(tmpObjectPath, $"tmp_tex_{temporaryFiles.Count}.png");

            // Write PNG file to disk - FBX exporter scans materials for their original texture files.
            File.WriteAllBytes(fullPngPath, bytes);
            temporaryFiles.Add(fullPngPath);

            RenderTexture.ReleaseTemporary(tmp);

            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();

            // Copy settings from original importer to importer of new PNG file.
            var newImporter = (TextureImporter) AssetImporter.GetAtPath(fullPngPath);

            newImporter.textureType = origImporter.textureType;
            newImporter.sRGBTexture = origImporter.sRGBTexture;
            newImporter.alphaSource = origImporter.alphaSource;
            newImporter.alphaIsTransparency = origImporter.alphaIsTransparency;
            newImporter.ignorePngGamma = origImporter.ignorePngGamma;
            newImporter.isReadable = origImporter.isReadable;
            newImporter.streamingMipmaps = origImporter.streamingMipmaps;
            newImporter.mipmapEnabled = origImporter.mipmapEnabled;
            newImporter.wrapMode = origImporter.wrapMode;
            newImporter.filterMode = origImporter.filterMode;
            newImporter.SaveAndReimport();

            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();

            CoreUtils.Destroy(mat);

            // Return newly created texture asset already processed by Unity's import pipeline.
            var newAsset = AssetDatabase.LoadAssetAtPath<Texture>(fullPngPath);
            return newAsset;
        }

        /// <summary>
        /// Creates new instance of glTF exporter.
        /// </summary>
        /// <param name="mainAssetFile">Path to 3d asset that should be exported. Can be a prefab with multiple renderers.</param>
        /// <param name="assetGuid">GUID of the created package. Affects output file name.</param>
        /// <param name="assetName">Name of the exported object. Affects output file name.</param>
        public GltfExporter(string mainAssetFile, string assetGuid, string assetName)
        {
            var prefab = AssetDatabase.LoadAssetAtPath(mainAssetFile, typeof(GameObject));
            tempObject = UnityEngine.Object.Instantiate(prefab) as GameObject;

            glbFilename = $"{assetGuid}_vehicle_{assetName}.glb";
            tmpObjectPath = Path.Combine("Assets", "External", "Vehicles", assetName);

            PrepareObject(tempObject);
            fbxObjectPath = ModelExporter.ExportObject(Path.Combine(tmpObjectPath, $"{assetName}.fbx"), tempObject);
        }

        /// <summary>
        /// Exports model assigned to this instance through constructor as glTF file.
        /// </summary>
        /// <param name="outputFolder">Folder in which file should be placed.</param>
        /// <returns>Exported file name.</returns>
        public string Export(string outputFolder)
        {
            var glbOut = Path.Combine(outputFolder, glbFilename);
            var p = new System.Diagnostics.Process();
            p.EnableRaisingEvents = true;
            p.StartInfo.FileName = Path.Combine(Application.dataPath, "Plugins", "FBX2glTF",
                SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows ? "FBX2glTF-windows-x64.exe" : "FBX2glTF-linux-x64");
            p.StartInfo.Arguments = $"--binary --input {fbxObjectPath} --output {glbOut}";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.OutputDataReceived += (o, e) => Debug.Log(e.Data);
            p.ErrorDataReceived += (o, e) => Debug.Log(e.Data);

            p.Start();

            var infoOut = p.StandardOutput.ReadToEnd();
            var errOut = p.StandardError.ReadToEnd();
            if (!string.IsNullOrEmpty(infoOut))
                Debug.Log($"[glTF] {infoOut}");
            if (!string.IsNullOrEmpty(errOut))
                Debug.LogError($"[glTF] {errOut}");

            p.WaitForExit();

            var exitCode = p.ExitCode;
            if (exitCode != 0)
                throw new Exception($"glTF converter failed with exit code: {exitCode}");

            return glbFilename;
        }

        public void Dispose()
        {
            CoreUtils.Destroy(tempObject);

            File.Delete(fbxObjectPath);
            File.Delete($"{fbxObjectPath}.meta");

            foreach (var file in temporaryFiles)
            {
                File.Delete(file);
                File.Delete($"{file}.meta");
            }
        }
    }
}