using KL.Drawing;
using KL.Extensions;
using 伊蕾娜.Managers;
using DrawHelper = 伊蕾娜.Managers.DrawHelper;

namespace 伊蕾娜.ElainaModSkills.Skills.Fire;

public class FireTornado : ElainaBasicProjectile
{
    float tornadoScale = 1f;
    float totalFadeTime = 0;
    int time = 0;
    private float fadeNeedTime = 60;
    // 龙卷主体中形状相同的四层共用，合并为一次绘制
    private readonly TornadoLayerShape bodyShape = new()
    {
        CameraZ = 2.5f,
        DrawScale = new Vector2(0.4f),
        RotationSpeed = -1.4f,
        HorizontalFadeRange = 0.02f,
        VerticalFadeRange = 0.1f,
        MaxInset = 0.25f,
        BezierP0 = new Vector2(-5, 0),
        BezierP1 = new Vector2(2.85f, 0.45f),
        BezierP2 = new Vector2(3f, 0.95f),
        BezierP3 = new Vector2(-8, 1),
        DisplaceNoiseScale = new Vector2(1.1f),
        DisplaceAmount = 0.05f,
        DisplaceNoiseBias = 0,
        DisplaceNoiseContrast = 1,
        DisplaceNoiseSpeed = new Vector2(-0.3f),
    };
    public override void SetDefaults()
    {
        totalFadeTime = 1;
        Projectile.timeLeft = 150;
        fadeNeedTime = 60;
        base.SetDefaults();
    }

    public override void AI()
    {
        Projectile.velocity = new Vector2(0, 0);

        tornadoScale = 1f;
        if(Main.mouseRight)Projectile.Kill();
        time++;
        if (Projectile.timeLeft <= fadeNeedTime)
        {
            totalFadeTime+=1/fadeNeedTime;
        }
        else if (time < fadeNeedTime)
        {
            totalFadeTime-=1/fadeNeedTime;
        }
        base.AI();
    }

    //绘制请求在 DrawBehind 中提交
    public override bool PreDraw(ref Color lightColor)
    {
        return false;
    }

