namespace Simulator.Sensors
{
    using Simulator.Utilities;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Unity.Collections.LowLevel.Unsafe;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.HighDefinition;

    public class LaneLineSensorPass : CustomPass
    {
        public struct Line
        {
            public Matrix4x4 transform;
            public Vector3 start;
            public Vector3 end;
            public Vector3 color;
        };
        
        private const float LineWidth = 8f;
        private const int BufferGranularity = 1024;
        private static readonly int WidthProperty = Shader.PropertyToID("_LineWidth");
        private static readonly int BufferProperty = Shader.PropertyToID("_Lines");
        
        private Material lineMaterial;
        private ComputeBuffer buffer;

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            lineMaterial = new Material(RuntimeSettings.Instance.LineShader);
            lineMaterial.SetFloat(WidthProperty, LineWidth * Utility.GetDpiScale());
            base.Setup(renderContext, cmd);
        }

        private void VerifyBuffer(int requiredCount)
        {
            if (buffer == null || requiredCount > buffer.count)
            {
                buffer?.Release();

                var newCount = ((requiredCount + BufferGranularity - 1) / BufferGranularity) * BufferGranularity;
                buffer = new ComputeBuffer(newCount, UnsafeUtility.SizeOf<Line>());
                lineMaterial.SetBuffer(BufferProperty, buffer);
            }
        }

        protected override void Execute(CustomPassContext ctx)
        {
            var sensors = ctx.hdCamera.camera.GetComponents<SensorBase>();
            var sensor = sensors.FirstOrDefault<SensorBase>(s => s.GetType().GetCustomAttribute<SensorType>().Name == "LaneLineSensor");
            if (sensor == null)
                return;

            var lineCount = ((List<Line>)sensor.GetType().GetField("linesToRender").GetValue(sensor)).Count;
            if (lineCount == 0)
                return;

            VerifyBuffer(lineCount);
            buffer.SetData((List<Line>)sensor.GetType().GetField("linesToRender").GetValue(sensor), 0, 0, lineCount);
            CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer, ctx.cameraDepthBuffer, ClearFlag.None);
            ctx.cmd.DrawProcedural(Matrix4x4.identity, lineMaterial, 0, MeshTopology.Points, lineCount);
        }
        
        protected override void Cleanup()
        {
            buffer?.Release();
            CoreUtils.Destroy(lineMaterial);
            base.Cleanup();
        }
    }
}