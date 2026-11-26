using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaAttribute;

internal sealed class ChargePointHud(Texture2D star, Texture2D glow)
{
    // 辉光：StarGlow.png 由 Tools/FixedTextureBake 预烘焙（高斯模糊星形），星形半径为纹理边长的 3/16。
    // 预乘贴图以顶点 alpha 0 叠加绘制，只加亮、不压暗背景。所有星画完后统一叠在最上层，
    // 呼吸才会作用到星芯本身，而不是只在被星芯盖住的底部变化。
    private static Vector2 GlowSize => new(6.4f, 7.6f);
    private static Color GlowInk => new(176, 150, 255);
    private static float GlowBase => .16f;
    private static float GlowBreath => .3f;
    private static float BreathSpeed => 2.1f;
    private static float BreathStep => .55f;
    private static float GlowFull => .12f;
    private static float GainGlow => 1.1f;
    private static float SpendGlow => .6f;
    // 恢复进度：下一颗空星里逐渐凝聚的微光，攒满一点时接上获得动画。
    private static float EmberCore => 1.25f;
    private static float EmberAlpha => .9f;
    private static float EmberGlow => .45f;
    private static float EmberGlowSize => .75f;
    private static float EmberFadeIn => .2f;
    private static Vector2 Offset => new(12, 25);
    private static float Pitch => 16;
    private static float Stagger => .045f;
    private static float FlashTime => .105f;
    private static float CollapseTime => .235f;
    private static float JoinTime => .32f;
    private static float GrowDelay => .12f;
    private static float GrowTime => .30f;
    private static float SettleTime => .16f;
    private static float Overshoot => .16f;
    private static float FlashGrowth => .5f;
    private static Vector2 Socket => new(5.7f, 7);
    private static Vector2 SocketInner => new(3.8f, 4.9f);
    private static Vector2 Core => new(4.6f, 5.9f);
    private static Vector2 Inner => new(2.7f, 3.8f);
    private static Vector2 Highlight => new(2.45f, 3.6f);
    private static Vector2 Flash => new(6.8f, 8);
    private static Color SocketInk => new(82, 77, 112);
    private static Color HollowInk => new(107, 100, 136);
    private static Color CoreInk => new(165, 148, 214);
    private static Color InnerInk => new(202, 180, 239);
    private static Color LightInk => new(251, 245, 255);
    private static Color BurstInk => new(255, 251, 255);
    private static Vector2 JoinOffset => new(9, 3);
    private static Vector2 JoinStar => new(1.55f, 2.2f);
    private static Vector2 DustOuter => new(1.45f, 1.95f);
    private static Vector2 DustInner => new(1, 1.55f);
    private static float DustHaloAlpha => .55f;
    private static float DustFadeIn => 14;
    private static float DustShrink => .48f;
    private static float DustLift => 2.5f;
    private static float CollapsePower => 1.45f;
    private static int DustCount => 7;
    private readonly float[] ages = new float[ChargePoints.Maximum];
    private int shown;
    private float recovery;
    // 每颗星至多一层主辉光和一层恢复微光，绘制时先收集，最后一遍叠加。
    private readonly (Vector2 Center, Vector2 Radius, float Strength)[] halos = new (Vector2, Vector2, float)[ChargePoints.Maximum * 2];
    private int haloCount;
    // 双精度累计，长时间运行后呼吸相位仍保持连续。
    private double time;

    internal void Reset(int count, float recovery)
    {
        shown = count;
        this.recovery = recovery;
        Array.Fill(ages, float.PositiveInfinity);
    }

    internal void Update(int count, float recovery, float elapsed)
    {
        this.recovery = recovery;
        time += elapsed;
        for (int i = 0; i < ages.Length; i++) ages[i] += elapsed;
        for (int i = count; i < shown; i++) ages[i] = -(shown - 1 - i) * Stagger;
        for (int i = shown; i < count; i++) ages[i] = -(i - shown) * Stagger;
        shown = count;
    }

