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
    // 排布后的实际阵心，绘制、右键使用范围和另一端的传送目标共用。
    internal Vector2 DisplayCenter;

    public override void SetStaticDefaults() =>
        // 错位后的图形可能在屏幕内，原始阵心却在屏幕外，不能按原始弹幕位置提前剔除。
        Terraria.ID.ProjectileID.Sets.DrawScreenCheckFluff[Type] =
            (int)RecallPortalLayout.HorizontalSpacing * Main.maxPlayers * 2;
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

    // 悬停匹配显示阵心的椭圆；右键使用范围按玩家脚底到同一阵心的距离校验。
    public bool Hovered(Vector2 world) => RecallPortalDrawing.Contains(world - DisplayCenter, RecallPortalSystem.AreaRadius);

    public override bool PreDraw(ref Color lightColor)
    {
        RecallPortalPresentation.Refresh();
        // 原版剔除已放宽；这里按实际显示位置剔除，避免把远端的法阵也提交给 bloom。
        Matrix view = Main.GameViewMatrix.TransformationMatrix;
        Vector2 screen = Vector2.Transform(DisplayCenter - Main.screenPosition, view);
        Vector2 padding = Vector2.TransformNormal(new Vector2(RecallPortalSystem.AreaRadius + 96), view);
        if (screen.X + padding.X < 0 || screen.X - padding.X > Main.screenWidth ||
            screen.Y + padding.Y < 0 || screen.Y - padding.Y > Main.screenHeight) return false;
        RecallPortalVisual.Request(DisplayCenter - Main.screenPosition, RecallPortalSystem.AreaRadius, Projectile.ai[1],
            RecallPortalSystem.DelayTicks, RecallPortalSystem.ResidueTicks, Side == 1, Hovered(Main.MouseWorld), BurstStart);
        return false;
    }
}
