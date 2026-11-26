using System;
using KL.Drawing;
using ReLogic.Content;
using Terraria.GameContent;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

// 悬停与确认共享同一套曲线；确认后的排程仍与游戏控制器同步。
internal static class ShardbindReticle
{
    private const float ReticleExtent = 2.8f, NoiseRange = 9f;
    private static readonly Vector3 DeepColor = new(.1f, .32f, 1f);
    private static readonly Vector3 MidColor = new(.42f, .85f, 1f);

    internal static float Radius(NPC target) => MathHelper.Clamp(Math.Max(target.width, target.height) * .6f, 24f, 150f) * 1.2f;

    internal static void Draw(ReticlePresentation presentation, int elapsedTicks = -1)
    {
        if (elapsedTicks >= ShardbindSequence.LaunchTick) return;
        Effect effect = ModContent.Request<Effect>("伊蕾娜/Effects/Content/IceReticle", AssetRequestMode.ImmediateLoad).Value;
        Texture2D noise = ModContent.Request<Texture2D>("伊蕾娜/ElainaModSkills/Skills/Ice/Assets/ReticleNoise", AssetRequestMode.ImmediateLoad).Value;
        float radius = presentation.Tracking.Radius;
        Vector2 center = presentation.Tracking.Center;
        SetReticleParameters(effect, radius, elapsedTicks, presentation.Sample(elapsedTicks), presentation.Tracking.Opacity);
        EndBeginDraw(0, 1, ss: SamplerState.LinearClamp);
        GraphicsDevice gd = Main.instance.GraphicsDevice;
        gd.Textures[1] = noise;
        gd.SamplerStates[1] = SamplerState.LinearClamp;
        Texture2D quad = TextureAssets.MagicPixel.Value;
        float size = radius * ReticleExtent * 2f;
        foreach (EffectPass pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            DrawInWorld(quad, center, Color.White, new Vector2(size / quad.Width, size / quad.Height));
        }
        EndBeginDraw();
    }
    internal static void SetReticleParameters(Effect effect, float reticleRadius, float elapsedTicks, ReticleMotion.Pose pose, float opacity = 1f)
    {
        float lockProgress = MathHelper.Clamp(elapsedTicks / (float)ShardbindSequence.TightenTicks, 0f, 1f);
        float age = elapsedTicks < ShardbindSequence.TightenTicks ? -1f : (elapsedTicks - ShardbindSequence.TightenTicks) / 60f;
        float lk = MathHelper.Clamp(lockProgress, 0f, 1f);
        float grow = SmoothStep(0.5f, 1f, lk);

        Vector2 spike = new(0.17f * grow + 0.001f, 0.03f);
        Vector2 side = new(0.06f * grow + 0.001f, 0.018f);
        float creep = age < 0f ? 0f : MathHelper.Clamp(age / 0.9f, 0f, 1f);

        Vector3 ring = Vector3.Zero;
        Vector2 flash = Vector2.Zero;
        if (age is >= 0f and < 0.55f)
        {
            float k = age / 0.55f;
            float fade = (1f - k) * (1f - k);
            ring = new Vector3(1f + k * 1.1f, 0.012f * (1f - k) + 0.003f, fade * 1.3f);
            flash = new Vector2(0.9f * (0.6f + 0.4f * k), fade * 1.6f);
        }

        if (age >= 0f)
        {
            float attack = (float)ReticleMotion.Smooth(age / .05);
            ring.Z *= attack;
            flash.Y *= attack;
        }

        effect.Parameters["uHalfPx"].SetValue(reticleRadius * Main.GameViewMatrix.Zoom.X * 0.5f);
        effect.Parameters["uExtent"].SetValue(ReticleExtent);
        effect.Parameters["uTime"].SetValue((float)pose.Time);
        effect.Parameters["uAlpha"].SetValue((float)pose.Alpha * opacity);
        effect.Parameters["uDeep"].SetValue(DeepColor);
        effect.Parameters["uMid"].SetValue(MidColor);
        effect.Parameters["uS"].SetValue((float)pose.Scale);
        effect.Parameters["uE3"].SetValue((float)pose.Presence);
        effect.Parameters["uAmbient"].SetValue((float)pose.Ambient);
        effect.Parameters["uSpike"].SetValue(new Vector4(spike, 1f / spike.LengthSquared(), 0.17f * grow));
        effect.Parameters["uSide"].SetValue(new Vector4(side, 1f / side.LengthSquared(), grow * 0.8f));
        effect.Parameters["uGrow"].SetValue(grow);
        effect.Parameters["uFrost"].SetValue(new Vector2(Math.Max(0.75f * creep, 1e-4f), age >= 0f ? .8f * (float)ReticleMotion.Smooth(age / .12) : 0f));
        effect.Parameters["uNoiseRange"].SetValue(NoiseRange);
        effect.Parameters["uCross"].SetValue(new Vector2((float)Math.Cos(pose.Angle), (float)Math.Sin(pose.Angle)));
        effect.Parameters["uG0"].SetValue((float)pose.Gap);
        effect.Parameters["uPulse"].SetValue((float)pose.Pulse);
        effect.Parameters["uMoteK"].SetValue((float)pose.Motes);
        effect.Parameters["uRing"].SetValue(ring);
        effect.Parameters["uFlash"].SetValue(flash);
    }
    private static float SmoothStep(float edge0, float edge1, float x)
    {
        float t = MathHelper.Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
        return t * t * (3f - 2f * t);
    }
}
