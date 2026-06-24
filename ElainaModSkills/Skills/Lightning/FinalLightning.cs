using KL.Drawing;
using KL.Dusts;
using KL.Dusts.Lightning;


namespace 伊蕾娜.ElainaModSkills.Skills.Lightning;

public class FinalLightning : ElainaBasicProjectile
{
    private static Texture2D tex;

    private float mainCoreStrength = 0.1f;
    private int mainCoreFlashFrame = 3;
    float mainCoreStartTime = 0;
    private int hideTime = 0;
    public override void SetDefaults()
    {
        Projectile.timeLeft = 25;
        Projectile.tileCollide = false;
        tex ??=ModContent.Request<Texture2D>("KL/Effects/Tex/Lightning", AssetRequestMode.ImmediateLoad).Value;
        mainCoreStrength = Main.rand.NextFloat(0.05f, 0.1f);
        mainCoreFlashFrame = Main.rand.Next(3,5);
        mainCoreStartTime = Main.rand.NextFloat(0,100);
        base.SetDefaults(); 
    }

    public override void OnSpawn_AllClient()
    {
        Color blue = new Color(71, 176, 255, 255);

        /*KLBasicDust.SpawnDustsCircle(Projectile.Center, ModContent.DustType<ShockDust>(), 30, new Vector2(0,-15), 
            1.68f,15,blue,new Vector2(0.2f,0.05f)*1,0,0,new Vector2(0.1f,0.2f));*/
        
        KLBasicDust.SpawnDustsCircle(Projectile.Center, ModContent.DustType<LightningDust>(), 10, new Vector2(0,-15), 
            2.8f,20, new Color(151, 206, 255, 255),new Vector2(2,1.5f),
            0,50, scaleOffset: new Vector2(0.8f,0.1f),5);
        
        KLBasicDust.SpawnDustsCircle(Projectile.Center, ModContent.DustType<LightningDust3>(), 3, new Vector2(0,-15), 
            1.2f,15, new Color(151, 206, 255, 255),new Vector2(2,1.5f),
            0,200, scaleOffset: new Vector2(0.8f,0.1f),5);


        base.OnSpawn_AllClient();
    }

    public override void AI()
    {
        if (Projectile.timeLeft % mainCoreFlashFrame == 0)
        {
            hideTime = Main.rand.Next(2, 3);
        }
        else
        {
            hideTime--;
        }
        base.AI();
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture =ModContent.Request<Texture2D>("KL/Effects/Tex/Lightning/LightningDustTex", AssetRequestMode.ImmediateLoad).Value;
        int frame = (int)MathHelper.Lerp(10, 0, Projectile.timeLeft / 30f);
        Color blue = new Color(71, 176, 255, 255);
        
        EndBeginDraw(1,1);
        ReColorEffect(new Vector4(3)*Projectile.timeLeft/30f);
        DrawInWorld(new DrawHelper.TextureInfo(texture,frame,1,10),Projectile.Center+new Vector2(0,-70),blue,new Vector2(0.8f));

        if (hideTime <= 0||Projectile.timeLeft<=16)
        {
            float EdgeStrength = MathHelper.SmoothStep( 5f, 0f,Projectile.timeLeft / 25f);
            float EdgeStrength2 = MathHelper.SmoothStep( 10f, 0.8f,Projectile.timeLeft / 25f);

            float coreExtraStrength =0;
            float coreExtraFrequency = 0;
            if (Projectile.timeLeft <= 12)
            {
                coreExtraStrength = MathHelper.SmoothStep( 0.08f, 0f,Projectile.timeLeft / 12f);
                coreExtraFrequency = MathHelper.SmoothStep( 0.15f, 0f,Projectile.timeLeft / 12f);
            }

            LightningEffect(mainCoreStrength+coreExtraStrength,0.2f+coreExtraFrequency,coreTime:mainCoreStartTime+Projectile.timeLeft/90f,
                bloomColor:Vector4.One*2.8f,edgeStrength:EdgeStrength);
            DrawInWorld(tex,Projectile.Center+new Vector2(0,-500),new Color(71,176,255),new Vector2(20.5f,0.8f),3.14f/2);
            
            LightningEffect(mainCoreStrength+coreExtraStrength+0.2f,0.2f,coreTime:mainCoreStartTime-Projectile.timeLeft/30f+10.8f,
                bloomColor:Vector4.One*3.2f,edgeStrength:EdgeStrength2,unlockEndLightning:true);
            DrawInWorld(tex,Projectile.Center+new Vector2(0,-500),new Color(71,176,255),new Vector2(20.5f,0.15f),-3.14f/2);
            
            LightningEffect(mainCoreStrength+coreExtraStrength+0.5f,0.5f,coreTime:mainCoreStartTime-Projectile.timeLeft/60f-6.2f,
                bloomColor:Vector4.One*3.2f,edgeStrength:EdgeStrength2,unlockEndLightning:true);
            DrawInWorld(tex,Projectile.Center+new Vector2(0,-500),new Color(71,176,255),new Vector2(20.5f,0.15f),-3.14f/2);
        }


        EndBeginDraw();
        return base.PreDraw(ref lightColor);
    }

    public override void OnKill(int timeLeft)
    {
        base.OnKill(timeLeft);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        base.OnHitNPC(target, hit, damageDone);
    }

    public override bool ShouldUpdatePosition()
    {
        return false;
    }
}