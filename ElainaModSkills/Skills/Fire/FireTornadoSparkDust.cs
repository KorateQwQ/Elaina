using System;
using KL.Drawing;
using KL.Dusts;

namespace 伊蕾娜.ElainaModSkills.Skills.Fire;

/// <summary>
/// 火焰龙卷的火星粒子：附着在龙卷上随其移动，生命周期独立于弹幕，龙卷消失后留在原地按自己的寿命飞散。
/// 与龙卷共用 Projectiles.After 的 Bloom：背面火星排在龙卷本体之前被遮挡，正面火星排在之后。
/// </summary>
public sealed class FireTornadoSparkDust : KLBasicDust
{
    private const string RequestId = "Elaina:FireTornado:Sparks";
    // 低于龙卷本体请求的 order（默认 0）
    private const int BackOrder = -100;
    // 火星的 HDR 倍率：小光源经 Bloom 模糊后能量分散，需要较高倍率才有明显光晕；
    // 核心截断为偏黄的清晰圆点，溢出部分形成橙色光晕
    private float Hdr => 12f;

    private static readonly Random SparkRandom = new();

    private sealed class ParticleState(FireTornadoSpark spark, Projectile tornado, float scale)
    {
        public FireTornadoSpark Spark = spark;
        // 龙卷中心（世界坐标），附着期间随龙卷位移
        public Vector2 Anchor = tornado.Center;
        public Vector2 LastAttachmentPosition = tornado.position;
        // Main.projectile 的槽位会被新弹幕复用，用 ModProjectile 实例确认仍是同一个龙卷
        public readonly ModProjectile Owner = tornado.ModProjectile;
        public readonly float Scale = scale;
        public Action<LayerDrawRequestSystem.DrawRequestContext> Draw;
    }

    /// <summary>在龙卷中心生成一颗附着在龙卷上的火星。</summary>
    public static void Emit(Projectile tornado, float scale)
    {
        if (Main.dedServ) return;
        FireTornadoSpark spark = FireTornadoSparks.Create(SparkRandom);
        Vector2 position = tornado.Center + FireTornadoSparks.Offset(spark) * scale;
        Dust dust = Dust.NewDustDirect(position, 0, 0, ModContent.DustType<FireTornadoSparkDust>(), 0, 0, 0, Color.White, 1f);
        dust.position = position;
        dust.velocity = Vector2.Zero;
        dust.customData = new DustInfo(spark.Life, Vector2.One, tornado)
        {
            DustData = new ParticleState(spark, tornado, scale)
        };
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
        if (dust.customData is not DustInfo { DustData: ParticleState state } info)
        {
            dust.active = false;
            return false;
        }

        if (info.AttachedEntity is Projectile attached)
        {
            if (attached.active && ReferenceEquals(attached.ModProjectile, state.Owner))
            {
                // 消费自上次粒子更新以来龙卷的真实位移，不依赖 oldPosition 的更新时机
                state.Anchor += attached.position - state.LastAttachmentPosition;
                state.LastAttachmentPosition = attached.position;
            }
            else info.AttachedEntity = null;
        }

        bool alive = FireTornadoSparks.Step(ref state.Spark);
        dust.position = state.Anchor + FireTornadoSparks.Offset(state.Spark) * state.Scale;
        info.CurrentLifeTime = state.Spark.Life - state.Spark.Age;
        info.LifeProgress = MathHelper.Clamp(state.Spark.Age / state.Spark.Life, 0f, 1f);
        if (!alive) dust.active = false;
        // 位置完全由火星状态与附着决定，不调用 base.Update（避免重复叠加附着位移）
        return false;
    }

    public override void SubmitBloomDrawRequest(Dust dust)
    {
        if (dust.customData is not DustInfo { DustData: ParticleState state } info) return;
        state.Draw ??= context =>
        {
            // 同组火星只切换一次状态
            if (context.IsFirst)
            {
                EndBeginDraw(1, 1, ss: SamplerState.LinearClamp);
                ReColorEffect(new Vector4(Hdr, Hdr, Hdr, 1));
            }
            if (dust.active && ReferenceEquals(dust.customData, info))
                FireTornadoSparks.Draw(Main.spriteBatch, MainTexture, state.Anchor - Main.screenPosition, state.Spark, state.Scale);
            if (context.IsLast) EndBeginDraw();
        };
        bool front = FireTornadoSparks.IsFront(state.Spark);
        LayerDrawRequestSystem.RequestBloom(RequestId, LayerDrawRequestSystem.DrawTargetLayer.Projectiles,
            LayerDrawRequestSystem.DrawTiming.After, state.Draw, order: front ? LayerDrawRequestSystem.DustOrder : BackOrder);
    }

    public override bool PreDraw(Dust dust) => false;
}
