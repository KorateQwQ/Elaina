using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

/// <summary>The bear badge identifies the existing chest hover hint when an idle doll can assist.</summary>
public sealed class DollChestCursorSystem : ModSystem
{
    private static Asset<Texture2D> bear;
    private static bool hoveredChest;
    private static uint hoverTick;
    private static int hoverIcon;

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
    }

    public override void OnWorldUnload() => hoveredChest = false;

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

    // tML increments GameUpdateCount after player updates, before drawing this frame's hint.
    internal static bool HasBadge(Player player) => hoveredChest && unchecked(Main.GameUpdateCount - hoverTick) <= 1
        && player.whoAmI == Main.myPlayer && player.active && !player.dead
        && !Main.gameMenu && !Main.mapFullscreen && !Main.HoveringOverAnNPC && !player.mouseInterface
        && player.cursorItemIconEnabled && player.cursorItemIconID == hoverIcon && hoverIcon > 0
        && player.GetModPlayer<DollCommandPlayer>().FindChestHelper() != null;

    private static void DrawBadge(On_Main.orig_DrawInterface_40_InteractItemIcon orig, Main self)
    {
        orig(self);
        Player player = Main.LocalPlayer;
        if (!HasBadge(player) || hoverIcon >= ItemLoader.ItemCount) return;
        // Vanilla has loaded and drawn this exact item frame. Reuse its offset/scale in the current UI batch.
        Rectangle frame = Item.GetDrawHitbox(hoverIcon, null);
        float push = 10 + player.cursorItemIconPush;
        bear ??= ModContent.Request<Texture2D>("伊蕾娜/ElainaModSkills/Skills/DollControl/Assets/ChestCursorBear");
        DollChestCursorDrawing.Draw(Main.spriteBatch, bear.Value, new(Main.mouseX + push, Main.mouseY + push),
            new(frame.Width, frame.Height), Main.cursorScale);
    }
}
