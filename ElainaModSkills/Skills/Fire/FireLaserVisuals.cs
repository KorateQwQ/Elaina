using KL.Drawing;

namespace 伊蕾娜.ElainaModSkills.Skills.Fire;

/// <summary>游戏与 FNA 预览共享的火焰射线主体与缎带绘制入口；资源归 ModContent 管理。</summary>
public sealed class FireLaserVisuals : ModSystem
{
    internal const LayerDrawRequestSystem.DrawTargetLayer Layer = LayerDrawRequestSystem.DrawTargetLayer.Projectiles;
    private static FireLaserRibbonRenderer renderer;
    private static FireLaserBodyRenderer bodyRenderer;

    public override void Unload()
    {
        FireLaserRibbonRenderer current = renderer;
        FireLaserBodyRenderer body = bodyRenderer;
        renderer = null;
        bodyRenderer = null;
        if (current != null) Main.QueueMainThreadAction(current.Dispose);
        if (body != null) Main.QueueMainThreadAction(body.Dispose);
    }

    private static Texture2D Load(string path) => ModContent.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad).Value;

    /// <summary>在 Bloom 请求回调中调用；结束并恢复调用方的 SpriteBatch。</summary>
    internal static void DrawBody(Vector2 start, Vector2 direction, float length, int time)
    {
        bodyRenderer ??= new FireLaserBodyRenderer(Main.instance.GraphicsDevice,
            ModContent.Request<Effect>("伊蕾娜/Effects/Content/FireLaserBody", AssetRequestMode.ImmediateLoad).Value, Load);
        Main.spriteBatch.End();
        try
        {
            bodyRenderer.Draw(start, direction, length, time / 60f, Main.screenPosition,
                Main.GameViewMatrix.TransformationMatrix);
        }
        finally
        {
            BeginDraw();
        }
    }

    /// <summary>在 Bloom 请求回调中调用；结束并恢复调用方的 SpriteBatch。</summary>
    internal static void DrawRibbons(Vector2 start, Vector2 direction, float length, int time, bool foreground)
    {
        renderer ??= new FireLaserRibbonRenderer(Main.instance.GraphicsDevice,
            ModContent.Request<Effect>("伊蕾娜/Effects/Content/FireLaserRibbon", AssetRequestMode.ImmediateLoad).Value, Load);
        Main.spriteBatch.End();
        try
        {
            renderer.Draw(start, direction, length, time / 60f, foreground, Main.screenPosition,
                Main.GameViewMatrix.TransformationMatrix);
        }
        finally
        {
            BeginDraw();
        }
    }
}
