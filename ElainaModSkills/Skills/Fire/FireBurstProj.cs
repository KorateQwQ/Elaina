using System;
using System.Collections.Generic;
using KL.Drawing;
using KL.Dusts;
using KL.Dusts.Fire;
using KL.Extensions;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.DataStructures;
using Terraria.ModLoader;
using 伊蕾娜.Managers;
using 伊蕾娜.ElainaModSkills.Skills.Recall;

namespace 伊蕾娜.ElainaModSkills.Skills.Fire;

public class FireBurstProj : ElainaBasicProjectile
{
    private const int StarDuration = 30;
    private const int BurstDuration = 30;
    private int Age => (int)Projectile.ai[0];
    private bool IsBursting => Age > StarDuration;
    private int time => Math.Max(0, Age - StarDuration);
    private bool burstEffectsStarted;
    private Action drawVisual;
    private Action drawStar;
    private Action drawCircle;
    private Action drawRearRings;
    private Action drawFrontRings;

    public override void SetDefaults()
    {
        InfusionElement = KL.DamageSystem.ElementType.Fire;
        Projectile.GetGlobalProjectile<KL.DamageSystem.ElementalGlobalProjectile>().SuppressBuildup = true;
        Projectile.timeLeft = StarDuration + BurstDuration;
        Projectile.tileCollide = false;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.hide = true;

        Projectile.friendly = false;
        Projectile.penetrate = -1;
        Projectile.localNPCHitCooldown = 30;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.height = Projectile.width = 50;

        base.SetDefaults();
    }
    
    public override void OnSpawn(IEntitySource source)
    {
        if (Projectile.owner == Main.myPlayer)
        {
            Projectile.ai[1] = Main.rand.NextFloat(-MathHelper.Pi, MathHelper.Pi);
            Projectile.netUpdate = true;
        }
        base.OnSpawn(source);
    }

    private void SpawnBurstEffects()
    {
        /*
        KLBasicDust.SpawnDustsCircle(Projectile.Center+new Vector2(0,30), ModContent.DustType<ShockDust>(), 10, new Vector2(0,-18), 
            1.9f,20, new Color(255, 120, 35,255),new Vector2(0.3f,0.15f),
            0,20,new Vector2(0.8f,0f),7);
            */
        
        /*
        KLBasicDust.SpawnDustsCircle(Projectile.Center+new Vector2(0,50), ModContent.DustType<ShockBlackDust>(), 20, new Vector2(0,-25), 
            1.3f,20, new Color(0, 0, 0,255),new Vector2(0.3f,0.15f),
            0,20,new Vector2(0.8f,0f),7);
            */

        
        // 爆发冲击：LineSparkle 亮线与 FireDust2 火舌从落点向上方扇形快速迸出，寿命短
        KLBasicDust.SpawnDustsCircle(Projectile.Center, ModContent.DustType<LineSparkle>(), 18, new Vector2(0, -28),
            2.6f, 14, new Color(255, 120, 35, 255), new Vector2(2.8f, 0.65f),
            0, 20, new Vector2(0.6f, 0f), 4, 0.3f);

        KLBasicDust.SpawnDustsCircle(Projectile.Center, ModContent.DustType<FireDust2>(), 12, new Vector2(0, -24),
            2.6f, 16, Color.White, new Vector2(0.6f,2f),
            0, 20, new Vector2(0.2f), 4, 0.3f);

        /*// 火柱主体的燃烧火焰：序列帧火焰（Eff_Fire_10 预乘版），沿火柱下半段向上翻涌
        KLBasicDust.SpawnDustsCircle(Projectile.Center, ModContent.DustType<FireBurstFlameDust>(), 10, new Vector2(0, -26),
            0.9f, 26, Color.White, new Vector2(1.4f),
            0, 240, new Vector2(0.4f), 6, 0.4f);*/

        // 火焰残渣：FireDust 小火团低速飘散在火柱周围，寿命长，火柱收起后仍残留片刻
        // 粒子速度每帧衰减（FireDust 为 0.9），总位移约为初速的 10 倍
        KLBasicDust.SpawnDustsCircle(Projectile.Center, ModContent.DustType<FireDust>(), 12, new Vector2(0, -6),
            1.4f, 20, new Color(255,255,255), new Vector2(0.45f),
            0, 280, new Vector2(0.15f), 12, 0.5f);

        //CreateRadialBlur(Projectile.Center,0.005f,16,iteration:5);
    }
    
