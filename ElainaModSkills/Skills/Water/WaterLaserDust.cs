using System;
using KL.Drawing;
using KL.Dusts;

namespace 伊蕾娜.ElainaModSkills.Skills.Water;

/// <summary>水射线专用 KL 粒子：与射线共用 Projectiles.After Bloom，按 order 绘制在主体上方。</summary>
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
        // 本类通过 SubmitBloomDrawRequest 提交自定义水花绘制。
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
            // 同组所有水花只 Begin/End 一次；与射线同层，仅做一次后处理。
            if (context.IsFirst) WaterLaserVisuals.BeginSprites();
            if (dust.active && ReferenceEquals(dust.customData, info))
                WaterLaserVisuals.DrawParticle(dust.position, dust.rotation, info.scale,
                    info.LifeProgress, info.randFrame, state.Foam, dust.color);
            if (context.IsLast) EndBeginDraw();
        };
        LayerDrawRequestSystem.RequestBloom("Elaina:WaterLaser:Particles", WaterLaserVisuals.Layer,
            LayerDrawRequestSystem.DrawTiming.After, state.Draw, order: LayerDrawRequestSystem.DustOrder);
    }

    public override bool PreDraw(Dust dust) => false;
}
