using Terraria.ID;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

/// <summary>选区确认与实际执行共用的工具/进度限制；墙边缘等可随任务推进解除的阻挡不算工具不足。</summary>
internal static class DollMiningRules
{
    // 暗影珠与猩红之心共用此物块类型，样式由帧区分。
    internal static bool IsHammerTarget(int type) => type == TileID.ShadowOrbs;

    internal static string BlockReason(Player player, Point point, bool walls, Item tool)
    {
        Tile tile = Main.tile[point.X, point.Y];
        bool hammer = walls || IsHammerTarget(tile.TileType);
        if (tool == null || (hammer ? tool.hammer : tool.pick) <= 0)
            return hammer ? "DollNoHammer" : "DollNoPickaxe";
        if (!walls && hammer) return null;
        if (!walls)
            return DollTileBreaker.CanDamageTile(player, point.X, point.Y, tool.pick) ? null : "DollPickPowerInsufficient";
        if (Main.wallDungeon[tile.WallType] && !NPC.downedBoss3) return "DollWallRequiresSkeletron";
        if (tile.WallType == WallID.LihzahrdBrickUnsafe && !NPC.downedGolemBoss) return "DollWallRequiresGolem";
        return null;
    }
}