    private void DrawVisual()
    {
        Asset<Texture2D> wind = ModContent.Request<Texture2D>("KL/Effects/Tex/Wind/Eff_Noise_57");
        Asset<Texture2D> wind1 = ModContent.Request<Texture2D>("KL/Effects/Tex/Wind/wind1");
        Asset<Texture2D> wind2 = ModContent.Request<Texture2D>("KL/Effects/Tex/Wind/wind2");
        Asset<Texture2D> wind3 = ModContent.Request<Texture2D>("KL/Effects/Tex/Wind/Eff_Noise_705");

        Asset<Texture2D> cellnoise = ModContent.Request<Texture2D>("KL/Effects/Tex/cellnoise");
        Asset<Texture2D> FireNoi = ModContent.Request<Texture2D>("KL/Effects/Tex/Noise/Chap04_Eff_A203_WaterEffect_04");
        Asset<Texture2D> waterNoi = ModContent.Request<Texture2D>("KL/Effects/Tex/水波");
        Asset<Texture2D> displaceNoi = ModContent.Request<Texture2D>("KL/Effects/Tex/Noise/Eff_Noise_20");

        Asset<Texture2D> voronoiOrigin = ModContent.Request<Texture2D>("KL/Effects/Tex/voronoiOrigin");
        Vector2 windSlashOffset = new Vector2(0, 400);
        float totalTilt = -0.1f;
        float rotTime =time / 60f+0.5f;
        Asset<Texture2D> windSlash = ModContent.Request<Texture2D>("KL/Effects/Tex/222545rguggggwbpjh1j9h");
        //rotTime = 0;

        float slashRotation = rotTime * 7;
        float slashFadeTime = DrawManager.FrameTime(0.2f, 0.5f, 30, time);
        float slashHeight =  DrawManager.FrameTime(0.8f, 1.8f, 30, time);
        float slashWidth =  0.04f;

        if (Projectile.GetCurrentDrawLayer() == DrawSystem.DrawLayer.BehindNPCsAndTiles)
        {
            //底层环绕slash（亮色 Additive 层 + 暗色描边层，一次绘制）
            DrawSlash(windSlash.Value, windSlashOffset, slashRotation, slashFadeTime, slashWidth, slashHeight, true);
            EndBeginDraw();
            return;
        }


        float light = 1.0f;
        if (Projectile.timeLeft < 30) light = MathHelper.Lerp(1.0f, 1.0f, Projectile.timeLeft / 30f);

        //底层垫色（防止画面过曝）+ 旋风 + 高亮旋风，形状相同，合并为一次绘制。
        //高亮旋风原本是 Additive，与之后的高亮残片同为 Additive，顺序可交换。
        bodyShape.Time = rotTime;
        bodyShape.NoiseTexture = displaceNoi.Value;
        bodyShape.DissolveTexture = PerLinNoiseX;
        bodyShape.DissolveThreshold = GetDissolveAmount();
        Color fadedColor = new Color(255,100,50,255)*(1-totalFadeTime);
        EndBeginDraw(0,1,ss:SamplerState.LinearWrap);
        DrawTornadoLayers(Projectile.Center, bodyShape, [
            new TornadoLayer(waterNoi.Value, fadedColor, new Vector2(1.2f,2.3f)*2.0f) { UseRForAlpha = true, Tilt = 1.0f },
            new TornadoLayer(wind1.Value, fadedColor, new Vector2(1.2f,2.3f)*2.0f) { Tilt = 1.0f },
            new TornadoLayer(waterNoi.Value, new Color(255,70,20,255)*(1-totalFadeTime), new Vector2(1.3f,2.3f)*2.0f) { EffectColor = new Vector4(1.2f), Tilt = 2.0f },
            new TornadoLayer(wind1.Value, new Color(255,100,30,255), new Vector2(1.3f,2.3f)*2.0f) { EffectColor = new Vector4(2.5f*light), Tilt = 1.0f, Additive = true },
        ]);

        //高亮残片
        EndBeginDraw(1,1,ss:SamplerState.LinearWrap);
        TornadoEffect(new Vector4(4.5f*light),true, rotTime,2.5f,new Vector2(0.4f),-0.4f,new Vector2(1),1.5f,horizontalFadeRange:0.02f,verticalFadeRange:0.02f,
            maxInset:0.25f,bezierP0:new Vector2(-5,0),bezierP1:new Vector2(1.85f,0.45f),bezierP2:new Vector2(1f,0.95f),bezierP3:new Vector2(-5,1),
            dissolveTex:PerLinNoiseX,dissolveTexTiling:new Vector2(0.5f),dissolveThreshold:GetDissolveAmount(0.5f),dissolveEdgeWidth:0.00f,
            noiseTex:displaceNoi.Value,displaceNoiseScale:new Vector2(1.1f),displaceAmount:0.05f,displaceNoiseBias:0,displaceNoiseContrast:1,displaceNoiseSpeed:new Vector2(-0.3f));
        
        DrawInWorld(cellnoise.Value,Projectile.Center+new Vector2(-0,0),color: new Color(255,100,30,255),new Vector2(0.4f,1.2f)*1.6f);

        //外圈黑色条带
        EndBeginDraw(2,1,ss:SamplerState.LinearWrap);
        TornadoEffect(new Vector4(1.0f),false,  rotTime,2.5f,new Vector2(0.4f),-0.5f,new Vector2(2.0f),1.4f,horizontalFadeRange:0.00f,verticalFadeRange:0.05f,
            maxInset:0.25f,bezierP0:new Vector2(-5,0),bezierP1:new Vector2(2.85f,0.45f),bezierP2:new Vector2(3f,0.95f),bezierP3:new Vector2(-10,1),
            dissolveTex:PerLinNoiseX,dissolveTexTiling:new Vector2(1),dissolveThreshold:GetDissolveAmount(0.3f),dissolveEdgeColor:new Vector4(8*light,2*light,0,1),dissolveEdgeWidth:0.02f,
            noiseTex:displaceNoi.Value,displaceNoiseScale:new Vector2(1.1f),displaceAmount:0.05f,displaceNoiseBias:0,displaceNoiseContrast:1,displaceNoiseSpeed:new Vector2(-0.3f));
        
        DrawInWorld(wind3.Value,Projectile.Center+new Vector2(-0,0),color: new Color(50,20,0,255),new Vector2(1.2f,2.3f)*0.6f);


        //黑色石头粒子
        TornadoEffect(new Vector4(1),false,  rotTime,2.5f,new Vector2(0.4f),-0.9f,new Vector2(1.2f),1.5f,horizontalFadeRange:0.00f,verticalFadeRange:0.05f,
            maxInset:0.25f,bezierP0:new Vector2(-2.5f,0),bezierP1:new Vector2(2.85f,0.45f),bezierP2:new Vector2(3f,0.95f),bezierP3:new Vector2(-10,1),
            dissolveTex:PerLinNoiseX,dissolveTexTiling:new Vector2(1),dissolveThreshold:GetDissolveAmount(0.6f),dissolveEdgeColor:new Vector4(4*light,2*light,0,1),dissolveEdgeWidth:0.02f,
            noiseTex:displaceNoi.Value,displaceNoiseScale:new Vector2(1.1f),displaceAmount:0.05f,displaceNoiseBias:0,displaceNoiseContrast:1,displaceNoiseSpeed:new Vector2(-0.3f));
        
        DrawInWorld(PerLinNoiseX,Projectile.Center+new Vector2(-0,0),color: new Color(20,10,0,255),new Vector2(2.0f,2.3f)*1.0f);
        
        
        //绘制底部最外图层气流圈
        DrawSlash(windSlash.Value, windSlashOffset, slashRotation, slashFadeTime, slashWidth, slashHeight, false);

        
        EndBeginDraw();
        EndBeginDraw();

        return;
    }