    internal void Draw(SpriteBatch batch, Matrix matrix, Vector2 manaOrigin)
    {
        Vector2 start = manaOrigin + Offset;
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, null, matrix);
        haloCount = 0;
        for (int i = 0; i < ages.Length; i++)
        {
            Vector2 center = start + new Vector2(i * Pitch, 0);
            Stamp(batch, center, Socket, SocketInk);
            Stamp(batch, center, SocketInner, HollowInk);
            float age = ages[i];
            if (i < shown) Gain(batch, center, age, i);
            else if (age < 0) Fill(batch, center, 1, 0, i, 0);
            else if (float.IsFinite(age)) Spend(batch, center, age, i);
            if (i == shown) Ember(batch, center, recovery, float.IsFinite(age)
                ? MathHelper.Clamp((age - FlashTime - CollapseTime) / EmberFadeIn, 0, 1) : 1);
        }
        for (int i = 0; i < haloCount; i++)
        {
            var (center, radius, strength) = halos[i];
            Color ink = GlowInk * Math.Min(1, strength);
            ink.A = 0;
            batch.Draw(glow, center, null, ink, 0, new Vector2(glow.Width * .5f),
                radius / (glow.Width * .1875f), SpriteEffects.None, 0);
        }
        batch.End();
    }

    private void Halo(Vector2 center, Vector2 radius, float strength)
    {
        if (strength > 0) halos[haloCount++] = (center, radius, strength);
    }

    private float Breath(int slot) =>
        .5f + .5f * MathF.Sin((float)(time * BreathSpeed % MathF.Tau) - slot * BreathStep);

    // Star.png 是从 HTML 的 star4 按 4× 烘焙的白色笔刷，有效半径为纹理边长的 3/8。
    private void Stamp(SpriteBatch batch, Vector2 center, Vector2 radius, Color ink) =>
        batch.Draw(star, center, null, ink, 0, new Vector2(star.Width * .5f),
            radius / (star.Width * .375f), SpriteEffects.None, 0);

    private void Fill(SpriteBatch batch, Vector2 center, float scale, float flare, int slot, float boost)
    {
        float full = shown == ages.Length ? GlowFull : 0;
        Halo(center, GlowSize * scale, (GlowBase + GlowBreath * Breath(slot) + full) * scale + boost);
        Stamp(batch, center, Core * scale, CoreInk);
        Stamp(batch, center, Inner * scale, InnerInk);
        if (flare > 0) Stamp(batch, center, Flash * scale, BurstInk * flare);
        Stamp(batch, center, Highlight * scale, LightInk);
    }

    private void Ember(SpriteBatch batch, Vector2 center, float progress, float alpha)
    {
        if (progress <= 0 || alpha <= 0) return;
        float eased = 1 - (1 - progress) * (1 - progress);
        Halo(center, GlowSize * (EmberGlowSize * eased), EmberGlow * eased * alpha);
        Stamp(batch, center, Inner * (EmberCore * eased), CoreInk * (EmberAlpha * alpha * (.35f + .65f * progress)));
    }

    private void Gain(SpriteBatch batch, Vector2 center, float age, int slot)
    {
        if (age < 0) return;
        if (age < JoinTime)
        {
            float t = age / JoinTime, arc = MathF.Sin(t * MathF.PI);
            Vector2 offset = JoinOffset * new Vector2(1 - t*t, arc);
            Stamp(batch, center - offset, JoinStar, Color.White * arc);
            Stamp(batch, center + offset, JoinStar, Color.White * arc);
        }
        float grow = MathHelper.Clamp((age - GrowDelay) / GrowTime, 0, 1);
        float settle = MathHelper.Clamp((age - GrowDelay - GrowTime) / SettleTime, 0, 1);
        // 恢复攒满的那一颗：凝聚的微光留到新星长出后再淡去，避免中间空一拍。
        if (slot == shown - 1 && grow < 1) Ember(batch, center, 1, 1 - grow);
        if (grow > 0) Fill(batch, center, (1 - (1-grow)*(1-grow)) * (1 + Overshoot*(1-settle)*(1-settle)), 0,
            slot, GainGlow * (1 - settle) * grow);
    }

    private static float DustDelay => .055f;
    private static float DustLife => .32f;
    private static float DustLifeSpread => .27f;
    private static Vector2 DustRange => new(8, 4);
    private static Vector2 DustSpread => new(10, 6);
    private static float DustBias => -3;
    private static float DustJitter => .55f;
    private static float LargeDust => 1.75f;
    private static float SmallDust => 1.05f;
    private void Spend(SpriteBatch batch, Vector2 center, float age, int slot)
    {
        if (age < FlashTime + CollapseTime)
        {
            float rise = Math.Min(1, age / FlashTime);
            float remaining = 1 - Math.Max(0, (age - FlashTime) / CollapseTime);
            Fill(batch, center, (1 + FlashGrowth * MathF.Sin(rise * MathHelper.PiOver2))
                * MathF.Pow(remaining, CollapsePower), rise * remaining, slot, SpendGlow * rise * remaining);
        }
        if (age < FlashTime || age >= FlashTime + DustDelay + DustLife + DustLifeSpread) return;
        long seed = (slot + 1) * 1297;
        float Next() => (seed = seed * 16807 % 2147483647) / 2147483647f;
        for (int j = 0; j < DustCount; j++)
        {
            float angle = (j + DustJitter * Next()) * MathF.Tau / DustCount;
            Vector2 travel = (DustRange + DustSpread * new Vector2(Next(), Next()))
                * new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            travel.Y += DustBias;
            float life = DustLife + Next()*DustLifeSpread;
            float t = (age - FlashTime - Next()*DustDelay) / life;
            if (t <= 0 || t >= 1) continue;
            Vector2 at = center + travel*(1-(1-t)*(1-t)) - Vector2.UnitY*(DustLift*t*t);
            float size = (j % 3 == 0 ? LargeDust : SmallDust)*(1-DustShrink*t);
            float alpha = Math.Min(1, t*DustFadeIn)*(1-t*t);
            Stamp(batch, at, DustOuter*size, Color.White*(DustHaloAlpha*alpha));
            Stamp(batch, at, DustInner*size, Color.White*alpha);
        }
    }
}
