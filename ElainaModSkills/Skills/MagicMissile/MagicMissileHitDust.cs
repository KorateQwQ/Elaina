using System;
using KL.Drawing;
using KL.Dusts;
using ReLogic.Content;

namespace 伊蕾娜.ElainaModSkills.Skills.MagicMissile;

/// <summary>
/// 魔法飞弹的命中特效：一颗粒子承载一次完整命中，生命周期独立于飞弹，
/// 与飞弹共用 Projectiles.After 的 Bloom，按 DustOrder 绘制在飞弹本体之上。构成与绘制见 MagicMissileHitVisuals。
/// </summary>
public sealed class MagicMissileHitDust : KLBasicDust
{
    private const string RequestId = "Elaina:MagicMissile:Hit";

    public override string Texture => "伊蕾娜/ElainaModSkills/UICore/Assets/GlowDot";

    private static readonly Random HitRandom = new();
    private static MagicMissileHitTextures? textures;

    private sealed class HitState(MagicMissileHitPart[] parts, float light)
    {
        public readonly MagicMissileHitPart[] Parts = parts;
        public readonly int Duration = MagicMissileHitVisuals.Duration(parts);
        public readonly float Light = light;
        public int Age;
        public Action<LayerDrawRequestSystem.DrawRequestContext> Draw;
    }

    /// <summary>在命中点生成一次命中特效；burst 为第五击花印爆发。</summary>
    public static void Emit(Vector2 position, float angle, bool burst = false, bool contactOnly = false)
    {
        if (Main.dedServ) return;
        MagicMissileHitPart[] parts = contactOnly ? MagicMissileHitVisuals.CreateContact(angle) : burst
            ? MagicMissileHitVisuals.CreateBurst(HitRandom, angle)
            : MagicMissileHitVisuals.CreateHit(HitRandom, angle);
        var state = new HitState(parts, burst ? 1.6f : 0.9f);
        // 不经过 KL SpawnDust：其立即 Update 会让首帧跳过闪光
        Dust dust = Dust.NewDustDirect(position, 0, 0, ModContent.DustType<MagicMissileHitDust>(), 0, 0, 0, Color.White, 1f);
        dust.position = position;
        dust.velocity = Vector2.Zero;
        dust.customData = new DustInfo(state.Duration, Vector2.One) { DustData = state };
    }

    public override void OnSpawn(Dust dust)
    {
        dust.noGravity = true;
        dust.noLight = true;
        // 本类通过 SubmitBloomDrawRequest 提交自定义绘制
        ShouldBloom = false;
        base.OnSpawn(dust);
    }

    public override bool Update(Dust dust)
    {
        if (dust.customData is not DustInfo { DustData: HitState state } info)
        {
            dust.active = false;
            return false;
        }

        state.Age++;
        info.CurrentLifeTime = state.Duration - state.Age;
        info.LifeProgress = MathHelper.Clamp(state.Age / (float)state.Duration, 0f, 1f);
        if (state.Age > state.Duration)
        {
            dust.active = false;
            return false;
        }

        float light = state.Light * MagicMissileHitVisuals.LightStrength(state.Age, state.Duration);
        Lighting.AddLight(dust.position, 伊蕾娜.skillColor[伊蕾娜.SkillType.MagicMissile].ToVector3() * light);
        return false;
    }

    public override void SubmitBloomDrawRequest(Dust dust)
    {
        if (dust.customData is not DustInfo { DustData: HitState state } info) return;
        state.Draw ??= context =>
        {
            if (!dust.active || !ReferenceEquals(dust.customData, info)) return;
            MagicMissileHitTextures tex = textures ??= LoadTextures();
            // AlphaBlend + Immediate：顶点 alpha 为覆盖度，每次绘制前单独设置 ReColor 倍率
            EndBeginDraw(0, 1, ss: SamplerState.LinearClamp);
            // 生成后未经 Update 就绘制时按第 1 帧处理
            MagicMissileHitVisuals.Draw(Main.spriteBatch, tex, dust.position - Main.screenPosition, state.Parts, Math.Max(state.Age, 1),
                hdr => ReColorEffect(new Vector4(hdr, hdr, hdr, 1)));
            EndBeginDraw();
        };
        LayerDrawRequestSystem.RequestBloom(RequestId, LayerDrawRequestSystem.DrawTargetLayer.Projectiles,
            LayerDrawRequestSystem.DrawTiming.After, state.Draw, order: LayerDrawRequestSystem.DustOrder);
    }

    public override bool PreDraw(Dust dust) => false;

    private static MagicMissileHitTextures LoadTextures()
    {
        static Texture2D Texture(string path) => ModContent.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad).Value;
        var petals = new Texture2D[5];
        for (int i = 0; i < petals.Length; i++)
            petals[i] = Texture($"伊蕾娜/ElainaModSkills/Skills/MagicMissile/Flo_{i + 1}");
        return new MagicMissileHitTextures(
            Texture("KL/Effects/Tex/Sparkle/T_StartFlare001"),
            Texture("伊蕾娜/ElainaModSkills/UICore/Assets/GlowDot"),
            Texture("KL/Dusts/Burst/BurstCircle"),
            Texture("KL/Dusts/Burst/BurstDust"),
            Texture("KL/Effects/Tex/Sparkle/ShotLineSPA"),
            petals);
    }

    public override void Unload() => textures = null;
}
