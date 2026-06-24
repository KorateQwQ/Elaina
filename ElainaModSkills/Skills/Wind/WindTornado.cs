namespace 伊蕾娜.ElainaModSkills.Skills.Wind;

/// <summary>
/// 蓄力1秒后召唤一个风球，风球会朝着初始发射方向移动，并追击最近的敌人，优先锁定boss。
/// </summary>
public class WindTornado : ElainaBasicProjectile
{
    enum State
    {
        Charge,
        Move,
        End
    }
    State state = State.Charge;

    private int startTime = 300;
    private readonly List<WindTornadoBlade> bladePool = new List<WindTornadoBlade>();
    private readonly List<WindTornadoBlade> activeBlades = new List<WindTornadoBlade>();
    private const int MaxBlades = 32;
    public override void SetDefaults()
    {
        Projectile.timeLeft = startTime;
        base.SetDefaults();
    }

    public override void AI()
    {
        if (Projectile.timeLeft > startTime - 60)
        {
            state = State.Charge;
        }
        else if (Projectile.timeLeft <= 60)
        {
            state = State.End;
        }
        else
        {
            state = State.Move;
        }

        if (Main.GameUpdateCount % 8 == 0 && Projectile.timeLeft > 60)
        {
            int count = Main.rand.Next(1, 2);
            for (int i = 0; i < count; i++)
            {
                SpawnBlade();
            }
        }

        UpdateBlades();
        
        base.AI();
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D wind = ModContent.Request<Texture2D>("KL/Effects/Tex/Wind/wind2").Value;
        Texture2D wind2 = ModContent.Request<Texture2D>("KL/Effects/Tex/Wind/wind3").Value;
        Texture2D bladeTex = ModContent.Request<Texture2D>("KL/Effects/Tex/Wind/wind1").Value;

        float xTime = (float)(Main.timeForVisualEffects % 1200) / 5;
        Vector3 rotation = new Vector3(xTime, 0, 0);
        Vector2 sphereImageScale = new Vector2(1,1f);
        
        Vector2 scale = new Vector2(1)*0.4f;
        
        EndBeginDraw(1, 1);
        DrawBlades(bladeTex);
        
        EndBeginDraw(2,1);
        //SphereEffect(rotation:rotation,showInside:true,imageScale:sphereImageScale);
        DrawInWorld(wind,Projectile.Center,NormalMagicColor*0.7f,scale,0.0f);

        EndBeginDraw(1,1);
        //SphereEffect(rotation:rotation,imageScale:sphereImageScale);
        DrawInWorld(wind,Projectile.Center,NormalMagicColor,scale,0.0f);


        
        EndBeginDraw();
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

    private void SpawnBlade()
    {
        if (activeBlades.Count >= MaxBlades)
        {
            return;
        }

        WindTornadoBlade blade = bladePool.Count > 0 ? bladePool[^1] : new WindTornadoBlade();
        if (bladePool.Count > 0)
        {
            bladePool.RemoveAt(bladePool.Count - 1);
        }

        blade.Reset(Main.rand.NextVector2Circular(70, 70));
        activeBlades.Add(blade);
    }

    private void UpdateBlades()
    {
        for (int i = activeBlades.Count - 1; i >= 0; i--)
        {
            WindTornadoBlade blade = activeBlades[i];
            blade.Update(Projectile.Center);
            if (!blade.IsAlive)
            {
                activeBlades.RemoveAt(i);
                bladePool.Add(blade);
            }
        }
    }

    private void DrawBlades(Texture2D bladeTex)
    {
        for (int i = 0; i < activeBlades.Count; i++)
        {
            activeBlades[i].Draw(this, bladeTex, NormalMagicColor);
        }
    }
}