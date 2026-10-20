using System;
using Terraria.GameContent;

namespace 伊蕾娜.ElainaModSkills.Skills.Recall;

/// <summary>魔法阵弹幕：ai[0] 配对编号，ai[1] 存在帧数，ai[2] 起点0/出生地1。</summary>
public sealed class RecallPortalProjectile : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_0";
    public int PairId => (int)Projectile.ai[0];
    public int Side => (int)Projectile.ai[2];
    public bool Ready => Projectile.ai[1] >= RecallPortalSystem.DelayTicks;

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
        if (Projectile.ai[1] < 0) return; // 生成端等待建立通道的确认，尚未扣费或开始蓄力。
        Projectile.ai[1]++;
        // 寿命以生成端同步的年龄为准；确认回包与出口生成会重置阶段起点，
        // 不能用 Math.Min 保留等待网络期间已经减少的 timeLeft，否则高延迟会缩短残留时间。
        Projectile.timeLeft = Math.Max(1,
            RecallPortalSystem.DelayTicks + RecallPortalSystem.ResidueTicks - (int)Projectile.ai[1] + 1);
    }

    // 与占位图的椭圆足迹一致。真实传送范围另外按玩家脚底到阵心的距离校验。
    public bool Hovered(Vector2 world) => RecallPortalDrawing.Contains(world - Projectile.Center, RecallPortalSystem.AreaRadius);

    public override bool PreDraw(ref Color lightColor)
    {
        RecallPortalDrawing.Draw(Main.spriteBatch, TextureAssets.MagicPixel.Value, Projectile.Center - Main.screenPosition,
            RecallPortalSystem.AreaRadius, Projectile.ai[1], RecallPortalSystem.DelayTicks, RecallPortalSystem.ResidueTicks,
            Side == 1, Hovered(Main.MouseWorld));
        return false;
    }
}
