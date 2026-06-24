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

    public override bool PreDraw(ref Color lightColor)
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
            //底层环绕slash
            EndBeginDraw(1,1,ss:SamplerState.LinearWrap);
            SphereEffect( worldRotation:new Vector3(0,0.2f,0),new Vector3(-slashRotation,0,0),clipImageY:true,sphereScale:new Vector2(0.4f,0.5f),sphereColor:new Vector4(2),
                dissolveTex:PerLinNoiseX,dissolveAmount:GetDissolveAmount(slashFadeTime-slashWidth),drawInner:true);
            DrawInWorld(windSlash.Value,Projectile.Center+windSlashOffset+new Vector2(0,-slashHeight*150),color: new Color(255,50,11,255),new Vector2(1.5f,slashHeight)*tornadoScale);
            EndBeginDraw(2,1,ss:SamplerState.LinearWrap);
            SphereEffect( worldRotation:new Vector3(0,0.2f,0),new Vector3(-slashRotation,0,0),clipImageY:true,sphereScale:new Vector2(0.4f,0.5f),sphereColor:new Vector4(1),
                dissolveTex:PerLinNoiseX,dissolveAmount:GetDissolveAmount(slashFadeTime),drawInner:true);
            DrawInWorld(windSlash.Value,Projectile.Center+windSlashOffset+new Vector2(0,-slashHeight*150),color: new Color(50,10,0,255),new Vector2(1.5f,slashHeight)*tornadoScale);
            
            EndBeginDraw();
            return false;
        }


        //绘制底层垫色，防止画面过曝。
        EndBeginDraw(2,1,ss:SamplerState.LinearWrap);
        TornadoEffect(new Vector4(1.0f),true,  rotTime,2.5f,new Vector2(0.4f),-1.4f,new Vector2(1),1.0f,horizontalFadeRange:0.02f,verticalFadeRange:0.1f,
            maxInset:0.25f,bezierP0:new Vector2(-5,0),bezierP1:new Vector2(2.85f,0.45f),bezierP2:new Vector2(3f,0.95f),bezierP3:new Vector2(-8,1),
            noiseTex:displaceNoi.Value,displaceNoiseScale:new Vector2(1.1f),displaceAmount:0.05f,displaceNoiseBias:0,displaceNoiseContrast:1,displaceNoiseSpeed:new Vector2(-0.3f),
            dissolveTex:PerLinNoiseX,dissolveThreshold:GetDissolveAmount(),dissolveEdgeWidth:0.00f);
        DrawInWorld(waterNoi.Value,Projectile.Center+new Vector2(-0,0),color: new Color(255,100,50,255)*(1-totalFadeTime),new Vector2(1.2f,2.3f)*2.0f);
        
        TornadoEffect(new Vector4(1.0f),false,  rotTime,2.5f,new Vector2(0.4f),-1.4f,new Vector2(1),1.0f,horizontalFadeRange:0.02f,verticalFadeRange:0.1f,
            maxInset:0.25f,bezierP0:new Vector2(-5,0),bezierP1:new Vector2(2.85f,0.45f),bezierP2:new Vector2(3f,0.95f),bezierP3:new Vector2(-8,1),
            noiseTex:displaceNoi.Value,displaceNoiseScale:new Vector2(1.1f),displaceAmount:0.05f,displaceNoiseBias:0,displaceNoiseContrast:1,displaceNoiseSpeed:new Vector2(-0.3f),
            dissolveTex:PerLinNoiseX,dissolveThreshold:GetDissolveAmount(),dissolveEdgeWidth:0.00f);
        DrawInWorld(wind1.Value,Projectile.Center+new Vector2(-0,0),color: new Color(255,100,50,255)*(1-totalFadeTime),new Vector2(1.2f,2.3f)*2.0f);
        
        TornadoEffect(new Vector4(1.2f),false,  rotTime,2.5f,new Vector2(0.4f),-1.4f,new Vector2(1),2.0f,horizontalFadeRange:0.02f,verticalFadeRange:0.1f,
            maxInset:0.25f,bezierP0:new Vector2(-5,0),bezierP1:new Vector2(2.85f,0.45f),bezierP2:new Vector2(3f,0.95f),bezierP3:new Vector2(-8,1),
            noiseTex:displaceNoi.Value,displaceNoiseScale:new Vector2(1.1f),displaceAmount:0.05f,displaceNoiseBias:0,displaceNoiseContrast:1,displaceNoiseSpeed:new Vector2(-0.3f),
            dissolveTex:PerLinNoiseX,dissolveThreshold:GetDissolveAmount(),dissolveEdgeWidth:0.00f);
        DrawInWorld(waterNoi.Value,Projectile.Center+new Vector2(-0,0),color: new Color(255,70,20,255)*(1-totalFadeTime),new Vector2(1.3f,2.3f)*2.0f);
        
        float light = 1.0f;
        if (Projectile.timeLeft < 30) light = MathHelper.Lerp(0.0f, 1.0f, Projectile.timeLeft / 30f);
        //绘制高亮核心
        //高亮残片
        EndBeginDraw(1,1,ss:SamplerState.LinearWrap);
        TornadoEffect(new Vector4(4.5f*light),true, rotTime,2.5f,new Vector2(0.4f),-0.4f,new Vector2(1),1.5f,horizontalFadeRange:0.02f,verticalFadeRange:0.02f,
            maxInset:0.25f,bezierP0:new Vector2(-5,0),bezierP1:new Vector2(1.85f,0.45f),bezierP2:new Vector2(1f,0.95f),bezierP3:new Vector2(-5,1),
            dissolveTex:PerLinNoiseX,dissolveTexTiling:new Vector2(0.5f),dissolveThreshold:GetDissolveAmount(0.5f),dissolveEdgeWidth:0.00f,
            noiseTex:displaceNoi.Value,displaceNoiseScale:new Vector2(1.1f),displaceAmount:0.05f,displaceNoiseBias:0,displaceNoiseContrast:1,displaceNoiseSpeed:new Vector2(-0.3f));
        
        DrawInWorld(cellnoise.Value,Projectile.Center+new Vector2(-0,0),color: new Color(255,70,11,255),new Vector2(0.4f,1.2f)*1.6f);
        //高亮旋风
        TornadoEffect(new Vector4(1.5f*light),false,  rotTime,2.5f,new Vector2(0.4f),-1.4f,new Vector2(1),1.0f,horizontalFadeRange:0.02f,verticalFadeRange:0.1f,
            maxInset:0.25f,bezierP0:new Vector2(-5,0),bezierP1:new Vector2(2.85f,0.45f),bezierP2:new Vector2(3f,0.95f),bezierP3:new Vector2(-8,1),
            noiseTex:displaceNoi.Value,displaceNoiseScale:new Vector2(1.1f),displaceAmount:0.05f,displaceNoiseBias:0,displaceNoiseContrast:1,displaceNoiseSpeed:new Vector2(-0.3f),
            dissolveTex:PerLinNoiseX,dissolveThreshold:GetDissolveAmount(),dissolveEdgeWidth:0.00f);
        DrawInWorld(wind1.Value,Projectile.Center+new Vector2(-0,0),color: new Color(255,70,20,255),new Vector2(1.3f,2.3f)*2.0f);
        


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
        EndBeginDraw(1,1,ss:SamplerState.LinearWrap);
        SphereEffect( worldRotation:new Vector3(0,0.2f,0),new Vector3(-slashRotation,0,0),clipImageY:true,sphereScale:new Vector2(0.4f,0.5f),sphereColor:new Vector4(2),
            dissolveTex:PerLinNoiseX,dissolveAmount:GetDissolveAmount(slashFadeTime-slashWidth));
        DrawInWorld(windSlash.Value,Projectile.Center+windSlashOffset+new Vector2(0,-slashHeight*150),color: new Color(255,50,11,255),new Vector2(1.5f,slashHeight)*tornadoScale);
        EndBeginDraw(2,1,ss:SamplerState.LinearWrap);
        SphereEffect( worldRotation:new Vector3(0,0.2f,0),new Vector3(-slashRotation,0,0),clipImageY:true,sphereScale:new Vector2(0.4f,0.5f),sphereColor:new Vector4(1),
            dissolveTex:PerLinNoiseX,dissolveAmount:GetDissolveAmount(slashFadeTime));
        DrawInWorld(windSlash.Value,Projectile.Center+windSlashOffset+new Vector2(0,-slashHeight*150),color: new Color(50,10,0,255),new Vector2(1.5f,slashHeight)*tornadoScale);

        
        EndBeginDraw();
        EndBeginDraw();

        return base.PreDraw(ref lightColor);
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
        behindNPCsAndTiles.Add(index);
        base.DrawBehind(index, behindNPCsAndTiles, behindNPCs, behindProjectiles, overPlayers, overWiresUI);
    }
}