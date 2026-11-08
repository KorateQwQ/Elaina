using System;
using KL.Drawing;
using KL.Dusts;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

/// <summary>发射时留在原处扩散的气流，通过普通请求将两个面夹在冰锥前后。</summary>
public sealed class IceConeWindDust : KLBasicDust
{
    public override string Texture => IceConeWindDrawing.TexturePath;

    private sealed class ParticleState(float phase)
    {
        public readonly float Phase = phase;
        public Action<LayerDrawRequestSystem.DrawRequestContext> Rear;
        public Action<LayerDrawRequestSystem.DrawRequestContext> Front;
    }

    public static void Emit(Vector2 position, Vector2 direction)
    {
        if (Main.dedServ) return;
        Dust dust = Dust.NewDustDirect(position, 0, 0, ModContent.DustType<IceConeWindDust>());
        dust.position = position;
        dust.velocity = direction * .7f;
        dust.rotation = MathF.Atan2(direction.Y, direction.X);
        dust.customData = new DustInfo(IceConeWindDrawing.Lifetime, Vector2.One)
        {
            DustData = new ParticleState(Main.rand.NextFloat(MathHelper.TwoPi))
        };
    }

    public override void OnSpawn(Dust dust)
    {
        dust.noGravity = true;
        dust.noLight = true;
        ShouldBloom = false;
        base.OnSpawn(dust);
    }

    // KL 在 Projectiles.After 之前收集所有 KLBasicDust，可在这个入口登记普通请求。
    public override void SubmitBloomDrawRequest(Dust dust)
    {
        var info = (DustInfo)dust.customData;
        var state = (ParticleState)info.DustData;
        state.Rear ??= context => DrawHalf(context, dust, info, state, front: false);
        state.Front ??= context => DrawHalf(context, dust, info, state, front: true);
        LayerDrawRequestSystem.Request("Elaina:IceCone:WindRear",
            LayerDrawRequestSystem.DrawTargetLayer.Projectiles, LayerDrawRequestSystem.DrawTiming.After,
            state.Rear, order: LayerDrawRequestSystem.TrailOrder);
        LayerDrawRequestSystem.Request("Elaina:IceCone:WindFront",
            LayerDrawRequestSystem.DrawTargetLayer.Dust, LayerDrawRequestSystem.DrawTiming.After,
            state.Front, order: LayerDrawRequestSystem.DustOrder);
    }

    private void DrawHalf(LayerDrawRequestSystem.DrawRequestContext context, Dust dust,
        DustInfo info, ParticleState state, bool front)
    {
        if (context.IsFirst) EndBeginDraw();
        if (dust.active && dust.type == Type && ReferenceEquals(dust.customData, info))
        {
            Effect effect = ModContent.Request<Effect>(IceConeWindDrawing.EffectPath, AssetRequestMode.ImmediateLoad).Value;
            Matrix transform = Main.GameViewMatrix.TransformationMatrix *
                Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1);
            IceConeWindDrawing.Draw(Main.instance.GraphicsDevice, effect, MainTexture, transform,
                dust.position - Main.screenPosition, info.LifeTime - info.CurrentLifeTime,
                front, dust.rotation, state.Phase);
        }
        if (context.IsLast) EndBeginDraw();
    }

    public override bool PreDraw(Dust dust) => false;
}
