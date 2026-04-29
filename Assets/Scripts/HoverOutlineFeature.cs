using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class HoverOutlineFeature : ScriptableRendererFeature
{
    public Material material;
    private ColorReplacePass _pass;

    public override void Create()
    {
        _pass = new ColorReplacePass(material)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (material != null)
            renderer.EnqueuePass(_pass);
    }
}

internal class ColorReplacePass : ScriptableRenderPass
{
    private readonly Material _material;
    
    public ColorReplacePass(Material mat)
    {
        _material = mat;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var resourceData = frameData.Get<UniversalResourceData>();
        
        if (_material == null || !resourceData.activeColorTexture.IsValid())
            return;

        var source = resourceData.activeColorTexture;
        var desc = renderGraph.GetTextureDesc(source);
        desc.name = "ColorReplaceTemp";
        desc.clearBuffer = false;
        
        var tempTexture = renderGraph.CreateTexture(desc);

        using (var builder = renderGraph.AddRasterRenderPass<PassData>("Color Replace Pass", out var passData))
        {
            passData.Material = _material;
            passData.Source = source;
            
            builder.UseTexture(source, AccessFlags.Read);
            builder.SetRenderAttachment(tempTexture, 0, AccessFlags.Write);
            builder.SetRenderFunc<PassData>((data, context) =>
            {
                Blitter.BlitTexture(context.cmd, data.Source, new Vector4(1, 1, 0, 0), data.Material, 0);
            });
        }

        using (var builder = renderGraph.AddRasterRenderPass<PassData>("Copy Back Pass", out var passData))
        {
            passData.Source = tempTexture;
    
            builder.UseTexture(tempTexture, AccessFlags.Read);
            builder.SetRenderAttachment(source, 0, AccessFlags.Write);
            builder.SetRenderFunc<PassData>((data, context) =>
            {
                Blitter.BlitTexture(context.cmd, data.Source, new Vector4(1, 1, 0, 0), 0, false);
            });
        }

    }

    private class PassData
    {
        public Material Material;
        public TextureHandle Source;
    }
}
