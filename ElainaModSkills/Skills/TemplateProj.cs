using System.Collections.Generic;

namespace 伊蕾娜.ElainaModSkills.Projectiles;

public abstract class TemplateProj : KLProjectile
{
    
    public override void SetDefaults()
    {
        base.SetDefaults();
    }

    public override void OnSpawn_AllClient()
    {
        base.OnSpawn_AllClient();
    }
    
    public override void AI()
    {
        base.AI();
    }

    public override bool PreDraw(ref Color lightColor)
    {
        return base.PreDraw(ref lightColor);
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