using UnityEngine;

using System.IO;
using System.Linq;

namespace Parabox.Stl.Editor
{
    [UnityEditor.AssetImporters.ScriptedImporter(1, "stl")]
    public class StlImporter : UnityEditor.AssetImporters.ScriptedImporter
    {
        [SerializeField]
        CoordinateSpace m_CoordinateSpace;

        [SerializeField]
        UpAxis m_UpAxis;

        [SerializeField]
        bool m_Smooth;

        public override void OnImportAsset(UnityEditor.AssetImporters.AssetImportContext ctx)
        {
            var name = Path.GetFileNameWithoutExtension(ctx.assetPath);
            var meshes = Importer.Import(ctx.assetPath, m_CoordinateSpace, m_UpAxis, m_Smooth).ToArray();

            if(meshes.Length < 1)
                return;

            if(meshes.Length < 2)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.DestroyImmediate(go.GetComponent<BoxCollider>());
                go.name = name;
                meshes[0].name = "Mesh-" + name;
                go.GetComponent<MeshFilter>().sharedMesh = meshes[0];

                ctx.AddObjectToAsset(go.name, go);
                ctx.AddObjectToAsset(meshes[0].name, meshes[0]);
                ctx.SetMainObject(go);
            }
            else
            {
                var parent = new GameObject();
                parent.name = name;

                for(int i = 0, c = meshes.Length; i < c; i++)
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Object.DestroyImmediate(go.GetComponent<BoxCollider>());
                    go.transform.SetParent(parent.transform, false);
                    go.name = name + "(" + i + ")";

                    var mesh = meshes[i];
                    mesh.name = "Mesh-" + name + "(" + i + ")";
                    go.GetComponent<MeshFilter>().sharedMesh = mesh;

                    // ctx.AddObjectToAsset(go.name, go);
                    ctx.AddObjectToAsset(mesh.name, mesh);
                }

                ctx.AddObjectToAsset(parent.name, parent);
                ctx.SetMainObject(parent);
            }
        }
    }
}
