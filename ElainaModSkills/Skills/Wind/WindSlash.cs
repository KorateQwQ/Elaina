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
    private Func<Vector2, float> streakOpacity;
    private float windPointScale = -1f;
    private Vector2 windPointDimensions;

    // 长拖尾的额外淡出起点（按截面序号，0 为刃口、1 为尾尖）。值越小，越早开始变淡。
    private static float TailFadeStart => 0.79f;
    // 月牙前缘短风丝的基础长度（世界像素），从各自所在的弧面向后量。
    private static float FringeLength => 28f;
    // 风丝间距倍率。1 为原间距，调大更稀疏，调小更密。
    private static float FringeSpacing => 1f;
    // 风丝长度随机范围（0～1）。0 时各束长度接近，越接近 1，长短差越明显。
    private static float FringeLengthVariation => 0.57f;
    // 风丝长度偏向。0 为均衡；-1 时中段 2 倍、两翼半长，+1 时两翼 2 倍、中段半长。
    private static float FringeLengthBias => 0.3f;
    // 前缘风丝的不透明度（0～1），调低会让月牙内侧的短风丝变淡。
    private static float FringeOpacity => 0.94f;
    // 无 Bloom 时沿月牙前缘补光：RGB 为加法颜色，W 控制沿椭圆法向的衰减。
    private static Vector4 DirectRimLight => new(0.52f, 0.14f, 0.46f, 16f);
    private static float DirectTrailGainExponent => 0.78f;

    // 长拖尾贴近刃口处的不透明度倍率，调低可减轻拖尾与月牙交叠处的亮块。
    private static float FrontStreakOpacity => 0.10f;
    // 整束长拖尾的亮度倍率。1 为原亮度，0 隐藏长拖尾。
    private static float LongTrailBrightness => 1f;
    // 长拖尾前端颜色（靠月牙）。Vector4 的前三项是 HDR RGB，可超过 1；第四项 A 是不透明度（0～1）。
    // 要加强 KL Bloom，可调大 RGB 的倍率，例如下面的 3.5f。保持三通道比例即可保留色相。
    private static Vector4 TrailFrontColor => new Vector4( new Vector3(1f, 160f / 255f, 239f / 255f)*8.5f, 1f);
    // 长拖尾末端颜色（靠尾尖）。RGBA 从前端沿拖尾插值到此值；末端 A 越低，后段越淡。
    private static Vector4 TrailEndColor => new Vector4(new Vector3(1f, 160f / 255f, 239f / 255f)*3.5f, 0f);
    // 长拖尾从中线到单侧边缘的距离（世界像素），全宽为此值的两倍。
    private static float TrailHalfWidth => 30f;
    // 从月牙椭圆中心向后量到尾尖的距离（世界像素）。
    private static float TrailLengthBehindCenter => 175f;
    // 长拖尾纹理的横向滚动相位。除数越大，纹理流动越慢。
    private static float TrailFlowPhase => (float)(Main.timeForVisualEffects % 1200) / 25f;

    // 三层共用的世界空间轮廓：X 沿飞行方向，Y 沿刃宽方向。
    // 月牙中心相对弹幕中心的前后偏移（世界像素），负值向后移。
    private const float BladeCenterOffset = -25f;
    // 沿飞行方向的椭圆半径（世界像素），调大使弧顶向前突出。
    private const float BladeRadiusX = 76f;
    // 垂直于飞行方向的椭圆半径（世界像素），调大使两翼张得更宽。
    private const float BladeRadiusY = 80f;

    private static Effect crescentFan;
    private static Effect fringeEffect;
    private static Texture2D fadeTexture;
    private static Texture2D arcMaterial;
    private static Texture2D arcNoise;
    private static Texture2D windNoiseTexture;

    public override void Load()
    {
        if (Main.netMode != NetmodeID.Server)
        {
            crescentFan = AssetManager.GetEffect("KL/Effects/Content/CrescentFan");
            fringeEffect = Mod.Assets.Request<Effect>("Effects/Content/WindSlashFringe", AssetRequestMode.ImmediateLoad).Value;
            fadeTexture = AssetManager.GetTexture("伊蕾娜.Effects.Tex.fadeUP");
            arcMaterial = AssetManager.GetTexture("KL/Effects/Tex/background");
            arcNoise = AssetManager.GetTexture("KL/Effects/Tex/水波");
            windNoiseTexture = Mod.Assets.Request<Texture2D>("ElainaModSkills/Skills/Wind/Assets/WindSlashNoise", AssetRequestMode.ImmediateLoad).Value;
        }
        base.Load();
    }

    public override void Unload()
    {
        base.Unload();
    }

    public override void SetDefaults()
    {
        InfusionElement = new(KL.DamageSystem.ElementType.Wind, SkillHitBalance.WindSlashBuildup);
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
        streakOpacity = BladeStreakOpacity;
        windPoints = new Vector2[100];
        windPointScale = -1f;
        UpdateWindPoints(1f);
        base.OnSpawn_AllClient();
    }

    private void UpdateWindPoints(float scale)
    {
        Vector2 dimensions = new(TrailLengthBehindCenter, TrailHalfWidth);
        if (windPointScale == scale && windPointDimensions == dimensions)
            return;

        FillWindPoints(windPoints, dimensions.X, dimensions.Y, scale);
        windPointDimensions = dimensions;
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
        return base.PreDraw(ref lightColor);
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers,
        List<int> overWiresUI)
    {
        LayerDrawRequestSystem.RequestBloom(GetType().FullName, LayerDrawRequestSystem.DrawTargetLayer.OverPlayers,
            LayerDrawRequestSystem.DrawTiming.Before, DrawWind);
        
        base.DrawBehind(index, behindNPCsAndTiles, behindNPCs, behindProjectiles, overPlayers, overWiresUI);
    }

    private void DrawWind()
    {
        Vector2 move = new Vector2(1, 0).RotatedBy(Projectile.velocity.ToRotation())*1f;
        float additiveProgress = MathHelper.Clamp(Projectile.localAI[0] / 10f, 0f, 1f);
        float additiveAlpha = additiveProgress * additiveProgress * (3f - 2f * additiveProgress);
        float appearScale = MathHelper.Lerp(0.78f, 1f, additiveAlpha);
        Vector2 bladeCenter = Projectile.Center + move * (BladeCenterOffset * appearScale);
        Vector2 bladeRadii = new Vector2(BladeRadiusX, BladeRadiusY) * appearScale;
        Vector2 drawScale = new(0.9f * appearScale,0.8f * appearScale);
        Vector2 topScale = new(0.8f * appearScale, 0.8f * appearScale);

        // 月牙与 HDR 风丝使用直通颜色。
        EndBeginDraw(2, 1, ss: SamplerState.LinearClamp);
        DrawCrescentFan(new Vector4(new Color(255, 170, 239).ToVector3() * 1.1f, additiveAlpha), bladeCenter, bladeRadii, topScale);

        DrawFringe(new Vector4(new Color(255, 170, 239).ToVector3() * 2.1f, FringeOpacity * additiveAlpha), bladeCenter, bladeRadii, drawScale, VisualTime);
        
        DrawWindTrails(additiveAlpha, 1.6f * additiveAlpha, bladeCenter, appearScale);
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

    private void DrawFringe(Vector4 effectColor, Vector2 bladeCenter, Vector2 bladeRadii, Vector2 scale, int time)
    {
        Vector2 textureSize = new(arcMaterial.Width * scale.X, arcMaterial.Height * scale.Y);
        Vector2 arcCenter = new(0.43f, 0.5f);
        // 原材质的噪声偏移幅度为 0.74。超过原幅度后，沿同一流动纹理拉开长短差。
        float noiseRange = Math.Min(FringeLengthVariation, 0.37f);
        float extraRange = Math.Max(0f, FringeLengthVariation - 0.37f);
        Vector2 lengthScale = new(1f - 1.35f * extraRange, 1f + 2f * extraRange);
        float wingScale = MathF.Pow(2f, FringeLengthBias);
        Vector2 arcLengthScale = new(1f / wingScale, wingScale);
        float length = FringeLength * bladeRadii.X / BladeRadiusX;
        float requiredDepth = length * lengthScale.Y * (1f + noiseRange) * Math.Max(arcLengthScale.X, arcLengthScale.Y);
        float canvasStretch = Math.Max(1f, requiredDepth / (arcCenter.X * textureSize.X));
        scale.X *= canvasStretch;
        textureSize.X *= canvasStretch;
        Vector2 radii = bladeRadii / textureSize;
        fringeEffect.SetValue("EffectColor", effectColor);
        fringeEffect.SetValue("ArcCenter", arcCenter);
        fringeEffect.SetValue("OuterRadius", radii.Y);
        fringeEffect.SetValue("OuterRadiusY", radii.X);
        fringeEffect.SetValue("FringeDepth", radii.X * FringeLength / BladeRadiusX);
        fringeEffect.SetValue("CanvasStretch", canvasStretch);
        fringeEffect.SetValue("FringeSpacing", FringeSpacing);
        fringeEffect.SetValue("NoiseAmplitude", noiseRange * 2f);
        fringeEffect.SetValue("LengthScale", lengthScale);
        fringeEffect.SetValue("ArcLengthScale", arcLengthScale);
        fringeEffect.SetValue("Phase", (time % 1200) / 55f);
        // 普通场景缓冲会先截断 HDR 再混合。直绘时让材质先乘覆盖率，保住低覆盖处的粉色。
        bool direct = !DrawSystem.GetShouldBloom();
        if (direct)
        {
            EndBeginDraw(0, 1, ss: SamplerState.LinearClamp);
            fringeEffect.SetValue("DirectRimLight", DirectRimLight);
        }
        fringeEffect.SetTexture(1, arcNoise);
        fringeEffect.Apply(direct ? 1 : 0);
        Vector2 centerOffset = ((arcCenter - new Vector2(0.5f)) * textureSize).RotatedBy(Projectile.velocity.ToRotation());
        DrawInWorld(arcMaterial, bladeCenter - centerOffset,
            color: Color.White, scale: scale, rotation: Projectile.velocity.ToRotation());
    }

    private float BladeStreakOpacity(Vector2 section)
    {
        float front = MathHelper.Clamp(section.X / (BladeRadiusX * windPointScale), 0f, 1f);
        return MathHelper.SmoothStep(1f, FrontStreakOpacity, front);
    }

    private void DrawWindTrails(float appearAlpha, float startAlpha, Vector2 bladeCenter, float appearScale)
    {
        UpdateWindPoints(appearScale);
        Vector2 time = new(TrailFlowPhase, 0);
        float rotation = Projectile.velocity.ToRotation();

        // 两端的 HDR 颜色始终用浮点数。KL 按 i / 截面数插值，末端外推一个截面。
        int intervals = windPoints.Length / 2 - 1;
        Vector4 frontColor = TrailFrontColor * appearAlpha;
        Vector4 endColor = TrailEndColor * appearAlpha;
        float noiseAlpha = startAlpha * 1.5f * LongTrailBrightness;
        if (!DrawSystem.GetShouldBloom())
        {
            // 加法混合在普通目标上会先把 RGB 截成白色，将公共增益移到覆盖率，保留风丝色相。
            Vector4 peak = Vector4.Max(frontColor, endColor);
            float gain = MathF.Pow(Math.Max(1f, Math.Max(peak.X, Math.Max(peak.Y, peak.Z))), DirectTrailGainExponent);
            Vector4 channelScale = new(1f / gain, 1f / gain, 1f / gain, 1f);
            frontColor *= channelScale;
            endColor *= channelScale;
            noiseAlpha *= gain;
        }
        endColor += (endColor - frontColor) / intervals;
        DrawRibbonWithOpacity(windNoiseTexture, windPoints, frontColor, endColor, TailFadeStart, streakOpacity,
            startAlpha: noiseAlpha, endAlpha: -noiseAlpha / intervals, blendState: 1, drawTimes: 1,
            uTime: time, attachPoint: bladeCenter, attachRotation: rotation,
            imageScale: new Vector2(2f, 1f), useRforAlpha: false,
            debugPoint: false);
    }

}
