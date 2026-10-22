using System;
using KL.Drawing;

namespace 伊蕾娜.ElainaModSkills.Skills.Water;

/// <summary>游戏与 FNA 预览共享的水射线绘制；资源归 ModContent 管理。</summary>
public sealed class WaterLaserVisuals : ModSystem
{
    internal const string AssetPath = "伊蕾娜/ElainaModSkills/Skills/Water/";
    internal const string SplashTexturePath = AssetPath + "Eff_Splash_4001_Clear";
    internal const string FoamTexturePath = AssetPath + "Eff_Splash_4002_Clear";
    internal const LayerDrawRequestSystem.DrawTargetLayer Layer = LayerDrawRequestSystem.DrawTargetLayer.Projectiles;
    private static Asset<Texture2D> splash, foam, glow;
    private static WaterLaserFlowRenderer flowRenderer;

    public override void Unload()
    {
        splash = foam = glow = null;
        WaterLaserFlowRenderer renderer = flowRenderer;
        flowRenderer = null;
        if (renderer != null) Main.QueueMainThreadAction(renderer.Dispose);
    }

    internal static void BeginSprites()
    {
        // 原始水花是直通 alpha；正常透明混合保留白沫，避免叠成发光节点。
        EndBeginDraw(state: 2, defferred: 1, ss: SamplerState.LinearClamp);
    }

    internal static void DrawBeam(Vector2[] points, float length, int time)
    {
        if (points.Length < 2 || length <= 0.01f) return;
        flowRenderer ??= new WaterLaserFlowRenderer(Main.instance.GraphicsDevice,
            ModContent.Request<Effect>("伊蕾娜/Effects/Content/WaterChargeFlow", AssetRequestMode.ImmediateLoad).Value,
            path => ModContent.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad).Value);
        Main.spriteBatch.End();
        try
        {
            flowRenderer.Draw(points, Main.screenPosition, time / 60f, Main.GameViewMatrix.TransformationMatrix);
        }
        finally
        {
            BeginDraw();
        }
    }

    internal static void DrawMuzzle(Vector2 origin, Vector2 direction, int time)
    {
        glow ??= ModContent.Request<Texture2D>(AssetPath + "WaterLaserGlow", AssetRequestMode.ImmediateLoad);
        float pulse = 1f + MathF.Sin(time * 0.21f) * 0.08f;
        Vector2 center = origin - Main.screenPosition;
        Texture2D texture = glow.Value;
        Vector2 pivot = new(texture.Width * 0.5f, texture.Height * 0.5f);
        float rotation = MathF.Atan2(direction.Y, direction.X);
        Main.spriteBatch.Draw(texture, center, null, new Color(50, 148, 235) * 0.52f,
            rotation, pivot, new Vector2(54f, 42f) * pulse / texture.Width, SpriteEffects.None, 0);
        Main.spriteBatch.Draw(texture, center + direction * 2f, null, new Color(156, 236, 255),
            rotation, pivot, new Vector2(23f, 20f) * pulse / texture.Width, SpriteEffects.None, 0);
        // 贴着杖尖持续绘制两股交错水舌；不会因随机粒子缺帧而露出射线平切口。
        for (int i = 0; i < 2; i++)
        {
            float phase = ((time + i * 9) % 18) / 18f;
            DrawParticle(origin + direction * (7f + phase * 6f), rotation,
                new Vector2(42f, 20f), phase * 0.72f, i, false, new Color(130, 223, 255));
        }
    }

    internal static void DrawParticle(Vector2 position, float rotation, Vector2 size,
        float progress, int variant, bool isFoam, Color color)
    {
        progress = MathHelper.Clamp(progress, 0f, 1f);
        if (progress >= 1f) return;
        Texture2D texture;
        Rectangle frame;
        if (isFoam)
        {
            foam ??= ModContent.Request<Texture2D>(FoamTexturePath, AssetRequestMode.ImmediateLoad);
            texture = foam.Value;
            int index = Math.Min(15, (int)(progress * 16f));
            frame = new Rectangle(index % 4 * (texture.Width / 4), index / 4 * (texture.Height / 4),
                texture.Width / 4, texture.Height / 4);
        }
        else
        {
            splash ??= ModContent.Request<Texture2D>(SplashTexturePath, AssetRequestMode.ImmediateLoad);
            texture = splash.Value;
            // 两列独立变体、六行动画；1024 不能被 6 整除，按边界计算避免累积错帧。
            int row = Math.Min(5, (int)(progress * 6f));
            int top = row * texture.Height / 6;
            frame = new Rectangle((variant & 1) * (texture.Width / 2), top,
                texture.Width / 2, (row + 1) * texture.Height / 6 - top);
        }
        float fade = 1f - MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((progress - 0.45f) / 0.55f, 0f, 1f));
        float expansion = MathHelper.Lerp(0.82f, 1.12f, progress);
        color = Color.Lerp(color, new Color(245, 253, 255, color.A), .82f);
        color.A = (byte)(color.A * fade);
        Main.spriteBatch.Draw(texture, position - Main.screenPosition, frame, color,
            rotation, new Vector2(frame.Width, frame.Height) * 0.5f,
            size * expansion / new Vector2(frame.Width, frame.Height), SpriteEffects.None, 0);
    }
}
