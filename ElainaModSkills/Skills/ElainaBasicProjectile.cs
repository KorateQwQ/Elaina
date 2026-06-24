

namespace 伊蕾娜.ElainaModSkills.Skills;

public abstract class ElainaBasicProjectile : KLProjectile
{
    public Color NormalMagicColor = new Color(255, 160, 239);
    
    public override string Texture => "伊蕾娜/Effects/Tex/background";
    public override void SetDefaults()
    {
        Projectile.DamageType = DamageClass.Magic;

        base.SetDefaults();
    }
    
    public override void OnSpawn_AllClient()
    {
        base.OnSpawn_AllClient();
    }

    public override bool PreAI()
    {
        return base.PreAI();
    }

    public override void AI()
    {
        base.AI();
    }

    public override void PostAI()
    {
        base.PostAI();
    }
}