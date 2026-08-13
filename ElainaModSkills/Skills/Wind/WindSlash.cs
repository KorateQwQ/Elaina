using System;
using Terraria.GameContent;

namespace 伊蕾娜.ElainaModSkills.Skills.Wind;

public class WindSlash : ElainaBasicProjectile
{
    
    private Vector2[] windPoints;
    private Vector2[] windPoints2;
    private Vector2[] windPoints3;

    public override void SetDefaults()
    {
        base.SetDefaults();
    }

    public override void OnSpawn_AllClient()
    {
        Vector2 move = new Vector2(1, 0);

        windPoints = QuickConePoints( move*50,-move*300f ,300, 200, 200,0.1f);
        windPoints2 = QuickConePoints( move*50,-move*300f ,300, 300, 200,0.1f);
        windPoints3 = QuickConePoints( move*50,-move*300f ,300, 300, 200,0.1f);
        base.OnSpawn_AllClient();
    }
    
    public override void AI()
    {
        Projectile.rotation = Projectile.velocity.ToRotation();
        Vector2 move = new Vector2(1, 0);

        windPoints = QuickConePoints( move*50,-move*300f ,300, 200, 200,0.1f);
        windPoints2 = QuickConePoints( move*50,-move*300f ,300, 300, 200,0.1f);
        windPoints3 = QuickConePoints( move*50,-move*300f ,300, 300, 200,0.1f);

        base.AI();
    }

    public override bool PreDraw(ref Color lightColor)
    {
        DrawWind();
        EndBeginDraw();
        
        return base.PreDraw(ref lightColor);
    }

    void DrawWind()
    {

        Texture2D top = ModContent.Request<Texture2D>("KL/Effects/Tex/lightMask", AssetRequestMode.ImmediateLoad).Value;
        Texture2D top2 = ModContent.Request<Texture2D>("KL/Effects/Tex/Wind/3eb465f7ddb4bc30589830a7750b398e", AssetRequestMode.ImmediateLoad).Value;

        Texture2D noise = ModContent.Request<Texture2D>("KL/Effects/Tex/Noise/4", AssetRequestMode.ImmediateLoad).Value;
        Texture2D noise2 = ModContent.Request<Texture2D>("KL/Effects/Tex/voronoiOrigin", AssetRequestMode.ImmediateLoad).Value;

        Texture2D wind = ModContent.Request<Texture2D>("KL/Effects/Tex/Wind/wind3", AssetRequestMode.ImmediateLoad).Value;
        Texture2D wind2 = ModContent.Request<Texture2D>("KL/Effects/Tex/Wind/windNoi", AssetRequestMode.ImmediateLoad).Value;

        Vector2 move = new Vector2(1, 0);
        windPoints = QuickConePoints( move*50,-move*150f ,300, 200, 200,0.1f);
        windPoints2 = QuickConePoints( move*50,-move*200f ,300, 300, 300,0.1f);
        windPoints3 = QuickConePoints( move*50,-move*400f ,300, 150, 150,0.1f);

        
        Vector2 uTime = new Vector2((float)(Main.timeForVisualEffects % 1200) / 165f, 0);

        Color pink = new Color(255, 160, 239,155);
        Color fire = new Color(255, 120, 30, 255);
        
        VertexDrawEffect(top, windPoints2, pink, pink*0,
            startAlpha: 2.2f, endAlpha: 1, blendState: 1, drawTimes: 1,
            uTime: new Vector2(0),
            attachPoint: Projectile.Center + move.RotatedBy(Projectile.rotation) * (1),
            attachRotation: Projectile.rotation,
            imageScale: new Vector2(1, 1),
            useRforAlpha: false, debugPoint: false);
            
        VertexDrawEffect(wind,windPoints,pink, pink,startAlpha:2.0f, endAlpha:0.0f,blendState:1,drawTimes:1,
            uTime:new Vector2((float)(Main.timeForVisualEffects%1200)/25f,0),
            attachPoint:Projectile.Center-move.RotatedBy(Projectile.rotation)*(1),attachRotation:Projectile.rotation,
            imageScale:new Vector2(2,1),
            useRforAlpha:false,debugPoint:false);
        
        VertexDrawEffect(wind2,windPoints3,pink, pink,startAlpha:3.0f, endAlpha:0f,blendState:1,drawTimes:1,
            uTime:new Vector2((float)(Main.timeForVisualEffects%1200)/25f,0),
            attachPoint:Projectile.Center-move.RotatedBy(Projectile.rotation)*(1),attachRotation:Projectile.rotation,
            imageScale:new Vector2(2,1),
            useRforAlpha:false,debugPoint:false);
    }
    
