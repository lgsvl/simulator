namespace Simulator.Sensors.Postprocessing
{
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.HighDefinition;

    public struct PostProcessPassContext
    {
        /// <summary>
        /// Command Buffer, used to enqueue graphic commands to the GPU.
        /// </summary>
        public readonly CommandBuffer cmd;

        /// <summary>
        /// HdCamera, HDRP data related to the rendering camera. Use the camera property to access the Camera class.
        /// </summary>
        public readonly HDCamera hdCamera;

        /// <summary>
        /// Camera color buffer.
        /// </summary>
        public readonly RTHandle cameraColorBuffer;

        /// <summary>
        /// Camera depth buffer.
        /// </summary>
        public readonly RTHandle cameraDepthBuffer;

        public PostProcessPassContext(CustomPassContext ctx)
        {
            cmd = ctx.cmd;
            hdCamera = ctx.hdCamera;
            cameraColorBuffer = ctx.cameraColorBuffer;
            cameraDepthBuffer = ctx.cameraDepthBuffer;
        }

        public PostProcessPassContext(CommandBuffer cmd, HDCamera hdCamera, RTHandle cameraColorBuffer, RTHandle cameraDepthBuffer)
        {
            this.cmd = cmd;
            this.hdCamera = hdCamera;
            this.cameraColorBuffer = cameraColorBuffer;
            this.cameraDepthBuffer = cameraDepthBuffer;
        }

        public PostProcessPassContext(CommandBuffer cmd, HDCamera hdCamera, SensorRenderTarget target) 
            : this(cmd, hdCamera, target.ColorHandle, target.DepthHandle)
        {
        }
    }
}