    // 气流圈的亮色 Additive 层与暗色层形状相同，开启 Bloom 时用 SphereDualLayerEffect 一次绘制
    private void DrawSlash(Texture2D windSlash, Vector2 offset, float slashRotation, float slashFadeTime, float slashWidth, float slashHeight, bool drawInner)
    {
        Vector2 position = Projectile.Center+offset+new Vector2(0,-slashHeight*150);
        Vector2 scale = new Vector2(1.5f,slashHeight)*tornadoScale;
        if (TornadoLayerRenderer.TargetClampsOutput(Main.graphics.GraphicsDevice))
        {
            //关闭 Bloom 时直接画在 8 位场景上，亮色层叠加后会被截断，合并后无法还原，仍逐层绘制
            EndBeginDraw(1,1,ss:SamplerState.LinearWrap);
            SphereEffect( worldRotation:new Vector3(0,0.2f,0),new Vector3(-slashRotation,0,0),clipImageY:true,sphereScale:new Vector2(0.4f,0.5f),sphereColor:new Vector4(2),
                dissolveTex:PerLinNoiseX,dissolveAmount:GetDissolveAmount(slashFadeTime-slashWidth),drawInner:drawInner);
            DrawInWorld(windSlash,position,color: new Color(255,50,11,255),scale);
            EndBeginDraw(2,1,ss:SamplerState.LinearWrap);
            SphereEffect( worldRotation:new Vector3(0,0.2f,0),new Vector3(-slashRotation,0,0),clipImageY:true,sphereScale:new Vector2(0.4f,0.5f),sphereColor:new Vector4(1),
                dissolveTex:PerLinNoiseX,dissolveAmount:GetDissolveAmount(slashFadeTime),drawInner:drawInner);
            DrawInWorld(windSlash,position,color: new Color(50,10,0,255),scale);
            return;
        }

        EndBeginDraw(0,1,ss:SamplerState.LinearWrap);
        SphereDualLayerEffect(new Color(255,50,11,255).ToVector4()*2, GetDissolveAmount(slashFadeTime-slashWidth),
            new Color(50,10,0,255).ToVector4(), GetDissolveAmount(slashFadeTime),
            worldRotation:new Vector3(0,0.2f,0), localRotation:new Vector3(-slashRotation,0,0), drawInner:drawInner,
            clipImageY:true, sphereScale:new Vector2(0.4f,0.5f), dissolveTex:PerLinNoiseX);
        DrawInWorld(windSlash,position,color: Color.White,scale);
    }

    float GetDissolveAmount(float currentDissolve = 0)
    {
        return MathHelper.Lerp(currentDissolve, 1, totalFadeTime);
    }

    public override void OnKill(int timeLeft)
    {
        base.OnKill(timeLeft);
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers,
        List<int> overWiresUI)
    {
        //KL 每帧在 CacheProjDraws 开始时清空请求，随后才绘制 BehindNPCsAndTiles 层；
        //DrawBehind 在这两者之间调用，在这里提交才能让背面气流圈画到图块后面。
        LayerDrawRequestSystem.RequestBloom("Elaina:FireTornado", LayerDrawRequestSystem.DrawTargetLayer.BehindNPCsAndTiles,
            LayerDrawRequestSystem.DrawTiming.After, DrawVisual);
        LayerDrawRequestSystem.RequestBloom("Elaina:FireTornado", LayerDrawRequestSystem.DrawTargetLayer.Projectiles,
            LayerDrawRequestSystem.DrawTiming.After, DrawVisual);
        base.DrawBehind(index, behindNPCsAndTiles, behindNPCs, behindProjectiles, overPlayers, overWiresUI);
    }
}
