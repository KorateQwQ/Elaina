using Terraria;
using Terraria.ModLoader;

namespace 伊蕾娜.Mounts;

/// <summary>同时覆盖物品、快捷坐骑和直接 SetMount（存档/同步/其他模组）。</summary>
public sealed class BroomAccessSystem : ModSystem
{
    public static bool CanRide(Player player) => player.GetModPlayer<ElainaModplayer>().Elaina;

    public override void Load()
    {
        On_Mount.CanMount += CanMount;
        On_Mount.SetMount += SetMount;
    }

    public override void Unload()
    {
        On_Mount.CanMount -= CanMount;
        On_Mount.SetMount -= SetMount;
    }

    private static bool CanMount(On_Mount.orig_CanMount orig, Mount self, int type, Player player) =>
        (type != ModContent.MountType<扫帚>() || CanRide(player)) && orig(self, type, player);

    private static void SetMount(On_Mount.orig_SetMount orig, Mount self, int type, Player player, bool faceLeft)
    {
        if (type == ModContent.MountType<扫帚>() && !CanRide(player)) return;
        orig(self, type, player, faceLeft);
    }
}
