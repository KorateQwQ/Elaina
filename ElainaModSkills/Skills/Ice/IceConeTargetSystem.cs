using System;
using System.Collections.Generic;
using KL.SkillSystem;
using Terraria.GameContent;
using Terraria.UI;
using 伊蕾娜.Items;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

/// <summary>
/// 冰锥/冰凌锁的客户端索敌：锁定碰撞箱距离鼠标最近的敌人，
/// 并在目标身上绘制「寒霜菱形框」准星（Effects/Content/IceReticle.fx）。
/// </summary>
public class IceConeTargetSystem : ModSystem
{
    // 鼠标到敌人碰撞箱的最大锁定距离（世界像素）
    private const float LockRange = 400f;
    // 已锁定的目标放宽保持距离，避免在边界来回闪烁
    private const float KeepRangeMultiplier = 1.15f;

    private const float Dt = 1f / 60f;
    private const float LockDuration = 0.35f;
    // 绘制四边形的半边长（以准星半径为单位），需要容纳最外圈的冰尘和冲击环
    private const float ReticleExtent = 2.8f;

    private const int NoiseSize = 512;
    // 霜纹只出现在锁定后收紧的菱形框内，fbm(p * 7) 的采样范围不超过 ±9
    private const float NoiseRange = 9f;

    private static readonly Vector3 DeepColor = new(0.1f, 0.32f, 1f);
    private static readonly Vector3 MidColor = new(0.42f, 0.85f, 1f);

    private static int targetIndex = -1;
    private static float lockProgress;
    private static float alpha;
    // 完全锁定后经过的秒数，未锁定为 -1；驱动霜纹蔓延和锁定闪光
    private static float age = -1f;
    private static Vector2 reticleCenter;
    private static float reticleRadius;
    private static Texture2D noiseTexture;

    /// <summary>当前准星锁定的目标；未手持魔杖、未选中冰锥或冰凌锁、准星没有在追踪时返回 null。</summary>
    public static NPC LockedTarget => CanTrack && targetIndex >= 0 && IsValidTarget(Main.npc[targetIndex]) ? Main.npc[targetIndex] : null;

    /// <summary>冰锥与冰凌锁共用目标：优先沿用准星锁定，否则按同样规则即时查找一次。</summary>
    public static NPC ResolveTarget() => CanTrack ? LockedTarget ?? FindTarget(Main.MouseWorld, -1) : null;

    public static NPC FindTarget(Vector2 mouseWorld, int currentIndex)
    {
        NPC best = null;
        float bestDistance = float.MaxValue;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (!IsValidTarget(npc))
                continue;

            bool isCurrent = npc.whoAmI == currentIndex;
            float distance = DistanceToHitbox(mouseWorld, npc);
            if (distance > (isCurrent ? LockRange * KeepRangeMultiplier : LockRange))
                continue;

            // 只比较实际距离；距离完全相同时保留当前目标，避免来回切换。
            if (best == null || distance < bestDistance || distance == bestDistance && isCurrent)
            {
                best = npc;
                bestDistance = distance;
            }
        }

