using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModAlchemy.Revealing;

/// <summary>固定区域用弹幕同步；年龄同步保证晚加入不会重置五分钟寿命。</summary>
public sealed class RevealingDustArea : ModProjectile
{
    public const int Radius = 1000;
    public const int Duration = 5 * 60 * 60;
    public const int ScanInterval = 60;
    internal readonly Dictionary<int, ushort> DangerousTiles = new();
    /// <summary>区域内水矢书本物块的世界线性索引，与危险物块同轮扫描。</summary>
    internal readonly HashSet<int> WaterBoltTiles = new();
    internal ulong NextScan;
    internal bool HasScanned;

    public override string Texture => "伊蕾娜/ElainaModAlchemy/item/ExampleAssets/RevealingDust_Pixel";

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.aiStyle = -1;
        Projectile.penetrate = -1;
        Projectile.timeLeft = Duration;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.friendly = Projectile.hostile = false;
        Projectile.netImportant = true;
        DangerousTiles.Clear();
        WaterBoltTiles.Clear();
        HasScanned = false;
        NextScan = 0;
    }

    public override bool? CanDamage() => false;
    public override bool? CanCutTiles() => false;
    public override bool ShouldUpdatePosition() => false;

    public override void AI()
    {
        Projectile.velocity = Vector2.Zero;
        if (++Projectile.ai[0] >= Duration) { Projectile.Kill(); return; }
        Projectile.timeLeft = Math.Max(2, Duration - (int)Projectile.ai[0]);
        if (!Main.dedServ) SubmitLight();
    }

    /// <summary>圆心持续照明提示释放位置；飞散期间部分粉尘沿实际位置照亮所经区域。</summary>
    private void SubmitLight()
    {
        float age = Projectile.ai[0];
        Lighting.AddLight(Projectile.Center,
            RevealingDustDrawing.CenterLightColor * RevealingDustDrawing.CenterStrength(age, Duration));
        if (age >= RevealingDustDrawing.BurstTicks) return;
        for (int i = 0; i < RevealingDustDrawing.ParticleCount; i += RevealingDustDrawing.LightStride)
            if (RevealingDustDrawing.Mote(i, age, Projectile.identity, Projectile.Center, out Vector2 at, out float alpha, out float size))
                Lighting.AddLight(at, RevealingDustDrawing.MoteLightColor * (alpha * size));
    }

    public bool Contains(Vector2 point) => Vector2.DistanceSquared(Projectile.Center, point) <= Radius * Radius;

    // 粉尘与圆心星光统一在 RevealingDustSystem.PostDrawTiles 以 LinearClamp 绘制。
    public override bool PreDraw(ref Color lightColor) => false;
}
