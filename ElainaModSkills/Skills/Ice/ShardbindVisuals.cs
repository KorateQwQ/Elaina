using System;
using KL.Drawing;
using KL.Dusts;
using KL.Dusts.Burst;
using KL.Dusts.Glow;
using KL.Dusts.Smoke;
using KL.Utils;
using static 伊蕾娜.ElainaModSkills.Skills.Ice.ShardbindVisualSettings;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

/// <summary>正式技能与 FNA 预览共用。全部固定贴图从已打包资源读取。</summary>
public static class ShardbindVisuals
{
    private static Effect SheetEffect => ModContent.Request<Effect>("伊蕾娜/Effects/Content/ShardbindChargeSheet", AssetRequestMode.ImmediateLoad).Value;
    private static Texture2D SheetCarrier => AssetManager.GetTexture("伊蕾娜.Effects.Tex.Baked.WhitePixel");

    public static float Charge(int time) => time < IceCycleBalance.LockChargeStartFrame || time >= IceCycleBalance.LockBurstFrame
        ? 0f : MathHelper.SmoothStep(0f, 1f, (time - IceCycleBalance.LockChargeStartFrame)
            / (float)(IceCycleBalance.LockBurstFrame - IceCycleBalance.LockChargeStartFrame));

    public static void DrawCharge(Vector2 center, int time)
    {
        float charge = Charge(time);
        if (charge <= 0f) return;
        DrawRays(center, time);
        Texture2D glow = AssetManager.GetTexture("KL.Effects.Tex.background_Premultiplied");
        EndBeginDraw();
        DrawInWorld(glow, center, SphereOuterColor * (charge * SphereOuterOpacity),
            new Vector2(MathHelper.Lerp(SphereOuterSize.X, SphereOuterSize.Y, charge)), 0);
        DrawInWorld(glow, center, SphereInnerColor * (charge * charge * SphereInnerOpacity),
            new Vector2(MathHelper.Lerp(SphereInnerSize.X, SphereInnerSize.Y, charge)), 0);
        EndBeginDraw();
    }

    public static void DrawRays(Vector2 center, int time)
    {
        if (Charge(time) <= 0f) return;
        Effect effect = SheetEffect;
        effect.Parameters["EdgeSoftness"].SetValue(RayEdgeSoftness);
        effect.Parameters["TailStart"].SetValue(RayTailStart);
        effect.Parameters["RootFade"].SetValue(RayRootFade);
        EndBeginDraw(2, 1, shader: effect);
        for (int i = 0; i < RayCount; i++)
        {
            var ray = GetRay(i);
            int start = IceCycleBalance.LockChargeStartFrame + ray.Delay;
            if (time <= start || start >= IceCycleBalance.LockBurstFrame) continue;
            float progress = MathHelper.Clamp((time - start) / (float)(IceCycleBalance.LockBurstFrame - start), 0f, 1f);
            float width = MathHelper.Lerp(RayInitialHalfWidth, ray.HalfWidth, MathHelper.SmoothStep(0, 1, progress));
            float strength = MathHelper.SmoothStep(0, 1, MathHelper.Clamp(progress * RayFadeInSpeed, 0, 1));
            float angle = MathHelper.ToRadians(ray.Angle);
            Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
            effect.Parameters["SheetColor"].SetValue(new Vector4(RayColor * RayHdr, RayOpacity * strength));
            effect.Parameters["RootWidth"].SetValue(RayRootHalfWidth / width);
            Main.spriteBatch.Draw(SheetCarrier, center + direction * RayRootOffset - Main.screenPosition,
                null, Color.White, angle, new Vector2(0, .5f),
                new Vector2(ray.Length, width * 2), SpriteEffects.None, 0);
        }
        EndBeginDraw();
    }

    // 只发射当前帧的事件。晚到的网络快照跳过旧星闪，避免在一帧补放整串闪光。
    public static void EmitEvents(Vector2 center, int time)
    {
        if (Main.dedServ) return;
        if (time < IceCycleBalance.LockBurstFrame)
        {
            for (int i = 0; i < StarCount; i++)
            {
                var star = GetStar(i);
                if (time == FirstRayFrame + StarStartDelay + star.Delay)
                    ShardbindFlashDust.Emit(center + star.Offset, false, i);
            }
            return;
        }
        EmitBurstEvents(center, time - IceCycleBalance.LockBurstFrame);
    }

    public static void EmitBurstEvents(Vector2 center, int age)
    {
        EmitGroup<SmokeDust2>(center, age, BurstSmoke);
        if (age == ShardDelay) IceShardDust.EmitShardbindBurst(center);
        EmitGroup<KL.Dusts.Burst.BurstPoint>(center, age, ShardbindVisualSettings.BurstPoint);
        EmitGroup<BurstDust>(center, age, BurstStreaks);
        EmitGroup<ShardbindLineDust>(center, age, BurstLines);
        EmitGroup<GlowDust>(center, age, BurstAfterglow);
        EmitGroup<ShardbindLineDust>(center, age, BurstFastLines);
        if (age == BurstFlashDelay) ShardbindFlashDust.Emit(center, true);
    }

    private static void EmitGroup<T>(Vector2 center, int age, BurstGroup group) where T : ModDust
    {
        if (age != group.Delay || group.Count <= 0) return;
        KLBasicDust.SpawnDustsCircle(center, ModContent.DustType<T>(), group.Count,
            velocity: group.Speed.RotatedBy(MathHelper.ToRadians(group.Angle)),
            rotRange: MathHelper.ToRadians(group.Arc), lifeTime: group.Life, lifeOffset: group.LifeJitter,
            velocityOffset: group.SpeedJitter, scale: group.Size, scaleOffset: group.SizeJitter,
            color: group.Color, startDistance: group.Radius, startOffset: group.RadiusJitter);
    }
}
