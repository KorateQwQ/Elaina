using System;
using KL.Drawing;
using KL.Dusts;
using KL.Utils;
using static 伊蕾娜.ElainaModSkills.Skills.Ice.ShardbindVisualSettings;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

public sealed class ShardbindFlashDust : KLBasicDust
{
    public override string Texture => "KL/Effects/Tex/Sparkle/T_StartFlare001";
    private sealed class FlashState(bool burst, int index, float angle, float gain, int life)
    {
        internal readonly bool Burst = burst;
        internal readonly int Index = index, Life = life;
        internal readonly float Angle = angle, Gain = gain;
        internal int Age;
    }

    public static void Emit(Vector2 center, bool burst, int index = 0)
    {
        if (Main.dedServ) return;
        var star = burst ? default : GetStar(index);
        int life = burst ? BurstFlashLife : StarLife;
        Dust dust = Dust.NewDustDirect(center, 0, 0, ModContent.DustType<ShardbindFlashDust>());
        dust.position = center;
        dust.velocity = Vector2.Zero;
        dust.customData = new DustInfo(life, Vector2.One)
        {
            DustData = new FlashState(burst, index, MathHelper.ToRadians(burst ? BurstFlashAngle : star.Angle), burst ? 1f : star.Gain, life)
        };
    }

    public override void OnSpawn(Dust dust)
    {
        dust.noGravity = dust.noLight = true;
        base.OnSpawn(dust);
    }

    public override bool Update(Dust dust)
    {
        var info = (DustInfo)dust.customData;
        var state = (FlashState)info.DustData;
        state.Age++;
        info.CurrentLifeTime = state.Life - state.Age;
        info.LifeProgress = state.Age / (float)state.Life;
        if (state.Age >= state.Life) dust.active = false;
        return false;
    }

    public override bool PreDraw(Dust dust) => false;
    public override void SubmitBloomDrawRequest(Dust dust)
    {
        var info = (DustInfo)dust.customData;
        var state = (FlashState)info.DustData;
        LayerDrawRequestSystem.RequestBloom("Elaina:Shardbind:Flash", LayerDrawRequestSystem.DrawTargetLayer.OverPlayers,
            LayerDrawRequestSystem.DrawTiming.Before, context =>
        {
            if (context.IsFirst) EndBeginDraw(0, 1, ss: SamplerState.LinearClamp);
            if (dust.active && dust.type == Type && ReferenceEquals(dust.customData, info)) DrawFlash(dust.position, state);
            if (context.IsLast) EndBeginDraw();
        }, order: LayerDrawRequestSystem.DustOrder);
    }

    private static void DrawFlash(Vector2 center, FlashState state)
    {
        float t = state.Age / (float)state.Life;
        float decay = state.Burst ? MathF.Exp(-state.Age / BurstFlashDecayTicks) * (1f - t) : MathF.Pow(1f - t, StarDecayPower);
        float index = state.Index / (float)Math.Max(1, StarCount - 1);
        float size = state.Burst ? BurstFlashSize : MathHelper.Lerp(StarSizeRange.X, StarSizeRange.Y, index);
        float hdr = (state.Burst ? BurstFlashHdr : MathHelper.Lerp(StarHdrRange.X, StarHdrRange.Y, index)) * decay * state.Gain;
        Vector3 color = state.Burst ? BurstFlashColor : StarColor;
        Vector3 glowColor = state.Burst ? BurstGlowColor : StarGlowColor;
        float glowHdr = state.Burst ? BurstGlowHdr : StarGlowHdr;
        float glowSize = state.Burst ? BurstGlowSize : StarGlowSize;
        ReColorEffect(new Vector4(glowColor * glowHdr * decay * state.Gain, 1f));
        DrawInWorld(AssetManager.GetTexture("KL.Effects.Tex.background_Premultiplied"), center,
            new Color(255, 255, 255, 0), new Vector2(glowSize), 0);
        ReColorEffect(new Vector4(color * hdr, 1f));
        DrawInWorld(AssetManager.GetTexture("KL.Effects.Tex.Sparkle.T_StartFlare001"), center,
            new Color(255, 255, 255, 0), new Vector2(size * (1f - t * (state.Burst ? BurstFlashShrink : StarShrink))), state.Angle);
    }

    public override void Unload() => MainTexture = null;
}
