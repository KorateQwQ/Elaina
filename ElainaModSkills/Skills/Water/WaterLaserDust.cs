using System;
using KL.Drawing;
using KL.Dusts;

namespace 伊蕾娜.ElainaModSkills.Skills.Water;

/// <summary>水流术与水球共用的水花粒子，在 Dust.After 普通绘制，覆盖玩家与水球。</summary>
public sealed class WaterLaserDust : KLBasicDust
{
    public override string Texture => WaterLaserVisuals.SplashTexturePath;

    private sealed class ParticleState(bool foam, Vector2 attachmentPosition = default)
    {
        public readonly bool Foam = foam;
        public Vector2 LastAttachmentPosition = attachmentPosition;
        public Action<LayerDrawRequestSystem.DrawRequestContext> Draw;
    }

    public static void Emit(Vector2 position, Vector2 velocity, Vector2 size, int lifetime,
        bool foam = false, Entity attachedEntity = null)
    {
        if (Main.dedServ) return;
        // 当前 position 已包含 owner 本帧移动；KL SpawnDust 的立即 Update 会重复加位移。
        // 先完整初始化附着快照，交给正常 Dust 更新推进生命和速度。
        Dust dust = Dust.NewDustDirect(position, 0, 0, ModContent.DustType<WaterLaserDust>(),
            velocity.X, velocity.Y, 0, new Color(118, 218, 255), 1f);
        dust.position = position;
        dust.velocity = velocity;
        dust.rotation = MathF.Atan2(velocity.Y, velocity.X);
        dust.customData = new DustInfo(Math.Max(1, lifetime), size, attachedEntity)
        {
            randFrame = Main.rand.Next(2),
            DustData = new ParticleState(foam, attachedEntity?.position ?? Vector2.Zero)
        };
    }

    public override void OnSpawn(Dust dust)
    {
        dust.noGravity = true;
        dust.noLight = true;
        // 复用 KL 的粒子收集入口，提交普通绘制请求，不参与 Bloom。
        ShouldBloom = false;
        base.OnSpawn(dust);
    }

    public override bool Update(Dust dust)
    {
        if (dust.customData is not DustInfo info) { dust.active = false; return false; }
        if (info.DustData is not ParticleState state)
            info.DustData = state = new ParticleState(false, info.AttachedEntity?.position ?? Vector2.Zero);
        if (info.randFrame < 0) info.randFrame = Main.rand.Next(2);
        if (info.AttachedEntity is { } attached)
        {
            if (attached.active)
            {
                // 消费自上次粒子更新以来的真实位移，不依赖 entity.oldPosition 的更新时机。
                dust.position += attached.position - state.LastAttachmentPosition;
                state.LastAttachmentPosition = attached.position;
            }
            else info.AttachedEntity = null;
        }
        dust.velocity *= 0.93f;
        dust.position += dust.velocity;
        dust.rotation = MathF.Atan2(dust.velocity.Y, dust.velocity.X);
        info.CurrentLifeTime--;
        info.LifeProgress = MathHelper.Clamp(1f - info.CurrentLifeTime / Math.Max(1f, info.LifeTime), 0f, 1f);
        if (info.CurrentLifeTime <= 0) dust.active = false;
        // 生命周期仍保存在 KL DustInfo，位置由本类处理，不能再叠加 base.Update 的附着位移。
        return false;
    }

    public override void SubmitBloomDrawRequest(Dust dust)
    {
        if (dust.customData is not DustInfo info) return;
        if (info.DustData is not ParticleState state)
            info.DustData = state = new ParticleState(false, info.AttachedEntity?.position ?? Vector2.Zero);
        state.Draw ??= context =>
        {
            // DrawDust 返回时已 End；本组自管 Begin/End，离开时仍保持批次关闭。
            if (context.IsFirst) BeginDraw(state: 2, defferred: 1, ss: SamplerState.LinearClamp);
            if (dust.active && ReferenceEquals(dust.customData, info))
                WaterLaserVisuals.DrawParticle(dust.position, dust.rotation, info.scale,
                    info.LifeProgress, info.randFrame, state.Foam, dust.color);
            if (context.IsLast) Main.spriteBatch.End();
        };
        LayerDrawRequestSystem.Request("Elaina:WaterLaser:Particles", LayerDrawRequestSystem.DrawTargetLayer.Dust,
            LayerDrawRequestSystem.DrawTiming.After, state.Draw, order: LayerDrawRequestSystem.DustOrder);
    }

    public override bool PreDraw(Dust dust) => false;
}
