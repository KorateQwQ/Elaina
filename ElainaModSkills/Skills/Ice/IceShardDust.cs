using System;
using KL.Drawing;
using KL.Dusts;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

public sealed class IceShardDust : KLBasicDust
{
    public override string Texture => IceShardVisuals.AtlasPath;
    private static readonly Random VisualRandom = new();

    private sealed class ShardState(IceShardPart[] parts)
    {
        internal readonly IceShardPart[] Parts = parts;
        internal int Age;
        internal Action<LayerDrawRequestSystem.DrawRequestContext> Draw;
    }

    public static void Emit(Vector2 position, float direction)
    {
        if (Main.dedServ) return;
        Emit(position, direction, VisualRandom.Next());
    }

    internal static void Emit(Vector2 position, float direction, int seed)
    {
        if (Main.dedServ) return;
        EmitParts(position, IceShardVisuals.Create(seed, direction));
    }

    public static void EmitShardbindBurst(Vector2 position)
    {
        if (Main.dedServ) return;
        EmitParts(position, ShardbindBurstProfile.Create(VisualRandom.Next(), 0f));
    }

    private static void EmitParts(Vector2 position, IceShardPart[] parts)
    {
        var state = new ShardState(parts);
        Dust dust = Dust.NewDustDirect(position, 0, 0, ModContent.DustType<IceShardDust>(), 0, 0, 0, Color.White, 1);
        dust.position = position;
        dust.velocity = Vector2.Zero;
        dust.customData = new DustInfo(IceShardVisuals.Duration, Vector2.One) { DustData = state };
    }

    public override void OnSpawn(Dust dust)
    {
        dust.noGravity = dust.noLight = true;
        ShouldBloom = false;
        base.OnSpawn(dust);
    }

    public override bool Update(Dust dust)
    {
        var info = (DustInfo)dust.customData;
        var state = (ShardState)info.DustData;
        state.Age++;
        info.CurrentLifeTime = IceShardVisuals.Duration - state.Age;
        info.LifeProgress = MathHelper.Clamp(state.Age / (float)IceShardVisuals.Duration, 0, 1);
        if (state.Age >= IceShardVisuals.Duration) dust.active = false;
        return false;
    }

    public override void SubmitBloomDrawRequest(Dust dust)
    {
        var info = (DustInfo)dust.customData;
        var state = (ShardState)info.DustData;
        state.Draw ??= context =>
        {
            // 同组所有命中合用一次延迟批次；失效回调也须维护首尾边界。
            if (context.IsFirst) EndBeginDraw(ss: SamplerState.LinearClamp);
            if (dust.active && dust.type == Type && ReferenceEquals(dust.customData, info))
                IceShardVisuals.Draw(Main.spriteBatch, MainTexture, dust.position - Main.screenPosition, state.Parts, state.Age);
            if (context.IsLast) EndBeginDraw();
        };
        // 普通绘制：冰锥 Bloom 之后、原有爆点/碎光/冷雾之前，与确认的组合预览顺序一致。
        LayerDrawRequestSystem.Request("Elaina:IceCone:Shards", LayerDrawRequestSystem.DrawTargetLayer.Dust,
            LayerDrawRequestSystem.DrawTiming.Before, state.Draw, order: LayerDrawRequestSystem.DustOrder);
    }

    public override bool PreDraw(Dust dust) => false;

    public override void Unload() => MainTexture = null;
}
