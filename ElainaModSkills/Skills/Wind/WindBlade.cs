using KL.Extensions;
using KL.Utils;

namespace 伊蕾娜.ElainaModSkills.Skills.Wind;

public class WindBlade : ElainaBasicProjectile
{
    public override void SetStaticDefaults()
    {

        base.SetStaticDefaults();
    }

    public override void SetDefaults()
    {
        Projectile.timeLeft = 600;
        Projectile.friendly = true;
        Projectile.damage = 10;
        Projectile.width = Projectile.height = 150;
        Projectile.penetrate = -1;
        Projectile.tileCollide = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 60;
        base.SetDefaults();
    }

    public override void AI()
    {
        if (Projectile.velocity.Length() > 0) Projectile.rotation = Projectile.velocity.ToRotation();
        if(Projectile.timeLeft<=30)
        {
            Projectile.velocity *= 0.9f;
            Projectile.tileCollide = false;
        }
        base.AI();
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D wind = ModContent.Request<Texture2D>("KL/Effects/Tex/Wind/SemiCircle2", AssetRequestMode.ImmediateLoad).Value;
        Texture2D waterNoise  = ModContent.Request<Texture2D>("KL/Effects/Tex/Wind/Eff_Noise_57", AssetRequestMode.ImmediateLoad).Value;

        float progress = 0.52f;
        if (Projectile.timeLeft < 30) progress += KLMathF.ClampLerp(0, 0.48f, (30 - Projectile.timeLeft) / 30f);
        EndBeginDraw(0,1);
        RadialDissolve(new Vector4(new Vector3(1f,0.65f,0.9f)*1.5f,1),waterNoise,
            progress, new Vector2((float)Projectile.timeLeft%120/60f,0),new Vector2(0.7f,2f),0.3f
            ,0.3f,8f,sweepDirection:new Vector2(1,0),
            distortStrength:0.01f,distortTime:new Vector2((float)Projectile.timeLeft%240/240f),distortScale:new Vector2(0.3f));
        
        DrawInWorld(wind,Projectile.Center,color:Color.White,new Vector2(1f,1.5f),Projectile.rotation);
        
        EndBeginDraw();

        return base.PreDraw(ref lightColor);
    }

    public void FadeOut()
    {
        if (Projectile.timeLeft > 30)
        {
            Projectile.timeLeft = 30;
        }
    }

    public override bool? CanHitNPC(NPC target)
    {
        if (Projectile.timeLeft <= 30) return false;
        return base.CanHitNPC(target);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 startPosition = Projectile.Center+ new Vector2(1,0).RotatedBy(Projectile.rotation)*150;
        Vector2 endPosition = Projectile.Center-new Vector2(1,0).RotatedBy(Projectile.rotation)*10;
        return AABBvLineCollision(targetHitbox, startPosition, endPosition, 160);
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Projectile.velocity = oldVelocity;
        if (Projectile.timeLeft > 30)RPC("FadeOut",KLNetModule.NetSendType.ClientToAll);

        return false;
        return base.OnTileCollide(oldVelocity);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        base.OnHitNPC(target, hit, damageDone);
    }
}