    public override bool ShouldUpdatePosition()
    {
        return false;
    }

    public override void AI()
    {
        Projectile.ai[0]++;
        Projectile.friendly = IsBursting;

        if (IsBursting)
        {
            if (!burstEffectsStarted)
            {
                burstEffectsStarted = true;
                if (!Main.dedServ) SpawnBurstEffects();
            }
            Lighting.AddLight(Projectile.Center + new Vector2(0, 50), new Vector3(1, 0.5f, 0.1f) / time);
        }
        else
        {
            Lighting.AddLight(Projectile.Center, new Vector3(1f, 0.5f, 0.2f) * (1f - Age / (float)StarDuration));
        }
        base.AI();
    }

    public override bool? CanDamage() => IsBursting ? null : false;

    public override bool PreDraw(ref Color lightColor)
    {
        return false;
    }

    private void DrawVisual()
    {
        if (!Projectile.active || !IsBursting) return;

        Texture2D trail = ModContent.Request<Texture2D>("KL/Effects/Tex/Trail/Eff_Trail_2014", AssetRequestMode.ImmediateLoad).Value;
        Texture2D wave = ModContent.Request<Texture2D>("KL/Effects/Tex/光圈", AssetRequestMode.ImmediateLoad).Value;

        List<FrameInfo> heightInfos =
        [
            new(0.5f, 4, 10),
            new(4, 3, 15),
            new(3, 0, 5)
        ];

        List<FrameInfo> widthInfos =
        [
            new(6, 1, 7, FrameType.SmoothStep),
            new(1, 1, 8),
            new(1, 0, 10)
        ];

        float height = GetFrameValue(heightInfos,time,clamp:true);
        float width = GetFrameValue(widthInfos,time,clamp:true);

        
        List<FrameInfo> heightInfos2 =
        [
            new(3f, 4, 10),
            new(4, 10, 15),
        ];

        List<FrameInfo> widthInfos2 =
        [
            new(5, 1, 10, FrameType.SmoothStep),
            new(1, 0, 5),
        ];
        
        float height2 = GetFrameValue(heightInfos2,time,clamp:true);
        float width2 = GetFrameValue(widthInfos2,time,clamp:true);
        
        // 火柱着色：流纹贴图（Eff_Trail_2014）× 包络（SetEnvelope）的亮度在三个阈值之间连续插值，从外到内 暗红 → 红 → 黄，
        // 颜色再乘贴图亮度；黄层超过 Bloom 阈值，中心晕成白芯。轮廓同原先阈值 0.15 外扩一圈。
        float outerClip = .06f, redClip = .15f, yellowClip = .42f;
        Vector4 outer = new(2f, 0.46f, .06f, 1f);
        Vector4 red = new(2f, 0.82f, .09f, 1f);
        Vector4 yellow = new(3.6f, 2.23f, .65f, 1f);

        Effect flame = ModContent.Request<Effect>("伊蕾娜/Effects/Content/FireBurstFlame", AssetRequestMode.ImmediateLoad).Value;
        EndBeginDraw(2,1,ss:SamplerState.LinearWrap);
        flame.Parameters["BandOuter"].SetValue(outer);
        flame.Parameters["BandMid"].SetValue(red);
        flame.Parameters["BandInner"].SetValue(yellow);
        flame.Parameters["ClipOuter"].SetValue(outerClip);
        flame.Parameters["ClipMid"].SetValue(redClip);
        flame.Parameters["ClipInner"].SetValue(yellowClip);
        flame.Parameters["uTime"].SetValue(new Vector2(time / 45f, 0));
        flame.Parameters["TexSize"].SetValue(new Vector2(trail.Width, trail.Height));
        SetEnvelope(flame);
        flame.CurrentTechnique.Passes["Apply"].Apply();

        DrawInWorld(new TextureInfo(trail,originOffset:new Vector2(trail.Width/2,0)),Projectile.Center,scale:new Vector2(height,width),rotation:3.14f/2);

        DrawInWorld(new TextureInfo(trail,originOffset:new Vector2(trail.Width/2,0)),Projectile.Center,scale:new Vector2(height2,width2),rotation:3.14f/2);

        EndBeginDraw();

        DrawBlackDust();
        
    }