    void DrawWind2()
    {
        Texture2D top = ModContent.Request<Texture2D>("KL/Effects/Tex/lightMask", AssetRequestMode.ImmediateLoad).Value;
        Texture2D top2 = ModContent.Request<Texture2D>("KL/Effects/Tex/Wind/SemiCircle2", AssetRequestMode.ImmediateLoad).Value;
        Texture2D top3 = ModContent.Request<Texture2D>("KL/Effects/Tex/Wind/SemiCircle", AssetRequestMode.ImmediateLoad).Value;

        Texture2D noise = ModContent.Request<Texture2D>("KL/Effects/Tex/Noise/4", AssetRequestMode.ImmediateLoad).Value;
        Texture2D noise2 = ModContent.Request<Texture2D>("KL/Effects/Tex/voronoi", AssetRequestMode.ImmediateLoad).Value;

        Texture2D wind = ModContent.Request<Texture2D>("KL/Effects/Tex/Wind/wind4", AssetRequestMode.ImmediateLoad).Value;
        Texture2D wind2 = ModContent.Request<Texture2D>("KL/Effects/Tex/Wind/wind3", AssetRequestMode.ImmediateLoad).Value;
        
        Texture2D trail = ModContent.Request<Texture2D>("KL/Effects/Tex/Trail/165612odcu1dschrvcz6yh", AssetRequestMode.ImmediateLoad).Value;

        Vector2 move = new Vector2(1, 0).RotatedBy(Projectile.rotation);
        
        Vector2 uTime = new Vector2((float)(Main.timeForVisualEffects % 1200) / 185f, 0);

        Color pink = new Color(255, 255, 255,255);
        Color fire = new Color(255, 120, 30, 255);
        
        Vector4 fireColor = new Vector4(1f,0.4f,0.1f,1f)*2.5f;
        
        /*VertexDrawEffect(trail,windPoints3,pink, pink,startAlpha:2.0f, endAlpha:0f,blendState:1,drawTimes:1,
            uTime:new Vector2(-(float)(Main.timeForVisualEffects%1200)/15f,0),
            attachPoint:Projectile.Center-move.RotatedBy(Projectile.rotation)*(1),attachRotation:Projectile.rotation+MathF.PI,
            imageScale:new Vector2(2f,1),
            useRforAlpha:false,debugPoint:false);*/
        
        /*VertexDrawEffect(wind,windPoints3,fire, fire,startAlpha:2.0f, endAlpha:0f,blendState:1,drawTimes:1,
            uTime:new Vector2(-(float)(Main.timeForVisualEffects%1200)/25f,0),
            attachPoint:Projectile.Center-move.RotatedBy(Projectile.rotation)*(1),attachRotation:Projectile.rotation+MathF.PI,
            imageScale:new Vector2(1f,1),
            useRforAlpha:false,debugPoint:false);*/
        
        /*VertexDrawEffect(noise2,windPoints3,fire, fire,startAlpha:2.0f, endAlpha:0f,blendState:1,drawTimes:1,
            uTime:new Vector2(-(float)(Main.timeForVisualEffects%1200)/45f,0),
            attachPoint:Projectile.Center-move.RotatedBy(Projectile.rotation)*(1),attachRotation:Projectile.rotation+MathF.PI,
            imageScale:new Vector2(1f,0.5f),
            useRforAlpha:false,debugPoint:false);*/

    }
    void DrawWind3()
    {
        Texture2D wind = ModContent.Request<Texture2D>("KL/Effects/Tex/Wind/wind4", AssetRequestMode.ImmediateLoad).Value;
        Texture2D circle = ModContent.Request<Texture2D>("KL/Effects/Tex/Wind/SemiCircle", AssetRequestMode.ImmediateLoad).Value;
        Texture2D noise = ModContent.Request<Texture2D>("KL/Effects/Tex/voronoi", AssetRequestMode.ImmediateLoad).Value;

        Vector2 move = new Vector2(1, 0).RotatedBy(Projectile.rotation);

        Color cyan = new Color(120, 255, 220, 255);

        VertexDrawEffect(circle, windPoints, cyan, cyan * 0,
            startAlpha: 2.2f, endAlpha: 0.8f, blendState: 1, drawTimes: 1,
            uTime: new Vector2((float)(Main.timeForVisualEffects % 1200) / 28f, 0),
            attachPoint: Projectile.Center + move * 10, attachRotation: Projectile.rotation + 0.4f,
            imageScale: new Vector2(1.3f, 0.7f),
            useRforAlpha: false, debugPoint: false);

        VertexDrawEffect(wind, windPoints2, cyan, cyan,
            startAlpha: 1.5f, endAlpha: 0f, blendState: 1, drawTimes: 1,
            uTime: new Vector2(-(float)(Main.timeForVisualEffects % 1200) / 22f, 0),
            attachPoint: Projectile.Center - move * 15, attachRotation: Projectile.rotation - 0.4f,
            imageScale: new Vector2(1.8f, 1.5f),
            useRforAlpha: false, debugPoint: false);

        VertexDrawEffect(noise, windPoints3, cyan, cyan * 0.3f,
            startAlpha: 1.2f, endAlpha: 0f, blendState: 1, drawTimes: 1,
            uTime: new Vector2((float)(Main.timeForVisualEffects % 1200) / 55f, 0),
            attachPoint: Projectile.Center + move * 5, attachRotation: Projectile.rotation + MathF.PI / 3,
            imageScale: new Vector2(0.9f, 1.2f),
            useRforAlpha: false, debugPoint: false);
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers,
        List<int> overWiresUI)
    {
        base.DrawBehind(index, behindNPCsAndTiles, behindNPCs, behindProjectiles, overPlayers, overWiresUI);
    }
    
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        base.OnHitNPC(target, hit, damageDone);
    }

    public override void OnKill(int timeLeft)
    {
        base.OnKill(timeLeft);
    }
    
    public override bool ShouldUpdatePosition()
    {
        return base.ShouldUpdatePosition();
    }
    
}