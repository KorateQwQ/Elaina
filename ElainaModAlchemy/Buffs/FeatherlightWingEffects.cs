using Terraria;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModAlchemy.Buffs;

public sealed class FeatherlightWingEffects : GlobalItem
{
    public override void VerticalWingSpeeds(Item item, Player player, ref float ascentWhenFalling,
        ref float ascentWhenRising, ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend)
    {
        float multiplier = player.GetModPlayer<AlchemyBuffPlayer>().TravelSpeedMultiplier;
        if (multiplier == 1f) return;
        ascentWhenFalling *= multiplier;
        ascentWhenRising *= multiplier;
        maxCanAscendMultiplier *= multiplier;
        maxAscentMultiplier *= multiplier;
        constantAscend *= multiplier;
    }
}

/// <summary>原版喷气背包按住上键时绕过 VerticalWingSpeeds，仅此分支临时调整其上升限速基准。</summary>
public sealed class FeatherlightJetpackSystem : ModSystem
{
    public override void Load() => On_Player.WingMovement += ModifyJetpackAscent;
    public override void Unload() => On_Player.WingMovement -= ModifyJetpackAscent;

    private static void ModifyJetpackAscent(On_Player.orig_WingMovement orig, Player player)
    {
        float multiplier = player.GetModPlayer<AlchemyBuffPlayer>().TravelSpeedMultiplier;
        if (player.wingsLogic != 4 || !player.TryingToHoverUp || multiplier == 1f)
        {
            orig(player);
            return;
        }

        // jumpSpeed 是原版按当前玩家更新的静态移动基准，只在本次原版调用内借用。
        float jumpSpeed = Player.jumpSpeed;
        try
        {
            Player.jumpSpeed *= multiplier;
            orig(player);
        }
        finally
        {
            Player.jumpSpeed = jumpSpeed;
        }
    }
}
