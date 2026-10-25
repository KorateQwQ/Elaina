using System;
using KL.Drawing;
using KL.Dusts;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

/// <summary>
/// 玩偶召唤/收回的星芒与星屑：生命周期独立于玩偶弹幕，玩偶移除后仍按自己的寿命飘散。
/// 与其他 KL 粒子共用 Projectiles.After 的 Bloom；外观与 FNA 预览共用 <see cref="DollSummonVisuals"/>。
/// </summary>
public sealed class DollSummonDust : KLBasicDust
{
    private const string RequestId = "Elaina:DollSummon:Sparks";
    // 同魔力飞弹本体：带覆盖度绘制并压暗背景，ReColor 把饱和粉紫抬到 HDR，Bloom 只提取溢出部分，
    // 白天与夜晚色相一致（Additive 叠在亮背景上会爆白）。贴图为预乘格式，故用预乘 AlphaBlend 而非 NonPremultiplied，
    // 否则柔边 alpha 被平方，亮背景上会出现灰雾。
    private const float Hdr = 2.2f;
    private const string UiAssets = "伊蕾娜/ElainaModSkills/UICore/Assets/";
    private static readonly Random SparkRandom = new();
    private static readonly Vector3 FlareLight = new(.75f, .42f, .85f);

    public override string Texture => UiAssets + "GlowDot";

    private sealed class ParticleState(DollSpark spark, Vector2 center)
    {
        public DollSpark Spark = spark;
        public readonly Vector2 Center = center;
        public Action<LayerDrawRequestSystem.DrawRequestContext> Draw;
    }

    /// <summary>在 center 爆出一组星屑；vanish 为收回/消散，否则为召唤/出现。</summary>
    public static void Burst(Vector2 center, bool vanish)
    {
        if (Main.dedServ) return;
        int type = ModContent.DustType<DollSummonDust>();
        for (int i = 0; i < DollSummonVisuals.BurstCount(vanish); i++)
        {
            DollSpark spark = DollSummonVisuals.Create(SparkRandom, i, vanish);
            Dust dust = Dust.NewDustDirect(center, 0, 0, type, 0, 0, 0, Color.White, 1f);
            dust.position = center + spark.Offset;
            dust.velocity = Vector2.Zero;
            dust.customData = new DustInfo(spark.Life, Vector2.One) { DustData = new ParticleState(spark, center) };
        }
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
        bool alive = DollSummonVisuals.Step(ref state.Spark);
        dust.position = state.Center + state.Spark.Offset;
        info.CurrentLifeTime = state.Spark.Life - state.Spark.Age;
        info.LifeProgress = MathHelper.Clamp(state.Spark.Age / state.Spark.Life, 0f, 1f);
        if (state.Spark.Kind == DollSparkKind.Flare)
            Lighting.AddLight(dust.position, FlareLight * DollSummonVisuals.FlareStrength(state.Spark));
        if (!alive) dust.active = false;
        // 位置完全由星屑状态决定，不调用 base.Update。
        return false;
    }

    public override void SubmitBloomDrawRequest(Dust dust)
    {
        if (dust.customData is not DustInfo { DustData: ParticleState state } info) return;
        state.Draw ??= context =>
        {
            // 同组星屑只切换一次状态。
            if (context.IsFirst)
            {
                EndBeginDraw(0, 1, ss: SamplerState.LinearClamp);
                ReColorEffect(new Vector4(Hdr, Hdr, Hdr, 1));
            }
            if (dust.active && ReferenceEquals(dust.customData, info))
                DollSummonVisuals.Draw(Main.spriteBatch, Load(UiAssets + "GlowDot"), Load(UiAssets + "Star4"),
                    Load("KL/Effects/Tex/Sparkle/T_StartFlare001"), Load("KL/Effects/Tex/Sparkle/ShotLine"),
                    state.Center - Main.screenPosition, state.Spark);
            if (context.IsLast) EndBeginDraw();
        };
        LayerDrawRequestSystem.RequestBloom(RequestId, LayerDrawRequestSystem.DrawTargetLayer.Projectiles,
            LayerDrawRequestSystem.DrawTiming.After, state.Draw, order: LayerDrawRequestSystem.DustOrder);
    }

    private static Texture2D Load(string path) => ModContent.Request<Texture2D>(path).Value;

    public override bool PreDraw(Dust dust) => false;
}