    private void DrawMagicCircle()
    {
        //
        //DrawSystem.SetBloomInfo(1f, 5, Vector2.Zero, true);

        if (!Projectile.active) return;
        float circleRadius = 110f; // 法阵横向半径（世界像素），调大即可整体放大。
        EndBeginDraw();
        try
        {
            Matrix transform = Main.GameViewMatrix.TransformationMatrix *
                Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1);
            Effect effect = ModContent.Request<Effect>("KL/Effects/Content/VertexDraw", AssetRequestMode.ImmediateLoad).Value;
            RecallPortalDrawing.Draw(Main.graphics.GraphicsDevice, effect, transform,
                Projectile.Center - Main.screenPosition, circleRadius, Age, StarDuration, BurstDuration,
                destination: false, hovered: false, burstStart: StarDuration + 1,
                tint: new Color(255, 100, 32), teleportBurst: false);
        }
        finally { EndBeginDraw(); }
    }

    private void DrawRearAirRings()
    {
        if (!Projectile.active || !IsBursting) return;
        DrawAirRings(front: false);
    }

    private void DrawFrontAirRings()
    {
        if (!Projectile.active || !IsBursting) return;
        // DrawDust 已结束它自己的批次；普通 Dust.After 请求没有 Bloom 回调的 Begin 保证。
        // 与水花、杖尖星芒相同，本回调自管批次，返回时保持关闭。
        BeginDraw(defferred: 1);
        DrawAirRings(front: true);
        Main.spriteBatch.End();
    }

    private void DrawAirRings(bool front)
    {
        // 游戏内调参入口：UV 是采样重复倍率，数值越大纹理越密；改材质时同时选择遮罩来源。
        string texturePath = "伊蕾娜/Mounts/Assets/BroomDashWind";
        Vector2 uvScale = new(2f, 1f);
        Vector2 uvOffset = Vector2.Zero;
        float maskFromRed = 0f; // 0：Alpha（当前气流 PNG）；1：RGB 最大值（灰度贴图）。
        Vector4 innerColor = new(3.2f, 0.85f, .06f, 1f);  // 条带朝火柱的整面：HDR 橙红。
        Vector4 outerColor = new(0f, 0f, 0f, 1f);         // 条带背向火柱的整面：黑色。

        Texture2D texture = ModContent.Request<Texture2D>(texturePath, AssetRequestMode.ImmediateLoad).Value;
        Effect effect = ModContent.Request<Effect>("伊蕾娜/Effects/Content/FireBurstFlame", AssetRequestMode.ImmediateLoad).Value;
        Matrix transform = Main.GameViewMatrix.TransformationMatrix *
            Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1);
        EndBeginDraw();
        try
        {
            // 同一条环分后半圈和前半圈夹住火柱；第二圈稍后从法阵升起。
            FireBurstWindDrawing.Draw(Main.graphics.GraphicsDevice, effect, texture, transform,
                Projectile.Center - Main.screenPosition, time, front, uvScale, uvOffset, maskFromRed,
                innerColor, outerColor, radiusStart: 100f, radiusEnd: 225f, riseSpeed: 10f, startAngle: .3f);
            FireBurstWindDrawing.Draw(Main.graphics.GraphicsDevice, effect, texture, transform,
                Projectile.Center - Main.screenPosition, time - 6f, front, uvScale, uvOffset, maskFromRed,
                innerColor, outerColor, radiusStart: 80f, radiusEnd: 180f, riseSpeed: 7f, startAngle: 2.7f);
        }
        finally { EndBeginDraw(); }
    }
    
    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers,
        List<int> overWiresUI)
    {
        if (Main.dedServ) return;
        drawCircle ??= DrawMagicCircle;
        LayerDrawRequestSystem.RequestBloom("Elaina:FireBurst:Circle", LayerDrawRequestSystem.DrawTargetLayer.Projectiles,
            LayerDrawRequestSystem.DrawTiming.After, drawCircle, order: LayerDrawRequestSystem.TrailOrder);
        // 同一弹幕按阶段切换绘制层：星芒在玩家前，爆发的火柱和碎石在弹幕层。
        // 在缓存弹幕时登记，早于 BehindNPCsAndTiles 层绘制。
        if (IsBursting)
        {
            // 与火柱同属 Projectiles.After，共享一次 Bloom；order 只控制这次捕获内的先后顺序。
            // 所有橙色上半圈先画在火柱后，不随单个弹幕回调交错。
            drawRearRings ??= DrawRearAirRings;
            LayerDrawRequestSystem.RequestBloom("Elaina:FireBurst:WindRear", LayerDrawRequestSystem.DrawTargetLayer.Projectiles,
                LayerDrawRequestSystem.DrawTiming.After, drawRearRings, order: LayerDrawRequestSystem.TrailOrder + 1);
            drawVisual ??= DrawVisual;
            LayerDrawRequestSystem.RequestBloom("Elaina:FireBurst", LayerDrawRequestSystem.DrawTargetLayer.Projectiles,
                LayerDrawRequestSystem.DrawTiming.After, drawVisual);
            // 黑色下半圈在整个火柱、粒子与 Bloom 已合成后画到场景上。
            // 不能放在同一 HDR 目标里：高亮乘黑面透明度后仍可能超过 1，显示时依然发亮。
            drawFrontRings ??= DrawFrontAirRings;
            LayerDrawRequestSystem.Request("Elaina:FireBurst:WindFront", LayerDrawRequestSystem.DrawTargetLayer.Dust,
                LayerDrawRequestSystem.DrawTiming.After, drawFrontRings);
        }
        else
        {
            drawStar ??= DrawStar;
            LayerDrawRequestSystem.RequestBloom("Elaina:FireBurst:Star", LayerDrawRequestSystem.DrawTargetLayer.OverPlayers,
                LayerDrawRequestSystem.DrawTiming.After, drawStar);
        }
        base.DrawBehind(index, behindNPCsAndTiles, behindNPCs, behindProjectiles, overPlayers, overWiresUI);
    }


    private void DrawStar()
    {
        if (!Projectile.active || IsBursting) return;
        int time = Age;
        Texture2D line = ModContent.Request<Texture2D>("KL/Effects/Tex/Sparkle/HShotA", AssetRequestMode.ImmediateLoad).Value;
        Texture2D circle = ModContent.Request<Texture2D>("KL/Effects/Tex/光圈渐变", AssetRequestMode.ImmediateLoad).Value;

        List<FrameInfo> lengthInfo = [new(1.8f, 0.6f, 10), new(0.6f, 0.5f, 20)];
        List<FrameInfo> widthInfo = [new(0.2f, 0.0f, StarDuration)];
        List<FrameInfo> rotateInfos = [new(0.01f, 1.5f, StarDuration)];
        float length = GetFrameValue(lengthInfo, time);
        float width = GetFrameValue(widthInfo, time) * 0.5f;
        float rotate = GetFrameValue(rotateInfos, time);
        float totalAlpha = DrawManager.FrameTime(1, 2, 20, time);

        // 保留原有亮度脉动；末段不再衰减到零，避免光环失去 Bloom 后发黑。
        EndBeginDraw(2, 1);
        ReColorEffect(new Vector4(1f, 0.5f, 0.2f, 1f) * 3.5f * totalAlpha);
        DrawInWorld(line, Projectile.Center, Color.White, scale: new Vector2(length, width), Projectile.ai[1] + rotate);
        DrawInWorld(line, Projectile.Center, Color.White, scale: new Vector2(length, width), Projectile.ai[1] + rotate + MathF.PI / 2f);

        if (time > 0)
        {
            EndBeginDraw(2, 1);
            int frame = Math.Min(3, (int)MathHelper.Lerp(0, 3, time / (float)StarDuration));
            List<FrameInfo> scaleInfos = [new(0.0f, 0.2f, 10), new(0.2f, 0.25f, 20)];
            List<FrameInfo> circleRotateInfos = [new(0.01f, 2.5f, StarDuration)];
            float scale = GetFrameValue(scaleInfos, time);
            float circleRotation = GetFrameValue(circleRotateInfos, time);

            List<FrameInfo> intensityInfos = [new(3f, 2.5f, 10), new(2.5f,1.2f, 20)];
            float intensity = GetFrameValue(intensityInfos, time, clamp: true);
            ReColorEffect(new Vector4(1f, 0.5f, 0.1f, 1f) * intensity * totalAlpha);
            DrawInWorld(new TextureInfo(circle, frame, 2, 2), Projectile.Center, Color.White,
                scale: new Vector2(2.5f) * scale, Projectile.ai[1] + circleRotation + MathF.PI / 2f);
        }
        EndBeginDraw();
    }

    // 火柱与碎石共用的包络（拟合原 lightMask）：贴图坐标 u 沿长度、1 为底部
    static void SetEnvelope(Effect effect)
    {
        // 椭圆：中心 u、沿长度半径、横向半径
        Vector3 maskEllipse = new(.577f, .398f, .466f);
        // 沿长度：向底部增强的速率、饱和位置、饱和过渡锐度
        Vector3 maskAlong = new(6.48f, .68f, 18f);
        // 横向：衰减速率、中线附加亮度、中线附加宽度
        Vector3 maskAcross = new(2.21f, .082f, .45f);
        effect.Parameters["MaskEllipse"].SetValue(maskEllipse);
        effect.Parameters["MaskAlong"].SetValue(maskAlong);
        effect.Parameters["MaskAcross"].SetValue(maskAcross);
    }

    // 被喷起的碎石：同火焰龙卷的石块，PerlinX 按阈值裁出岛状碎块，阈值附近为发光烧边；随火柱上升，先从烧边中成形，末段烧尽
    void DrawBlackDust()
    {
        Texture2D quad = ModContent.Request<Texture2D>("KL/Effects/Tex/Noise/5", AssetRequestMode.ImmediateLoad).Value;
        Texture2D noise = ModContent.Request<Texture2D>("KL/Effects/Tex/PerlinX", AssetRequestMode.ImmediateLoad).Value;
        Effect flame = ModContent.Request<Effect>("伊蕾娜/Effects/Content/FireBurstFlame", AssetRequestMode.ImmediateLoad).Value;

        Vector2 quadScale = new(2.5f, 1);
        float riseSpeed = 19f;              // 像素/帧
        float noiseScale = 0.5f / 340f;       // 噪声尺寸，越小碎块越大
        float threshold = .56f;            // 越低碎块越多越大（龙卷石块为 0.6）
        Vector3 stoneColor = new(30 / 255f, 12 / 255f, 8 / 255f);
        float burnEdge = .018f;
        Vector3 burnColor = new Vector3(5f, 1.7f, .3f)*1.5f;
        // 消融：前 8 帧从烧边中成形，保持，末 15 帧烧尽
        float burn = GetFrameValue([new(.4f, 0, 8, TweenEase.EaseOutQuad), new(0, 0, 7), new(0, .4f, 15, TweenEase.EaseInQuad)], time, clamp: true);

        EffectParameterCollection p = flame.Parameters;
        SetEnvelope(flame);
        p["QuadSize"].SetValue(new Vector2(quad.Width, quad.Height) * quadScale);
        p["RubbleScale"].SetValue(noiseScale);
        p["RubbleThreshold"].SetValue(threshold);
        p["RubbleBurn"].SetValue(burn);
        p["StoneColor"].SetValue(stoneColor);
        p["BurnEdge"].SetValue(burnEdge);
        p["BurnColor"].SetValue(burnColor);

        // 三组碎石：位置与原黑烟相同，噪声各自错开
        Span<(Vector2 offset, float seed)> groups = [(Vector2.Zero, 0f), (new(-50, -200), 157f), (new(50, -300), 311f)];
        EndBeginDraw(2, 1, ss: SamplerState.LinearWrap);
        Main.graphics.GraphicsDevice.Textures[1] = noise;
        Main.graphics.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
        foreach (var group in groups)
        {
            p["RubbleOffset"].SetValue(new Vector2(time * riseSpeed, group.seed));
            flame.CurrentTechnique.Passes["Rubble"].Apply();
            DrawInWorld(new TextureInfo(quad, originOffset: new Vector2(quad.Width / 2f, 0)), Projectile.Center + group.offset, scale: quadScale, rotation: 3.14f / 2);
        }
        EndBeginDraw();
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return GamePlayStatic.AABBvLineCollision(targetHitbox,Projectile.Center,Projectile.Center+new Vector2(0,-600),250);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        base.OnHitNPC(target, hit, damageDone);
    }

    public override void OnKill(int timeLeft)
    {
        base.OnKill(timeLeft);
    }
}
