using System;
using KL.Extensions;
using KL.Utils;
using Terraria.GameContent;
using Terraria.ID;

namespace 伊蕾娜.ElainaModSkills.Skills.Wind;

public class WindSlash : ElainaBasicProjectile
{
    private Vector2[] windPoints;
    private Vector2[] windPoints3;

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
        Projectile.localNPCHitCooldown = 30;
        base.SetDefaults();
    }

    public override void OnSpawn_AllClient()
    {
        Vector2 move = new(1, 0);
        windPoints = QuickConePoints(move * 50, -move * 100f, 100, 140, 140, 0.1f);
        windPoints3 = QuickConePoints(move * 50, -move * 200f, 100, 30, 70, 0.1f);
        base.OnSpawn_AllClient();
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 toward = new Vector2(1, 0).RotatedBy(Projectile.velocity.ToRotation());
        return AABBvLineCollision(targetHitbox, Projectile.Center + toward * 100,
            Projectile.Center - toward * 100, 300);
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
        const float collisionHalfWidth = 150f;
        const float sampleSpacing = 16f;
        int sampleCount = Math.Max(1, (int)(collisionHalfWidth * 2f / sampleSpacing));

        for (int i = 0; i <= sampleCount; i++)
        {
            float offset = MathHelper.Lerp(-collisionHalfWidth, collisionHalfWidth, i / (float)sampleCount);
            CutTile(Projectile.Center + widthDirection * offset, true);
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        DrawWind();
        EndBeginDraw();
        return base.PreDraw(ref lightColor);
    }

    private void DrawWind()
    {
        Vector2 move = new Vector2(1, 0).RotatedBy(Projectile.velocity.ToRotation())*1;
        float additiveProgress = MathHelper.Clamp(Projectile.localAI[0] / 10f, 0f, 1f);
        float additiveAlpha = additiveProgress * additiveProgress * (3f - 2f * additiveProgress);
        float baseProgress = MathHelper.Clamp(Projectile.localAI[0] / 15f, 0f, 1f);
        float baseAlpha = baseProgress * baseProgress * (3f - 2f * baseProgress);
        float appearScale = MathHelper.Lerp(0.78f, 1f, additiveAlpha);
        float offset = -21f * appearScale;
        Vector2 drawScale = new(0.9f * appearScale);
        Vector2 topScale = new(0.8f * appearScale, 1.2f * appearScale);
        Color pink = new(255, 160, 239, 255);
        
        // 先铺黑底，避免后续 additive 绘制产生颜色曝光。
        EndBeginDraw(2, 1, ss: SamplerState.LinearClamp);
        DrawCrescentFan(new Vector4(new Color(255, 120, 239).ToVector3() * 0.8f, 0.9f * baseAlpha), 1f, move*1.0f, topScale*1f);
        DrawArcFlame(new Vector4(new Color(255, 120, 239).ToVector3() * 0.5f, 0.5f * baseAlpha), move, offset*1.0f, drawScale*1.0f,VisualTime);
        DrawWindTrails(new Color(255, 160, 239)* 0.8f * baseAlpha, new Color(255, 160, 239)* 0.5f * baseAlpha, 0.8f * baseAlpha, 0f, 2);

        // 黑底完成后再绘制 additive 高光。
        EndBeginDraw(1, 1, ss: SamplerState.LinearClamp);
        DrawCrescentFan(new Color(255, 160, 239, 255).ToVector4() * additiveAlpha, 1f, move, topScale);

        DrawArcFlame(new Vector4(new Color(255, 180, 239).ToVector3() * 0.9f, 1.0f * additiveAlpha), move, offset+0, drawScale,VisualTime+0);
        DrawWindTrails(pink * additiveAlpha, pink * additiveAlpha, 1.3f * additiveAlpha, 0.0f, 1);
    }

    private void DrawCrescentFan(Vector4 effectColor, float positionScale, Vector2 move, Vector2 scale)
    {
        crescentFan.SetValue("OuterRadius", 0.37f);
        crescentFan.SetValue("OuterRadiusY", positionScale == 1f ? 0.37f : 0.34f);
        crescentFan.SetValue("InnerRadius", 0.36f);
        crescentFan.SetValue("InnerRadiusY", 0.42f);
        crescentFan.SetValue("InnerOffset", new Vector2(-0.06f, 0f));
        crescentFan.SetValue("TextureRotation", -1.57f);
        crescentFan.SetValue("TextureScale", new Vector2(1f, 1.57f));
        crescentFan.SetValue("TextureFlow", new Vector2(0f, 0.36f));
        crescentFan.SetValue("EffectColor", effectColor);
        crescentFan.SetValue("EdgeSoftness", 0.1f);
        crescentFan.Apply();
        DrawInWorld(fadeTexture, Projectile.Center + move * -27f * positionScale,
            color: Color.White, scale: scale, rotation: Projectile.velocity.ToRotation());
    }

    private void DrawArcFlame(Vector4 effectColor, Vector2 move, float offset, Vector2 scale,int time)
    {
        arcFlame.SetValue("EffectColor", effectColor);
        arcFlame.SetValue("sweepDirection", new Vector2(1f, 0f));
        arcFlame.SetValue("ArcCenter", new Vector2(0.43f, 0.5f));
        arcFlame.SetValue("OuterRadius", 0.52f);
        arcFlame.SetValue("OuterRadiusY", 0.38f);
        arcFlame.SetValue("EdgeSoftness", 0f);
        arcFlame.SetValue("TextureRotation", 0f);
        arcFlame.SetValue("TextureScale", new Vector2(0.5f));
        arcFlame.SetValue("TextureFlow", new Vector2(-0.1f, 0f));
        arcFlame.SetValue("useRGBforApha", false);
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
        DrawInWorld(arcMaterial, Projectile.Center + move * offset,
            color: Color.White, scale: scale, rotation: Projectile.velocity.ToRotation());
    }

    private void DrawWindTrails(Color startColor, Color endColor, float startAlpha, float endAlpha, int blendState)
    {
        Vector2 move = new Vector2(1, 0).RotatedBy(Projectile.velocity.ToRotation());
        Vector2 time = new((float)(Main.timeForVisualEffects % 1200) / 25f, 0);
        Vector2 attachPoint = Projectile.Center - move.RotatedBy(Projectile.rotation);

        VertexDrawEffect(windTexture, windPoints, startColor, endColor,
            startAlpha: startAlpha, endAlpha: endAlpha, blendState: blendState, drawTimes: 1,
            uTime: time, attachPoint: attachPoint, attachRotation: Projectile.rotation,
            imageScale: new Vector2(2f, 1f), useRforAlpha: false,
            debugPoint: false);
        VertexDrawEffect(windNoiseTexture, windPoints3, startColor, endColor,
            startAlpha: startAlpha * 1.5f, endAlpha: endAlpha, blendState: blendState, drawTimes: 1,
            uTime: time, attachPoint: attachPoint, attachRotation: Projectile.rotation,
            imageScale: new Vector2(2f, 1f), useRforAlpha: false,
            debugPoint: false);
    }

}
