using System;
using KL.Drawing;
using KL.Dusts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace 伊蕾娜.System.Experience;

/// <summary>
/// 升级特效的星屑与线条粒子：寿命独立于 <see cref="LevelUpEffect"/>，弹幕结束后仍按自己的寿命飘散。
/// 画在玩家之上，与升级光环共用 OverPlayers.After 的 Bloom，并在同层先画黑色垫底；外观与 FNA 预览共用 <see cref="LevelUpVisuals"/>。
/// </summary>
public sealed class LevelUpDust : KLBasicDust
{
    private const string RequestId = "Elaina:LevelUp:Particles", ShadowRequestId = "Elaina:LevelUp:ParticleShadows";
    /// <summary>黑色垫底的绘制顺序：同层先于升级光效与其他弹幕本体，压暗亮背景。</summary>
    internal const int ShadowOrder = LayerDrawRequestSystem.ProjectileOrder - 10;
    // 同玩偶星屑：预乘 AlphaBlend 写入覆盖度，ReColor 抬到 HDR，Bloom 只提取溢出部分，亮背景上不爆白。
    internal const float Hdr = 2.2f;
    internal const string UiAssets = "伊蕾娜/ElainaModSkills/UICore/Assets/";
    private static readonly Vector3 MoteLight = new(.26f, .22f, .34f);

    public override string Texture => UiAssets + "GlowDot";

    private sealed class ParticleState(LevelUpParticle particle, Vector2 origin)
    {
        public LevelUpParticle Particle = particle;
        public readonly Vector2 Origin = origin;
        public Action<LayerDrawRequestSystem.DrawRequestContext> Draw, Shadow;
    }

    /// <summary>放出一颗粒子；origin 为放出时的脚底点（世界坐标）。</summary>
    public static void Spawn(Vector2 origin, LevelUpParticle particle)
    {
        if (Main.dedServ) return;
        Dust dust = Dust.NewDustDirect(origin, 0, 0, ModContent.DustType<LevelUpDust>(), 0, 0, 0, Color.White, 1f);
        dust.position = origin + particle.Offset;
        dust.velocity = Vector2.Zero;
        dust.customData = new DustInfo(particle.Life, Vector2.One) { DustData = new ParticleState(particle, origin) };
    }

    public override void OnSpawn(Dust dust)
    {
        dust.noGravity = true;
        dust.noLight = true;
        // 本类通过 SubmitBloomDrawRequest 提交自定义绘制。
        ShouldBloom = false;
        base.OnSpawn(dust);
    }

    public override bool Update(Dust dust)
    {
        if (dust.customData is not DustInfo { DustData: ParticleState state } info)
        {
            dust.active = false;
            return false;
        }
        bool alive = LevelUpVisuals.Step(ref state.Particle);
        dust.position = state.Origin + state.Particle.Offset;
        info.CurrentLifeTime = state.Particle.Life - state.Particle.Age;
        info.LifeProgress = MathHelper.Clamp(state.Particle.Age / state.Particle.Life, 0f, 1f);
        if (state.Particle.Kind == LevelUpParticleKind.Mote)
            Lighting.AddLight(dust.position, MoteLight * LevelUpVisuals.Brightness(state.Particle) * state.Particle.Size);
        if (!alive) dust.active = false;
        // 位置完全由粒子状态决定，不调用 base.Update。
        return false;
    }

    public override void SubmitBloomDrawRequest(Dust dust)
    {
        if (dust.customData is not DustInfo { DustData: ParticleState state } info) return;
        state.Draw ??= context => DrawParticle(context, dust, info, state, false);
        state.Shadow ??= context => DrawParticle(context, dust, info, state, true);
        LayerDrawRequestSystem.RequestBloom(ShadowRequestId, LayerDrawRequestSystem.DrawTargetLayer.OverPlayers,
            LayerDrawRequestSystem.DrawTiming.After, state.Shadow, order: ShadowOrder);
        LayerDrawRequestSystem.RequestBloom(RequestId, LayerDrawRequestSystem.DrawTargetLayer.OverPlayers,
            LayerDrawRequestSystem.DrawTiming.After, state.Draw, order: LayerDrawRequestSystem.DustOrder);
    }

    private void DrawParticle(LayerDrawRequestSystem.DrawRequestContext context, Dust dust, DustInfo info,
        ParticleState state, bool shadow)
    {
        // 同组粒子只切换一次状态。
        if (context.IsFirst)
        {
            EndBeginDraw(0, 1, ss: SamplerState.LinearClamp);
            if (!shadow) ReColorEffect(new Vector4(Hdr, Hdr, Hdr, 1));
        }
        if (dust.active && ReferenceEquals(dust.customData, info))
            LevelUpVisuals.Draw(Main.spriteBatch, Textures(), state.Origin - Main.screenPosition, state.Particle, shadow);
        if (context.IsLast) EndBeginDraw();
    }

    internal static LevelUpTextures Textures() => new(Load(UiAssets + "GlowDot"), Load(UiAssets + "Star4"),
        Load(UiAssets + "StarFlare"), Load("KL/Effects/Tex/Sparkle/ShotLine"),
        Load(UiAssets + "ThinRing"), Load("伊蕾娜/ElainaModAlchemy/Revealing/Assets/MarkGlow"));

    private static Texture2D Load(string path) => ModContent.Request<Texture2D>(path).Value;

    public override bool PreDraw(Dust dust) => false;
}
