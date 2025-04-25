using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TankMaskPass : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public LayerMask layerMask = -1;
        public Material overrideMaterial;
        public RenderTexture targetTexture;
    }

    public Settings settings = new Settings();
    TankMaskRenderPass pass;

    public override void Create()
    {
        pass = new TankMaskRenderPass(settings.layerMask, settings.overrideMaterial, settings.targetTexture);
        pass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.overrideMaterial && settings.targetTexture)
            renderer.EnqueuePass(pass);
    }

    class TankMaskRenderPass : ScriptableRenderPass
    {
        LayerMask layerMask;
        Material overrideMaterial;
        RenderTargetIdentifier target;

        public TankMaskRenderPass(LayerMask mask, Material mat, RenderTexture tex)
        {
            layerMask = mask;
            overrideMaterial = mat;
            target = new RenderTargetIdentifier(tex);
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            ConfigureTarget(target);
            ConfigureClear(ClearFlag.All, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // var filtering = new FilteringSettings(RenderQueueRange.opaque, layerMask);
            // var draw = CreateDrawingSettings(new ShaderTagId("SRPDefaultUnlit"), ref renderingData, SortingCriteria.CommonOpaque);
            // draw.overrideMaterial = overrideMaterial;
            //
            // context.DrawRenderers(renderingData.cullResults, ref draw, ref filtering);
            
            CommandBuffer cmd = CommandBufferPool.Get("Debug Blit Mask");
            cmd.Blit(Texture2D.whiteTexture, target);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}