using System.Collections.Generic;
using System.IO;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class CustomRenderTextureSubShader : ICustomRenderTextureSubShader
    {
        public int GetPreviewPassIndex() => 0;

        private static bool GenerateShaderPass(CustomRenderTextureNode masterNode, GenerationMode mode, ShaderGenerator result, List<string> sourceAssetDependencyPaths)
        {
            string templateLocation = Path.Combine(
                HDUtils.GetHDRenderPipelinePath(),
                "Editor",
                "CustomRenderTextureShaderGraph",
                "CustomRenderTexturePass.template");

            if (!File.Exists(templateLocation))
            {
                Debug.LogError("Template not found: " + templateLocation);
                return false;
            }

            var activeFields = new HashSet<string>() { "uv0" };

            var pixelNodes = ListPool<AbstractMaterialNode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(pixelNodes, masterNode, NodeUtils.IncludeSelf.Include, CustomRenderTextureNode.AllSlots);

            var pixelRequirements = ShaderGraphRequirements.FromNodes(pixelNodes, ShaderStageCapability.Fragment, false);

            var graphNodeFunctions = new ShaderStringBuilder();
            graphNodeFunctions.IncreaseIndent();
            var functionRegistry = new FunctionRegistry(graphNodeFunctions);

            var pixelSlots = HDSubShaderUtilities.FindMaterialSlotsOnNode(CustomRenderTextureNode.AllSlots, masterNode);

            // build the graph outputs structure to hold the results of each active slots (and fill out activeFields to indicate they are active)
            string pixelGraphInputStructName = "SurfaceDescriptionInputs";
            string pixelGraphOutputStructName = "SurfaceDescriptionOutputs";
            string pixelGraphEvalFunctionName = "SurfaceDescriptionFunction";

            var sharedProperties = new PropertyCollector();
            var pixelGraphEvalFunction = new ShaderStringBuilder();

            // Build the graph evaluation code, to evaluate the specified slots
            GraphUtil.GenerateSurfaceDescriptionFunction(
                pixelNodes,
                masterNode,
                masterNode.owner as GraphData,
                pixelGraphEvalFunction,
                functionRegistry,
                sharedProperties,
                pixelRequirements,
                mode,
                pixelGraphEvalFunctionName,
                pixelGraphOutputStructName,
                null,
                pixelSlots,
                pixelGraphInputStructName);

            // build graph inputs structures
            ShaderGenerator pixelGraphInputs = new ShaderGenerator();
            ShaderSpliceUtil.BuildType(typeof(HDRPShaderStructs.SurfaceDescriptionInputs), activeFields, pixelGraphInputs);

            // build graph code
            var graph = new ShaderGenerator();
            {
                graph.AddShaderChunk("// Shared Graph Properties (uniform inputs)");
                graph.AddShaderChunk(sharedProperties.GetPropertiesDeclaration(1, mode));

                graph.AddShaderChunk("// Shared Graph Node Functions");
                graph.AddShaderChunk(graphNodeFunctions.ToString());

                graph.AddShaderChunk("// Pixel Graph Evaluation");
                graph.Indent();
                graph.AddShaderChunk(pixelGraphEvalFunction.ToString());
                graph.Deindent();
            }

            var namedFragments = new Dictionary<string, string>()
            {
                { "Graph", graph.GetShaderString(2, false) },
            };

            string sharedTemplatePath = Path.Combine(HDUtils.GetHDRenderPipelinePath(), "Editor", "ShaderGraph");

            string buildTypeAssemblyNameFormat = "UnityEditor.Experimental.Rendering.HDPipeline.HDRPShaderStructs+{0}, " + typeof(HDSubShaderUtilities).Assembly.FullName.ToString();

            var templatePreprocessor =
                new ShaderSpliceUtil.TemplatePreprocessor(activeFields, namedFragments, false, sharedTemplatePath, sourceAssetDependencyPaths, buildTypeAssemblyNameFormat);

            templatePreprocessor.ProcessTemplateFile(templateLocation);

            result.AddShaderChunk(templatePreprocessor.GetShaderCode().ToString(), false);

            return true;
        }

        public string GetSubshader(IMasterNode iMasterNode, GenerationMode mode, List<string> sourceAssetDependencyPaths = null)
        {
            var masterNode = iMasterNode as CustomRenderTextureNode;

            var subShader = new ShaderGenerator();
            subShader.AddShaderChunk("SubShader", false);
            subShader.AddShaderChunk("{", false);
            subShader.Indent();
            {
                GenerateShaderPass(masterNode, mode, subShader, sourceAssetDependencyPaths);
            }
            subShader.Deindent();
            subShader.AddShaderChunk("}", false);

            return subShader.GetShaderString(0);
        }

        public bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset) => true;
    }
}
