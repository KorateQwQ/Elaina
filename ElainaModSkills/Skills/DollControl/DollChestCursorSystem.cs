using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

/// <summary>
/// 熊头像角标标明这次操作交给玩偶：远处宝箱的原版悬停提示，以及远程放置时的物块图标。
/// </summary>
public sealed class DollChestCursorSystem : ModSystem
{
    private static Asset<Texture2D> bear;
    private static bool hoveredChest;
    private static uint hoverTick;
    private static int hoverIcon;
    private static uint placementTick = uint.MaxValue;
    private static int placementItem;

    public override void Load()
    {
        if (Main.dedServ) return;
        On_Player.TileInteractionsMouseOver += ChestMouseOver;
        On_Main.DrawInterface_40_InteractItemIcon += DrawBadge;
    }

    public override void Unload()
    {
        On_Player.TileInteractionsMouseOver -= ChestMouseOver;
        On_Main.DrawInterface_40_InteractItemIcon -= DrawBadge;
        bear = null;
        hoveredChest = false;
        placementTick = uint.MaxValue;
    }

    public override void OnWorldUnload()
    {
        hoveredChest = false;
        placementTick = uint.MaxValue;
    }

    private static void ChestMouseOver(On_Player.orig_TileInteractionsMouseOver orig, Player player, int x, int y)
    {
        orig(player, x, y);
        CaptureHover(player, x, y);
    }

    internal static void CaptureHover(Player player, int x, int y)
    {
        if (player.whoAmI != Main.myPlayer) return;
        hoveredChest = player.cursorItemIconEnabled && player.cursorItemIconID > 0
            && DollChestReachSystem.CanExtend(player, x, y);
        hoverTick = Main.GameUpdateCount;
        hoverIcon = player.cursorItemIconID;
    }

    /// <summary>远程放置分支已让原版显示手持物块图标；同一帧记下，绘制时补上熊头像。</summary>
    internal static void CapturePlacementHover(Player player)
    {
        if (player.whoAmI != Main.myPlayer || player.cursorItemIconID != 0) return;
        placementTick = Main.GameUpdateCount;
        placementItem = player.inventory[player.selectedItem].type;
    }

    // tML increments GameUpdateCount after player updates, before drawing this frame's hint.
    internal static bool HasBadge(Player player) => hoveredChest && unchecked(Main.GameUpdateCount - hoverTick) <= 1
        && player.whoAmI == Main.myPlayer && player.active && !player.dead
        && !Main.gameMenu && !Main.mapFullscreen && !Main.HoveringOverAnNPC && !player.mouseInterface
        && player.cursorItemIconEnabled && player.cursorItemIconID == hoverIcon && hoverIcon > 0
        && player.GetModPlayer<DollCommandPlayer>().FindChestHelper() != null;

    internal static bool HasPlacementBadge(Player player) => unchecked(Main.GameUpdateCount - placementTick) <= 1
        && player.whoAmI == Main.myPlayer && player.active && !player.dead
        && !Main.gameMenu && !Main.mapFullscreen && !Main.HoveringOverAnNPC && !player.mouseInterface
        && player.cursorItemIconEnabled && player.cursorItemIconID == 0 && placementItem > 0
        && player.inventory[player.selectedItem].type == placementItem && DollPetProjectile.FindOwned(player) != null;

    private static void DrawBadge(On_Main.orig_DrawInterface_40_InteractItemIcon orig, Main self)
    {
        orig(self);
        Player player = Main.LocalPlayer;
        int icon = HasBadge(player) ? hoverIcon : HasPlacementBadge(player) ? PlacementIcon(player) : 0;
        if (icon <= 0 || icon >= ItemLoader.ItemCount) return;
        // Vanilla has loaded and drawn this exact item frame. Reuse its offset/scale in the current UI batch.
        Rectangle frame = Item.GetDrawHitbox(icon, null);
        float push = 10 + player.cursorItemIconPush;
        bear ??= ModContent.Request<Texture2D>("伊蕾娜/ElainaModSkills/Skills/DollControl/Assets/ChestCursorBear");
        DollChestCursorDrawing.Draw(Main.spriteBatch, bear.Value, new(Main.mouseX + push, Main.mouseY + push),
            new(frame.Width, frame.Height), Main.cursorScale);
    }

    /// <summary>与原版提示相同的图标物品：生物群系火把/篝火按当前环境显示。</summary>
    private static int PlacementIcon(Player player)
    {
        int type = placementItem;
        if (!player.UsingBiomeTorches) return type;
        return type switch
        {
            Terraria.ID.ItemID.Torch => player.BiomeTorchHoldStyle(type),
            Terraria.ID.ItemID.Campfire => player.BiomeCampfireHoldStyle(type),
            _ => type,
        };
    }
}
