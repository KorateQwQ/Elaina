using System;
using KL.Drawing;
using KL.Extensions;
using KL.Utils;
using Terraria.GameContent;
using Terraria.ID;

namespace 伊蕾娜.ElainaModSkills.Skills.Wind;

public class WindSlash : ElainaBasicProjectile
{
    private Vector2[] windPoints;
    private Vector2[] windPoints3;
    private float windPointScale = -1f;

    // 三层共用的世界空间轮廓：X 沿飞行方向，Y 沿刃宽方向。
    private const float BladeCenterOffset = -25f;
    private const float BladeRadiusX = 76f;
    private const float BladeRadiusY = 80f;

    private static Effect crescentFan;
    private static Effect arcFlame;
    private static Texture2D fadeTexture;
    private static Texture2D arcMaterial;
    private static Texture2D arcNoise;
    private static Texture2D windTexture;
    private static Texture2D windNoiseTexture;

    public override void Load()
    {
        if (Main.netMode != NetmodeID.Server)
        {
            crescentFan = AssetManager.GetEffect("KL/Effects/Content/CrescentFan");
            arcFlame = AssetManager.GetEffect("KL/Effects/Content/ArcFlame");
            fadeTexture = AssetManager.GetTexture("伊蕾娜.Effects.Tex.fadeUP");
            arcMaterial = AssetManager.GetTexture("KL/Effects/Tex/background");
            arcNoise = AssetManager.GetTexture("KL/Effects/Tex/水波");
            windTexture = AssetManager.GetTexture("KL/Effects/Tex/Wind/wind3");
            windNoiseTexture = AssetManager.GetTexture("KL/Effects/Tex/Wind/windNoi");
        }

        base.Load();
    }

    public override void Unload()
    {
        base.Unload();
    }

    public override void SetDefaults()
    {
        Projectile.tileCollide = false;
        Projectile.timeLeft = 600;
        Projectile.friendly = true;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        // 每道风刃对同一敌人只结算一次，与配置中单次命中的伤害预算一致
        Projectile.localNPCHitCooldown = -1;
        base.SetDefaults();
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        // 击退方向跟随风刃飞行方向，而不是敌人相对玩家的位置
        modifiers.HitDirectionOverride = Projectile.velocity.X >= 0 ? 1 : -1;
        base.ModifyHitNPC(target, ref modifiers);
    }

    public override void OnSpawn_AllClient()
    {
        windPoints = new Vector2[100];
        windPoints3 = new Vector2[100];
        windPointScale = -1f;
        UpdateWindPoints(1f);
        base.OnSpawn_AllClient();
    }

    private void UpdateWindPoints(float scale)
    {
        if (windPointScale == scale)
            return;

        FillWindPoints(windPoints, 75f, BladeRadiusY, scale);
        FillWindPoints(windPoints3, 175f, 20f, scale);
        windPointScale = scale;
    }

    private static void FillWindPoints(Vector2[] points, float tailLength, float halfWidth, float scale)
    {
        int pairs = points.Length / 2;
        for (int i = 0; i < pairs; i++)
        {
            // 前端加密，避免细条纹的短圆弧退化为尖角；后段保留原来的拖长距离。
            float progress = i / (float)(pairs - 1);
            float x = MathHelper.Lerp(BladeRadiusX, -tailLength, progress * progress);
            float ellipseX = MathHelper.Clamp(x / BladeRadiusX, 0f, 1f);
            float y = Math.Min(halfWidth, BladeRadiusY * MathF.Sqrt(1f - ellipseX * ellipseX));
            points[i * 2] = new Vector2(x, y) * scale;
            points[i * 2 + 1] = new Vector2(x, -y) * scale;
        }
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 toward = new Vector2(1, 0).RotatedBy(Projectile.velocity.ToRotation());
        return AABBvLineCollision(targetHitbox, Projectile.Center + toward * 100,
            Projectile.Center - toward * 100, 200);
    }

