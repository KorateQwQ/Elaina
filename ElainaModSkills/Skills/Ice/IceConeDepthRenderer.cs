namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

// Each world layer shares one depth buffer across all Shardbind cones.
// Terraria's screenTarget has no depth attachment of its own.
public sealed class IceConeDepthRenderer : ModSystem
{
    private static RenderTarget2D depthTarget;
    private static RenderTarget2D sceneCopy;
    private static RenderTarget2D worldSceneCopy;
    private static RenderTarget2D hdrSceneCopy;
    private static RenderTargetBinding[] previousTargets;
    private static Viewport previousViewport;
    private static Rectangle previousScissor;
    private static RenderTargetUsage previousBackbufferUsage;
    private static bool restoreScene;

    // Store premultiplied RGB with coverage alpha for the final AlphaBlend composite.
    internal static readonly BlendState ModelBlend = new()
    {
        ColorSourceBlend = Blend.SourceAlpha,
        ColorDestinationBlend = Blend.InverseSourceAlpha,
        AlphaSourceBlend = Blend.One,
        AlphaDestinationBlend = Blend.InverseSourceAlpha
    };

    internal static void BeginLayer(GraphicsDevice gd)
    {
        previousTargets = gd.GetRenderTargets();
        previousViewport = gd.Viewport;
        previousScissor = gd.ScissorRectangle;
        previousBackbufferUsage = gd.PresentationParameters.RenderTargetUsage;
        restoreScene = false;

        // 两层复用深度目标，前景保留 HDR 值供合成时提取 Bloom。
        EnsureTarget(ref depthTarget, gd, previousViewport.Width, previousViewport.Height,
            SurfaceFormat.Vector4, DepthFormat.Depth24);

        // A DiscardContents destination loses its color on rebind. Keep a copy first.
        if (previousTargets.Length > 0 && previousTargets[0].RenderTarget is RenderTarget2D scene &&
            scene.RenderTargetUsage != RenderTargetUsage.PreserveContents)
        {
            // 世界场景与 HDR 捕获格式不同，分开缓存，避免切层时反复分配。
            ref RenderTarget2D cachedCopy = ref (scene.Format == SurfaceFormat.Vector4 ? ref hdrSceneCopy : ref worldSceneCopy);
            EnsureTarget(ref cachedCopy, gd, scene.Width, scene.Height, scene.Format, DepthFormat.None);
            sceneCopy = cachedCopy;
            gd.SetRenderTarget(sceneCopy);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone);
            Main.spriteBatch.Draw(scene, Vector2.Zero, Color.White);
            Main.spriteBatch.End();
            restoreScene = true;
        }
        else if (previousTargets.Length == 0)
        {
            // FNA also honors DiscardContents when rebinding the backbuffer.
            gd.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
        }

        gd.SetRenderTarget(depthTarget);
        gd.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Transparent, 1f, 0);
    }

    internal static void EndLayer(GraphicsDevice gd)
    {
        try
        {
            gd.SetRenderTargets(previousTargets);
            if (restoreScene)
            {
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp,
                    DepthStencilState.None, RasterizerState.CullNone);
                Main.spriteBatch.Draw(sceneCopy, Vector2.Zero, Color.White);
                Main.spriteBatch.End();
            }

            gd.Viewport = previousViewport;
            gd.ScissorRectangle = previousScissor;
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone);
            Main.spriteBatch.Draw(depthTarget, Vector2.Zero, Color.White);
            Main.spriteBatch.End();
        }
        finally
        {
            gd.PresentationParameters.RenderTargetUsage = previousBackbufferUsage;
            gd.Viewport = previousViewport;
            gd.ScissorRectangle = previousScissor;
            previousTargets = null;
        }
    }

    private static void EnsureTarget(ref RenderTarget2D target, GraphicsDevice gd, int width, int height,
        SurfaceFormat format, DepthFormat depth)
    {
        if (target != null && !target.IsDisposed && target.GraphicsDevice == gd &&
            target.Width == width && target.Height == height && target.Format == format)
        {
            return;
        }

        target?.Dispose();
        target = new RenderTarget2D(gd, width, height, false, format, depth);
    }

    public override void Unload()
    {
        Main.QueueMainThreadAction(() =>
        {
            depthTarget?.Dispose();
            hdrSceneCopy?.Dispose();
            worldSceneCopy?.Dispose();
            hdrSceneCopy = worldSceneCopy = null;
            depthTarget = null;
            sceneCopy = null;
        });
    }
}
