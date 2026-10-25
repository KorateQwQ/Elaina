using Terraria;
using Terraria.ModLoader;

namespace 伊蕾娜.Mounts;

public sealed class BroomPlatformCollision : ModSystem
{
    public override void Load()
    {
        On_Player.DryCollision += DryCollision;
        On_Player.WaterCollision += WaterCollision;
        On_Player.HoneyCollision += HoneyCollision;
        On_Player.ShimmerCollision += ShimmerCollision;
    }

    public override void Unload()
    {
        On_Player.DryCollision -= DryCollision;
        On_Player.WaterCollision -= WaterCollision;
        On_Player.HoneyCollision -= HoneyCollision;
        On_Player.ShimmerCollision -= ShimmerCollision;
    }

    private static void ApplyPlatformDrop(Player player, ref bool fallThrough, ref bool ignorePlats)
    {
        if (!player.controlDown || !player.mount.Active ||
            player.mount.Type != ModContent.MountType<扫帚>() ||
            !player.GetModPlayer<扫帚.扫帚ModPlayer>().PoweredFlight)
            return;

        // 原版仅为指定坐骑开启 ignorePlats；否则即使按下，Y 速度 > 1 仍会撞平台。
        // 两个参数一起传给原版碰撞，保留实体方块、斜坡和液体的正常处理。
        fallThrough = true;
        ignorePlats = true;
    }

    private static void DryCollision(On_Player.orig_DryCollision orig, Player player, bool fallThrough, bool ignorePlats)
    {
        ApplyPlatformDrop(player, ref fallThrough, ref ignorePlats);
        orig(player, fallThrough, ignorePlats);
    }

    private static void WaterCollision(On_Player.orig_WaterCollision orig, Player player, bool fallThrough, bool ignorePlats)
    {
        ApplyPlatformDrop(player, ref fallThrough, ref ignorePlats);
        orig(player, fallThrough, ignorePlats);
    }

    private static void HoneyCollision(On_Player.orig_HoneyCollision orig, Player player, bool fallThrough, bool ignorePlats)
    {
        ApplyPlatformDrop(player, ref fallThrough, ref ignorePlats);
        orig(player, fallThrough, ignorePlats);
    }

    private static void ShimmerCollision(On_Player.orig_ShimmerCollision orig, Player player, bool fallThrough, bool ignorePlats, bool noCollision)
    {
        ApplyPlatformDrop(player, ref fallThrough, ref ignorePlats);
        orig(player, fallThrough, ignorePlats, noCollision);
    }
}
