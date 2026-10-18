using System;
using KL.Drawing;

namespace 伊蕾娜.ElainaModSkills.Skills.Water;

/// <summary>游戏与 FNA 预览共享的水射线绘制；资源归 ModContent 管理。</summary>
public sealed class WaterLaserVisuals : ModSystem
{
    internal const string AssetPath = "伊蕾娜/ElainaModSkills/Skills/Water/";
    internal const LayerDrawRequestSystem.DrawTargetLayer Layer = LayerDrawRequestSystem.DrawTargetLayer.Dust;
    private static Asset<Texture2D> splash, foam, glow, noise, flow, mask;

    public override void Unload()
    {
        splash = foam = glow = noise = flow = mask = null;
    }

    internal static void BeginSprites()
    {
        // Color 顶点只能保存 0..1；统一 shader 将高光提升到 Bloom 提取阈值以上。
        // ReColor 有多个功能 pass，不能作为 SpriteBatch 的多 pass Effect 整体提交。
        EndBeginDraw(state: 1, defferred: 1, ss: SamplerState.LinearClamp);
        ReColorEffect(new Vector4(1.45f, 1.45f, 1.45f, 1f));
    }

    internal static void DrawBeam(Vector2[] points, float length, int time)
    {
        if (points.Length < 2 || length <= 0.01f) return;
        noise ??= ModContent.Request<Texture2D>("KL/Effects/Tex/Noise/5", AssetRequestMode.ImmediateLoad);
        flow ??= ModContent.Request<Texture2D>("KL/Effects/Tex/Trail/noi_1", AssetRequestMode.ImmediateLoad);
        mask ??= ModContent.Request<Texture2D>("KL/Effects/Tex/Noise/T_VFX_Noise_1", AssetRequestMode.ImmediateLoad);
        float inverseLength = 1000f / Math.Max(length, 24f);
        Vector2 scroll = new(-(time % 1800) / 20f, 0);
        Vector2 scale = new(0.5f * inverseLength, 1f);
        Vector4 waterColor = new(0.12f, 0.95f, 1.55f, 1f);
        TrailEffect(noise.Value, points, waterColor, waterColor,
            2.8f, 8f, startAlpha: 1f, endAlpha: 1f, blendState: 2,
            uTime: scroll, imageScale: scale, clipMask: noise.Value,
            maskTime: scroll, threshold: 0.4f, maskScale: scale, useRforAlpha: true);
        Vector4 crestColor = new(1.15f, 1.85f, 2.2f, 1f);
        TrailEffect(flow.Value, points, crestColor, crestColor,
            1.6f, 4.8f, startAlpha: 1f, endAlpha: 1f, blendState: 1,
            uTime: scroll, imageScale: new Vector2(1.5f * inverseLength, 1f),
            clipMask: mask.Value, maskTime: scroll * 2f, threshold: 0.2f,
            maskScale: new Vector2(0.25f * inverseLength, 1f));
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
            foam ??= ModContent.Request<Texture2D>(AssetPath + "Eff_Splash_4002", AssetRequestMode.ImmediateLoad);
            texture = foam.Value;
            int index = Math.Min(15, (int)(progress * 16f));
            frame = new Rectangle(index % 4 * (texture.Width / 4), index / 4 * (texture.Height / 4),
                texture.Width / 4, texture.Height / 4);
        }
        else
        {
            splash ??= ModContent.Request<Texture2D>(AssetPath + "Eff_Splash_4001", AssetRequestMode.ImmediateLoad);
            texture = splash.Value;
            // 两列独立变体、六行动画；1024 不能被 6 整除，按边界计算避免累积错帧。
            int row = Math.Min(5, (int)(progress * 6f));
            int top = row * texture.Height / 6;
            frame = new Rectangle((variant & 1) * (texture.Width / 2), top,
                texture.Width / 2, (row + 1) * texture.Height / 6 - top);
        }
        float fade = 1f - MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((progress - 0.45f) / 0.55f, 0f, 1f));
        float expansion = MathHelper.Lerp(0.82f, 1.12f, progress);
        color.A = (byte)(color.A * fade);
        Main.spriteBatch.Draw(texture, position - Main.screenPosition, frame, color,
            rotation, new Vector2(frame.Width, frame.Height) * 0.5f,
            size * expansion / new Vector2(frame.Width, frame.Height), SpriteEffects.None, 0);
    }
}