        return best;
    }

    public static bool IsValidTarget(NPC npc) => npc.active && npc.CanBeChasedBy();

    private static float DistanceToHitbox(Vector2 point, NPC npc)
    {
        Rectangle hitbox = npc.Hitbox;
        Vector2 closest = Vector2.Clamp(point, hitbox.TopLeft(), hitbox.BottomRight());
        return Vector2.Distance(point, closest);
    }

    private static bool CanTrack => !Main.dedServ && !Main.gameMenu
        && Main.LocalPlayer is { active: true, dead: false, ghost: false }
        && Main.LocalPlayer.HeldItem.ModItem is ElainaWand && IceConeSelected();

    private static bool IceConeSelected()
    {
        List<Skill> skills = ElainaSkillModPlayer.GetActiveSkill;
        int index = ElainaSkillModPlayer.CurrentSkillIndex;
        return index >= 0 && index < skills.Count && skills[index]?.ModSkill is IceConeSkill or ShardbindSkill;
    }

    public override void OnWorldUnload() => ResetTracking();

    private static void ResetTracking()
    {
        targetIndex = -1;
        lockProgress = 0f;
        alpha = 0f;
        age = -1f;
    }

    public override void Unload()
    {
        Texture2D texture = noiseTexture;
        noiseTexture = null;
        if (texture != null)
            Main.QueueMainThreadAction(texture.Dispose);
    }

    public override void PostUpdateEverything()
    {
        if (Main.dedServ)
            return;

        if (!CanTrack)
        {
            ResetTracking();
            return;
        }

        NPC target = FindTarget(Main.MouseWorld, targetIndex);
        int newIndex = target?.whoAmI ?? -1;
        if (newIndex != targetIndex)
        {
            targetIndex = newIndex;
            // 换目标时重新播放锁定动画：准星从鼠标处飞向新目标并收紧
            if (target != null)
            {
                lockProgress = 0f;
                age = -1f;
            }
        }

        if (target != null)
        {
            lockProgress = Math.Min(lockProgress + Dt / LockDuration, 1f);
            alpha = Math.Min(alpha + Dt * 10f, 1f);
            if (lockProgress >= 1f)
                age = age < 0f ? 0f : age + Dt;

            float travel = 1f - MathF.Pow(1f - lockProgress, 4f);
            reticleCenter = Vector2.Lerp(Main.MouseWorld, target.Center, travel);
            reticleRadius = MathHelper.Clamp(Math.Max(target.width, target.height) * 0.6f, 24f, 150f) * 1.2f;
        }
        else
        {
            // 失去目标后在原地松开并淡出；age 继续走，霜纹随整体一起淡出
            lockProgress = Math.Max(lockProgress - Dt * 3.5f, 0f);
            alpha = Math.Max(alpha - Dt * 4.5f, 0f);
            if (age >= 0f)
                age += Dt;
        }
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int index = layers.FindIndex(layer => layer.Name == "Vanilla: Interface Logic 1");
        layers.Insert(Math.Max(index, 0), new LegacyGameInterfaceLayer(
            "伊蕾娜: Ice Cone Reticle",
            () =>
            {
                DrawReticle();
                return true;
            },
            InterfaceScaleType.Game));
    }

    private static void DrawReticle()
    {
        if (alpha <= 0f || !CanTrack)
            return;

        Effect effect = ModContent.Request<Effect>("伊蕾娜/Effects/Content/IceReticle", AssetRequestMode.ImmediateLoad).Value;
        noiseTexture ??= BakeNoiseTexture(Main.instance.GraphicsDevice);
        SetReticleParameters(effect);

        // AlphaBlend 即预乘混合，与 shader 输出一致；Immediate 模式下逐个 pass 画同一个四边形
        EndBeginDraw(0, 1, ss: SamplerState.LinearClamp);
        GraphicsDevice gd = Main.instance.GraphicsDevice;
        gd.Textures[1] = noiseTexture;
        gd.SamplerStates[1] = SamplerState.LinearClamp;

        Texture2D quad = TextureAssets.MagicPixel.Value;
        float size = reticleRadius * ReticleExtent * 2f;
        Vector2 scale = new(size / quad.Width, size / quad.Height);
        foreach (EffectPass pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            DrawInWorld(quad, reticleCenter, Color.White, scale);
        }

        EndBeginDraw();
    }

    // 只依赖动画进度的量在这里算好，shader 里只保留逐像素的部分
    private static void SetReticleParameters(Effect effect)
    {
        float t = Main.GlobalTimeWrappedHourly;
        float lk = MathHelper.Clamp(lockProgress, 0f, 1f);
        float e = EaseOutBack(lk);
        float e3 = 1f - MathF.Pow(1f - lk, 3f);
        float grow = SmoothStep(0.5f, 1f, lk);

        Vector2 spike = new(0.17f * grow + 0.001f, 0.03f);
        Vector2 side = new(0.06f * grow + 0.001f, 0.018f);
        float creep = age < 0f ? 0f : MathHelper.Clamp(age / 0.9f, 0f, 1f);
        float crossAngle = (1f - e3) * MathHelper.PiOver4;

        Vector3 ring = Vector3.Zero;
        Vector2 flash = Vector2.Zero;
        if (age is >= 0f and < 0.55f)
        {
            float k = age / 0.55f;
            float fade = (1f - k) * (1f - k);
            ring = new Vector3(1f + k * 1.1f, 0.012f * (1f - k) + 0.003f, fade * 1.3f);
            flash = new Vector2(0.9f * (0.6f + 0.4f * k), fade * 1.6f);
        }

        effect.Parameters["uHalfPx"].SetValue(reticleRadius * Main.GameViewMatrix.Zoom.X * 0.5f);
        effect.Parameters["uExtent"].SetValue(ReticleExtent);
        effect.Parameters["uTime"].SetValue(t);
        effect.Parameters["uAlpha"].SetValue(alpha);
        effect.Parameters["uDeep"].SetValue(DeepColor);
        effect.Parameters["uMid"].SetValue(MidColor);
        effect.Parameters["uS"].SetValue(MathHelper.Lerp(1.6f, 0.86f, e));
        effect.Parameters["uE3"].SetValue(e3);
        effect.Parameters["uAmbient"].SetValue(0.12f * e3);
        effect.Parameters["uSpike"].SetValue(new Vector4(spike, 1f / spike.LengthSquared(), 0.17f * grow));
        effect.Parameters["uSide"].SetValue(new Vector4(side, 1f / side.LengthSquared(), grow * 0.8f));
        effect.Parameters["uGrow"].SetValue(grow);
        effect.Parameters["uFrost"].SetValue(new Vector2(Math.Max(0.75f * creep, 1e-4f), age >= 0f ? 0.8f : 0f));
        effect.Parameters["uNoiseRange"].SetValue(NoiseRange);
        effect.Parameters["uCross"].SetValue(new Vector2(MathF.Cos(crossAngle), MathF.Sin(crossAngle)));
        effect.Parameters["uG0"].SetValue(MathHelper.Lerp(0.4f, 0.22f, e3));
        effect.Parameters["uPulse"].SetValue(e3 * (0.6f + 0.4f * MathF.Sin(t * 6f)));
        effect.Parameters["uMoteK"].SetValue(SmoothStep(0f, 0.5f, lk) * 0.55f);
        effect.Parameters["uRing"].SetValue(ring);
        effect.Parameters["uFlash"].SetValue(flash);
    }

    private static float EaseOutBack(float x)
    {
        float t = x - 1f;
        return 1f + 2.70158f * t * t * t + 1.70158f * t * t;
    }

    private static float SmoothStep(float edge0, float edge1, float x)
    {
        float t = MathHelper.Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    // 与预览 / 原 GLSL 相同的 fbm，烘焙成纹理供霜纹采样（ps_3_0 指令数放不下实时计算）
    private static Texture2D BakeNoiseTexture(GraphicsDevice gd)
    {
        Color[] data = new Color[NoiseSize * NoiseSize];
        for (int y = 0; y < NoiseSize; y++)
        {
            float qy = ((y + 0.5f) / NoiseSize * 2f - 1f) * NoiseRange;
            for (int x = 0; x < NoiseSize; x++)
            {
                float qx = ((x + 0.5f) / NoiseSize * 2f - 1f) * NoiseRange;
                byte value = (byte)Math.Clamp((int)MathF.Round(Fbm(qx, qy) * 255f), 0, 255);
                data[y * NoiseSize + x] = new Color(value, value, value, 255);
            }
        }

        Texture2D texture = new(gd, NoiseSize, NoiseSize);
        texture.SetData(data);
        return texture;
    }

    private static float Fract(float x) => x - MathF.Floor(x);

    private static float Hash(float x, float y)
    {
        x = Fract(x * 123.34f);
        y = Fract(y * 456.21f);
        float d = x * (x + 45.32f) + y * (y + 45.32f);
        return Fract((x + d) * (y + d));
    }

    private static float Noise(float x, float y)
    {
        float ix = MathF.Floor(x), iy = MathF.Floor(y);
        float fx = x - ix, fy = y - iy;
        fx = fx * fx * (3f - 2f * fx);
        fy = fy * fy * (3f - 2f * fy);
        float bottom = MathHelper.Lerp(Hash(ix, iy), Hash(ix + 1f, iy), fx);
        float top = MathHelper.Lerp(Hash(ix, iy + 1f), Hash(ix + 1f, iy + 1f), fx);
        return MathHelper.Lerp(bottom, top, fy);
    }

    private static float Fbm(float x, float y)
    {
        float sum = 0f, amplitude = 0.5f;
        for (int i = 0; i < 4; i++)
        {
            sum += amplitude * Noise(x, y);
            x = x * 2.03f + 1.7f;
            y = y * 2.03f + 1.7f;
            amplitude *= 0.5f;
        }

        return sum;
    }
}
