/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

namespace Simulator.PointCloud.Trees
{
    using System.IO;
    using System.Linq;
    using Utilities;
    using UnityEngine;
    using UnityEngine.Rendering;
    using Utilities.Attributes;

    /// <summary>
    /// Class used to manage instance of a single node tree in runtime.
    /// </summary>
    [ExecuteInEditMode]
    public class NodeTreeLoader : MonoBehaviour
    {
        private const string MeshRootName = "CollidersRoot";
        
#pragma warning disable 0649

        [SerializeField]
        [PathSelector(SelectDirectory = true, TruncateToRelative = true)]
        [Tooltip("Path under which data for this tree is stored. Must exist.")]
        private string dataPath = "";

        [SerializeField]
        [Tooltip("Maximum amount of points that can be loaded into memory at once.")]
        private int pointLimit = 10000000;
        
        [SerializeField]
        [Tooltip("If true, meshes generated during import process will be loaded on tree initialization.")]
        private bool loadMeshes = true;
        
        [SerializeField]
        [HideInInspector]
        private Transform meshesRoot;

#pragma warning restore 0649

        private bool corrupted;
        private bool verboseLoad;

        private string lastUsedDataPath;
        private string dataHash;
        
        private NodeTree tree;

        public NodeTree Tree
        {
            get
            {
                if (!enabled)
                    return null;
                
                if (!string.Equals(dataPath, lastUsedDataPath))
                    Cleanup();
                
                if (tree == null && !corrupted && !string.IsNullOrEmpty(dataPath))
                {
                    if (!NodeTree.TryLoadFromDisk(dataPath, pointLimit, dataHash, out tree))
                    {
                        var notBundleScene = Application.isPlaying && gameObject.scene.buildIndex != -1 || !Application.isPlaying;
                        if (verboseLoad || notBundleScene)
                        {
                            Debug.LogError($"Unable to load octree under path {dataPath}. Check files.");
                        }

                        corrupted = true;
                        tree = null;
                    }
                    else if (loadMeshes) 
                    {
                        LoadMeshes();
                    }

                    verboseLoad = false;
                }

                lastUsedDataPath = dataPath;

                return tree;
            }
        }

        public string GetDataPath()
        {
            return dataPath;
        }

        public string GetFullDataPath()
        {
            return Utility.GetFullPath(dataPath);
        }

        public void UpdateData(string newDataPath)
        {
            Cleanup();
            verboseLoad = true;
            dataPath = newDataPath;
            dataHash = null;
        }

        public void UpdateData(string newDataPath, string newDataHash)
        {
            Cleanup();
            verboseLoad = true;
            dataPath = newDataPath;
            dataHash = newDataHash;
        }

        private void OnDisable()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            ClearMeshes();
            corrupted = false;
            tree?.Dispose();
            tree = null;
        }

        private void ClearMeshes()
        {
            for (var i = transform.childCount - 1; i >= 0; --i)
            {
                var child = transform.GetChild(i).gameObject;
                if (child.name == MeshRootName)
                    CoreUtils.Destroy(child);
            }
        }

        private void LoadMeshes()
        {
            ClearMeshes();

            var useZip = tree.ZipData != null;
            var fullDataPath = GetFullDataPath();

            var files = useZip
                ? tree.ZipData.EnumerateFiles.Where(x => x.EndsWith(TreeUtility.MeshFileExtension)).ToArray()
                : Directory.GetFiles(fullDataPath, $"*{TreeUtility.MeshFileExtension}");

            if (files.Length == 0)
                return;

            meshesRoot = new GameObject(MeshRootName).transform;
            meshesRoot.SetParent(transform);
            meshesRoot.Reset();

            foreach (var file in files)
            {
                var fileName = useZip ? fullDataPath : file;
                var offset = useZip ? tree.ZipData.GetEntryOffset(file) : 0;
                var size = useZip ? tree.ZipData.GetEntrySize(file) : new FileInfo(file).Length;
                
                var data = MeshData.LoadFromFile(fileName, offset, size);
                var meshes = data.GenerateMeshes();
                foreach (var mesh in meshes)
                {
                    var fName = Path.GetFileNameWithoutExtension(file);
                    var go = new GameObject($"MeshCollider ({fName})");
                    go.transform.SetParent(meshesRoot);
                    go.transform.Reset();
                    var col = go.AddComponent<MeshCollider>();
                    col.sharedMesh = mesh;
                }
            }
        }
    }
}