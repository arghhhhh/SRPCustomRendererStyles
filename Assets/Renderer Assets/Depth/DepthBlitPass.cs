using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DepthBlitPass : ScriptableRenderPass
{
    private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("DepthBlit");
    private Material m_Material;

    private RenderTextureDescriptor textureDescriptor;
    private RTHandle textureHandle;

    public DepthBlitPass(Material material)
    {
        m_Material = material;
        renderPassEvent = RenderPassEvent.AfterRendering;

        textureDescriptor = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Default, 0);
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        base.Configure(cmd, cameraTextureDescriptor);

        textureDescriptor.width = cameraTextureDescriptor.width;
        textureDescriptor.height = cameraTextureDescriptor.height;

        RenderingUtils.ReAllocateIfNeeded(ref textureHandle, textureDescriptor);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var cameraData = renderingData.cameraData;
        if (cameraData.camera.cameraType != CameraType.Game)
            return;

        if (m_Material == null)
            return;

        CommandBuffer cmd = CommandBufferPool.Get();
        RTHandle cameraTargetHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;

        using (new ProfilingScope(cmd, m_ProfilingSampler))
        {
            Blit(cmd, cameraTargetHandle, textureHandle, m_Material);
            Blit(cmd, textureHandle, cameraTargetHandle, m_Material);
        }
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public void Dispose()
    {
    #if UNITY_EDITOR
        if (EditorApplication.isPlaying)
        {
            Object.Destroy(m_Material);
        }
        else
        {
            Object.DestroyImmediate(m_Material);
        }
    #else
        Object.Destroy(material);
    #endif

        if (textureHandle != null) textureHandle.Release();
    }
}
