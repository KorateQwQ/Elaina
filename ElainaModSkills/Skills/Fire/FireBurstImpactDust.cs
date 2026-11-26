using System;
using KL.Drawing;
using KL.Dusts;

namespace 伊蕾娜.ElainaModSkills.Skills.Fire;

/// <summary>
/// 炎爆领域的落地爆燃：一颗粒子承载一次完整爆燃，生命周期独立于施法动作与领域，
/// 与领域火环同在 OverPlayers.Before 的 Bloom 组内，按 DustOrder 画在火环之上。构成见 FireBurstCastVisuals.DrawImpact。
/// 施法动作在爆燃帧于各端各自生成；快速施放没有施法动作，由领域在出现时生成。
/// </summary>
public sealed class FireBurstImpactDust : KLBasicDust
{
    private const string RequestId = "Elaina:FireBurstDomain:Impact";

    public override string Texture => "伊蕾娜/ElainaModSkills/UICore/Assets/GlowDot";

    private static readonly Random ImpactRandom = new();
    private static FireBurstCastTextures? textures;

    internal static FireBurstCastTextures Textures => textures ??= new FireBurstCastTextures(
        Load("伊蕾娜/ElainaModSkills/UICore/Assets/GlowDot"),
        Load("伊蕾娜/ElainaModSkills/UICore/Assets/Star4"),
        Load("KL/Effects/Tex/Sparkle/T_StartFlare001"),
        Load("KL/Effects/Tex/Sparkle/ShotLineSPA"));

    private static Texture2D Load(string path) => ModContent.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad).Value;

    internal static void SetCastIntensity(float intensity)
    {
        Effect effect = ModContent.Request<Effect>("伊蕾娜/Effects/Content/FireBurstCast", AssetRequestMode.ImmediateLoad).Value;
        FireBurstCastVisuals.SetIntensity(effect, intensity);
    }

    private sealed class ImpactState(Vector2 center, Vector2 ground, FireBurstImpactParts parts)
    {
        public readonly Vector2 Center = center, Ground = ground;
        public readonly FireBurstImpactParts Parts = parts;
        public int Age;
        public Action Draw;
    }

    /// <summary>在落点生成一次爆燃；本地玩家在领域内时轻微震屏。</summary>
    public static void Emit(Vector2 center, Vector2 ground)
    {
        if (Main.dedServ) return;
        // ---- 震屏（热重载可调）----
        float shakeStrength = 7f, shakeFrequency = 12f;
        int shakeTicks = 14;

        var state = new ImpactState(center, ground, FireBurstCastVisuals.CreateImpact(ImpactRandom));
        // 不经过 KL SpawnDust：其立即 Update 会让首帧跳过闪光
        Dust dust = Dust.NewDustDirect(ground, 0, 0, ModContent.DustType<FireBurstImpactDust>(), 0, 0, 0, Color.White, 1f);
        dust.position = ground;
        dust.velocity = Vector2.Zero;
        dust.customData = new DustInfo(FireBurstCastVisuals.ImpactDuration, Vector2.One) { DustData = state };

        Player local = Main.LocalPlayer;
        if (shakeStrength > 0 && local.active && Vector2.DistanceSquared(local.Center, center) <= FireBurstDomain.Radius * FireBurstDomain.Radius)
            ShakeScreen(ground, Vector2.UnitY, shakeStrength, shakeFrequency, shakeTicks);
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
        if (dust.customData is not DustInfo { DustData: ImpactState state } info)
        {
            dust.active = false;
            return false;
        }

        int duration = FireBurstCastVisuals.ImpactDuration;
        state.Age++;
        info.CurrentLifeTime = duration - state.Age;
        info.LifeProgress = MathHelper.Clamp(state.Age / (float)duration, 0f, 1f);
        if (state.Age > duration)
        {
            dust.active = false;
            return false;
        }

        float light = 1.6f;
        Lighting.AddLight(state.Ground + new Vector2(0, -20), new Vector3(1f, 0.5f, 0.15f) * light * FireBurstCastVisuals.ImpactLight(state.Age));
        return false;
    }

    public override void SubmitBloomDrawRequest(Dust dust)
    {
        if (dust.customData is not DustInfo { DustData: ImpactState state } info) return;
        state.Draw ??= () =>
        {
            if (!dust.active || !ReferenceEquals(dust.customData, info)) return;
            // 生成后未经 Update 就绘制时按第 1 帧处理
            int n = Math.Max(state.Age, 1);
            Vector2 ground = state.Ground - Main.screenPosition, center = state.Center - Main.screenPosition;
            EndBeginDraw(2, 1, ss: SamplerState.LinearClamp);
            FireBurstCastVisuals.DrawImpact(Main.spriteBatch, Textures, ground, center, FireBurstDomain.Radius, n, state.Parts,
                new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), SetCastIntensity);
            EndBeginDraw();
        };
        LayerDrawRequestSystem.RequestBloom(RequestId, LayerDrawRequestSystem.DrawTargetLayer.OverPlayers,
            LayerDrawRequestSystem.DrawTiming.Before, state.Draw, order: LayerDrawRequestSystem.DustOrder);
    }

    public override bool PreDraw(Dust dust) => false;

    public override void Unload() => textures = null;
}
