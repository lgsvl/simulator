using System.IO;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class CreateCustomRenderTextureGraph : EndNameEditAction
    {
        [MenuItem("Assets/Create/Shader/Custom Render Texture Graph", false, 201)]
        public static void CreateMaterialGraph()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0, CreateInstance<CreateCustomRenderTextureGraph>(), "New Shader Graph.shadergraph", null, null);
        }

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var graph = new GraphData();
            graph.AddNode(new CustomRenderTextureNode());
            graph.path = "Shader Graphs";
            File.WriteAllText(pathName, EditorJsonUtility.ToJson(graph));
            AssetDatabase.Refresh();
        }
    }
}