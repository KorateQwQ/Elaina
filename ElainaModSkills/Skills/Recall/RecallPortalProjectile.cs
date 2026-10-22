using System;

namespace 伊蕾娜.ElainaModSkills.Skills.Recall;

/// <summary>
/// 魔法阵弹幕：ai[0] 配对编号，ai[1] 存在帧数，ai[2] 起点 0 / 出口 1。
/// 两端由施法客户端同时生成、一起描绘；起点画完（年龄到 DelayTicks）的那一帧结算群体传送。
/// </summary>
public sealed class RecallPortalProjectile : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_0";
    public int PairId => (int)Projectile.ai[0];
    public int Side => (int)Projectile.ai[2] & 1;
    public bool Ready => Projectile.ai[1] >= RecallPortalSystem.DelayTicks;
    /// <summary>最近一次向上爆发光效开始时的年龄：画完时一次，之后每次有人经这对法阵传送再放一次。仅本地视觉。</summary>
    public float BurstStart => Projectile.localAI[1] > 0 ? Projectile.localAI[1] : RecallPortalSystem.DelayTicks;
    public void Flash() => Projectile.localAI[1] = Math.Max(Projectile.ai[1], RecallPortalSystem.DelayTicks);

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.netImportant = true;
        Projectile.timeLeft = RecallPortalSystem.DelayTicks + RecallPortalSystem.ResidueTicks + 5;
    }

    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;

    public override void AI()
    {
        Projectile.ai[1]++;
        // 寿命以生成端同步的年龄为准，晚到的同步不会让法阵多留。
        Projectile.timeLeft = Math.Max(1,
            RecallPortalSystem.DelayTicks + RecallPortalSystem.ResidueTicks - (int)Projectile.ai[1] + 1);
        // localAI 标记只结算一次，生成端重发同步让年龄跳过整数帧时也不会漏掉。
        if (Projectile.ai[1] >= RecallPortalSystem.DelayTicks && Projectile.localAI[0] == 0)
        {
            Projectile.localAI[0] = 1;
            if (Side == 0) RecallPortalSystem.Arrive(this);
        }
        // 施法者离开，或生成端发现另一端已不存在时，关闭这一端。
        if (!Main.player[Projectile.owner].active ||
            Projectile.owner == Main.myPlayer && RecallPortalSystem.FindPortal(Projectile.owner, PairId, 1 - Side) < 0)
            Projectile.Kill();
    }

    // 与占位图的椭圆足迹一致。真实传送范围另外按玩家脚底到阵心的距离校验。
    public bool Hovered(Vector2 world) => RecallPortalDrawing.Contains(world - Projectile.Center, RecallPortalSystem.AreaRadius);

    public override bool PreDraw(ref Color lightColor)
    {
        RecallPortalVisual.Request(Projectile.Center - Main.screenPosition, RecallPortalSystem.AreaRadius, Projectile.ai[1],
            RecallPortalSystem.DelayTicks, RecallPortalSystem.ResidueTicks, Side == 1, Hovered(Main.MouseWorld), BurstStart);
        return false;
    }
}
