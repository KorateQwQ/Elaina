using System;
using KL.Drawing;
using KL.Dusts;
using static 伊蕾娜.ElainaModSkills.Skills.Ice.ShardbindVisualSettings;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

// 冰凌锁专属亮线，参数热重载只影响本技能。
public sealed class ShardbindLineDust : KLBasicDust
{
    public override string Texture => "KL/Effects/Tex/Sparkle/ShotLine";
    public override bool PreDraw(Dust dust) => false;
    public override void OnSpawn(Dust dust)
    {
        dust.noGravity = dust.noLight = true;
        base.OnSpawn(dust);
    }
    public override bool Update(Dust dust)
    {
        dust.velocity *= BurstLineDrag;
        return base.Update(dust);
    }
    public override void SubmitBloomDrawRequest(Dust dust)
    {
        object data = dust.customData;
        LayerDrawRequestSystem.RequestBloom("Elaina:Shardbind:Lines", LayerDrawRequestSystem.DrawTargetLayer.OverPlayers,
            LayerDrawRequestSystem.DrawTiming.Before, context =>
        {
            if (context.IsFirst) EndBeginDraw(0, 1, ss: SamplerState.LinearClamp);
            if (dust.active && dust.type == Type && ReferenceEquals(dust.customData, data))
            {
                float fade = MathF.Pow(MathHelper.Clamp(1f - dust.LifeProgress(), 0f, 1f), BurstLineFadePower);
                float hdr = MathHelper.Lerp(BurstLineHdrRange.X, BurstLineHdrRange.Y, fade);
                ReColorEffect(new Vector4(hdr, hdr, hdr, 1f));
                Color color = dust.color;
                color.A = (byte)(255 * fade);
                Vector2 size = dust.Size();
                Main.spriteBatch.Draw(MainTexture, dust.position - Main.screenPosition, new Rectangle(0, 45, 244, 12), color,
                    dust.rotation, new Vector2(122, 6),
                    new Vector2(.2f * size.X * fade, .25f * size.Y * MathHelper.Lerp(.4f, 1f, fade)), SpriteEffects.None, 0);
            }
            if (context.IsLast) EndBeginDraw();
        }, order: LayerDrawRequestSystem.DustOrder);
    }
    public override void Unload() => MainTexture = null;
}