    public override void AI()
    {
        Projectile.localAI[0]++;
        Projectile.rotation = Projectile.velocity.ToRotation();
        if (HasAuthority())
        {
            CutTilesInWindHitbox();
        }
        Lighting.AddLight(Projectile.Center,new Color(255, 160, 239).ToVector3());
        base.AI();
    }

    private void CutTilesInWindHitbox()
    {
        Vector2 direction = new Vector2(1f, 0f).RotatedBy(Projectile.velocity.ToRotation());
        Vector2 widthDirection = direction.RotatedBy(MathHelper.PiOver2);
        const float collisionHalfWidth = 200f;
        const float sampleSpacing = 16f;
        int sampleCount = Math.Max(1, (int)(collisionHalfWidth / sampleSpacing));

        for (int i = 0; i <= sampleCount; i++)
        {
            float offset = MathHelper.Lerp(-collisionHalfWidth, collisionHalfWidth, i / (float)sampleCount);
            CutTile(Projectile.Center + widthDirection * offset, true);
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        LayerDrawRequestSystem.RequestBloom(GetType().FullName, LayerDrawRequestSystem.DrawTargetLayer.Projectiles,
            LayerDrawRequestSystem.DrawTiming.After, DrawWind);
        return base.PreDraw(ref lightColor);
    }

    private void DrawWind()
    {
        Vector2 move = new Vector2(1, 0).RotatedBy(Projectile.velocity.ToRotation())*1f;
        float additiveProgress = MathHelper.Clamp(Projectile.localAI[0] / 10f, 0f, 1f);
        float additiveAlpha = additiveProgress * additiveProgress * (3f - 2f * additiveProgress);
        float appearScale = MathHelper.Lerp(0.78f, 1f, additiveAlpha);
        Vector2 bladeCenter = Projectile.Center + move * (BladeCenterOffset * appearScale);
        Vector2 bladeRadii = new Vector2(BladeRadiusX, BladeRadiusY) * appearScale;
        Vector2 drawScale = new(0.9f * appearScale,0.6f * appearScale);
        Vector2 topScale = new(0.8f * appearScale, 0.8f * appearScale);
        Color pink = new(255, 160, 239, 255);

        // 两层材质保留 NonPremultiplied，条纹保留自己的混合方式。
        EndBeginDraw(2, 1, ss: SamplerState.LinearClamp);
        DrawCrescentFan(new Vector4(new Color(255, 180, 239).ToVector3()*1.3f,1 * additiveAlpha), bladeCenter, bladeRadii, topScale);

        DrawArcFlame(new Vector4(new Color(255, 200, 239).ToVector3() * 1.6f, 1.0f * additiveAlpha), bladeCenter, bladeRadii, drawScale,VisualTime+0);
        
        DrawWindTrails(pink * additiveAlpha, pink * additiveAlpha, 1.6f * additiveAlpha, 0.0f, 1, bladeCenter, appearScale);
    }

    private void DrawCrescentFan(Vector4 effectColor, Vector2 bladeCenter, Vector2 bladeRadii, Vector2 scale)
    {
        Vector2 radii = bladeRadii / new Vector2(fadeTexture.Width * scale.X, fadeTexture.Height * scale.Y);
        crescentFan.SetValue("OuterRadius", radii.X);
        crescentFan.SetValue("OuterRadiusY", radii.Y);
        crescentFan.SetValue("InnerRadius", radii.X * (0.36f / 0.37f));
        crescentFan.SetValue("InnerRadiusY", radii.Y * (0.42f / 0.37f));
        crescentFan.SetValue("InnerOffset", new Vector2(radii.X * (-0.06f / 0.37f), 0f));
        crescentFan.SetValue("TextureRotation", -1.57f);
        crescentFan.SetValue("TextureScale", new Vector2(1f, 1.57f));
        crescentFan.SetValue("TextureFlow", new Vector2(0f, 0.36f));
        // fadeUP 是 Alpha 全满的灰度渐变，暗部应透明，不能作为黑色材质覆盖背景。
        crescentFan.SetValue("useRGBforApha", true);
        crescentFan.SetValue("EffectColor", effectColor);
        crescentFan.SetValue("EdgeSoftness", 0.1f);
        crescentFan.Apply();
        DrawInWorld(fadeTexture, bladeCenter,
            color: Color.White, scale: scale, rotation: Projectile.velocity.ToRotation());
    }

    private void DrawArcFlame(Vector4 effectColor, Vector2 bladeCenter, Vector2 bladeRadii, Vector2 scale,int time)
    {
        Vector2 textureSize = new(arcMaterial.Width * scale.X, arcMaterial.Height * scale.Y);
        Vector2 radii = bladeRadii / textureSize;
        Vector2 arcCenter = new(0.43f, 0.5f);
        arcFlame.SetValue("EffectColor", effectColor);
        arcFlame.SetValue("sweepDirection", new Vector2(1f, 0f));
        arcFlame.SetValue("ArcCenter", arcCenter);
        // ArcFlame 的 rx 沿切线、ry 沿朝向，与月牙 Shader 的 XY 定义相反。
        arcFlame.SetValue("OuterRadius", radii.Y);
        arcFlame.SetValue("OuterRadiusY", radii.X);
        arcFlame.SetValue("EdgeSoftness", 0f);
        arcFlame.SetValue("TextureRotation", 0f);
        arcFlame.SetValue("TextureScale", new Vector2(0.5f));
        arcFlame.SetValue("TextureFlow", new Vector2(-0.1f, 0f));
        arcFlame.SetValue("useRGBforApha", true);
        arcFlame.SetValue("iTimeProgress", 0.6f);
        arcFlame.SetValue("edgeWidth", 0.2f);
        arcFlame.SetValue("noiseStrength", 0.2f);
        arcFlame.SetValue("curveStrength", 0.45f);
        arcFlame.SetValue("radialCenter", new Vector2(0.5f));
        arcFlame.SetValue("dissolveRotation", 1.57f);
        arcFlame.SetValue("dissolveScale", new Vector2(2f, 0.05f));
        arcFlame.SetValue("iTimeDisolve", new Vector2(0f, (float)(time % 1200) / 55f));
        arcFlame.SetTexture(1, arcNoise);
        arcFlame.Apply();
        Vector2 centerOffset = ((arcCenter - new Vector2(0.5f)) * textureSize).RotatedBy(Projectile.velocity.ToRotation());
        DrawInWorld(arcMaterial, bladeCenter - centerOffset,
            color: Color.White, scale: scale, rotation: Projectile.velocity.ToRotation());
    }

    private void DrawWindTrails(Color startColor, Color endColor, float startAlpha, float endAlpha, int blendState,
        Vector2 bladeCenter, float appearScale)
    {
        UpdateWindPoints(appearScale);
        Vector2 time = new((float)(Main.timeForVisualEffects % 1200) / 25f, 0);
        float rotation = Projectile.velocity.ToRotation();

        VertexDrawEffect(windTexture, windPoints, startColor, endColor,
            startAlpha: startAlpha, endAlpha: endAlpha, blendState: blendState, drawTimes: 1,
            uTime: time, attachPoint: bladeCenter, attachRotation: rotation,
            imageScale: new Vector2(2f, 1f), useRforAlpha: false,
            debugPoint: false);
        VertexDrawEffect(windNoiseTexture, windPoints3, startColor, endColor,
            startAlpha: startAlpha * 1.5f, endAlpha: endAlpha, blendState: blendState, drawTimes: 1,
            uTime: time, attachPoint: bladeCenter, attachRotation: rotation,
            imageScale: new Vector2(2f, 1f), useRforAlpha: false,
            debugPoint: false);
    }

}
