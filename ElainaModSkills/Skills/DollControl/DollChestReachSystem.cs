using System;
using MonoMod.Cil;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ObjectData;

namespace 伊蕾娜.ElainaModSkills.Skills.DollControl;

/// <summary>Only vanilla's aimed tile-interaction gate is extended. Mod right-click code and its own range checks stay intact.</summary>
public sealed class DollChestReachSystem : ModSystem
{
    public override void Load()
    {
        IL_Player.LookForTileInteractions += ExtendChestGate;
        On_Player.TileInteractionsUse += UseChest;
        IL_Player.HandleBeingInChestRange += ExtendChestLifetime;
        On_NetMessage.SendData += ObserveChestRequest;
    }

    public override void Unload()
    {
        IL_Player.LookForTileInteractions -= ExtendChestGate;
        On_Player.TileInteractionsUse -= UseChest;
        IL_Player.HandleBeingInChestRange -= ExtendChestLifetime;
        On_NetMessage.SendData -= ObserveChestRequest;
    }

    public override void OnWorldUnload()
    {
        if (Main.netMode != NetmodeID.Server && Main.myPlayer >= 0 && Main.myPlayer < Main.maxPlayers)
            Main.LocalPlayer.GetModPlayer<DollCommandPlayer>().ReleaseRemoteChest(false);
    }

    private static void ExtendChestGate(ILContext il)
    {
        var cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchCall<Player>(nameof(Player.IsInTileInteractionRange))))
            throw new InvalidOperationException("Doll chest reach: vanilla tile-interaction gate was not found.");
        cursor.Remove();
        cursor.EmitDelegate<Func<Player, int, int, TileReachCheckSettings, bool>>(InteractionGate);
    }

    internal static bool InteractionGate(Player player, int x, int y, TileReachCheckSettings settings)
        => player.IsInTileInteractionRange(x, y, settings) || CanExtend(player, x, y);

    internal static bool CanExtend(Player player, int x, int y)
        => player.whoAmI == Main.myPlayer && Main.netMode != NetmodeID.Server && player.active && !player.dead
            && !Main.gameMenu && !Main.mapFullscreen && !Main.blockMouse && !player.mouseInterface
            && !Main.drawingPlayerChat && !Main.editSign && !Main.inFancyUI
            && TryChestOrigin(x, y, out _) && TileOnScreen(x, y)
            && player.GetModPlayer<DollCommandPlayer>().FindIdleChestHelper() != null;

    internal static bool TileOnScreen(int x, int y, float margin = 0)
    {
        Vector2 screen = Vector2.Transform(new Vector2(x * 16 + 8, y * 16 + 8) - Main.screenPosition,
            Main.GameViewMatrix.TransformationMatrix);
        return screen.X >= -margin && screen.Y >= -margin
            && screen.X < Main.screenWidth + margin && screen.Y < Main.screenHeight + margin;
    }

    internal static bool TryChestOrigin(int x, int y, out Point origin)
    {
        origin = default;
        if (!WorldGen.InWorld(x, y, 2)) return false;
        Tile tile = Main.tile[x, y];
        if (!tile.HasTile || !TileID.Sets.BasicChest[tile.TileType] || tile.TileFrameX < 0 || tile.TileFrameY < 0) return false;
        var data = TileObjectData.GetTileData(tile);
        int column, row;
        if (data == null)
        { column = tile.TileFrameX % 36 / 18; row = tile.TileFrameY % 36 / 18; }
        else
        {
            column = tile.TileFrameX % data.CoordinateFullWidth / (data.CoordinateWidth + data.CoordinatePadding);
            int offset = tile.TileFrameY % data.CoordinateFullHeight;
            for (row = 0; row < data.Height - 1 && offset >= data.CoordinateHeights[row] + data.CoordinatePadding; row++)
                offset -= data.CoordinateHeights[row] + data.CoordinatePadding;
        }
        origin = new(x - column, y - row);
        return WorldGen.InWorld(origin.X, origin.Y, 2) && Main.tile[origin.X, origin.Y].HasTile
            && Main.tile[origin.X, origin.Y].TileType == tile.TileType;
    }

    private static void UseChest(On_Player.orig_TileInteractionsUse orig, Player player, int x, int y)
    {
        bool remote = player.tileInteractAttempted && player.releaseUseTile
            && !player.IsInTileInteractionRange(x, y, TileReachCheckSettings.Simple) && CanExtend(player, x, y);
        var commands = player.GetModPlayer<DollCommandPlayer>();
        if (remote)
        {
            TryChestOrigin(x, y, out var origin);
            // Closing an already-open chest remains immediate; opening is performed after the doll arrives.
            if (player.chest < 0 || player.chestX != origin.X || player.chestY != origin.Y)
            {
                commands.BeginRemoteChest(origin, new Point(x, y), orig);
                player.tileInteractionHappened = true;
                player.releaseUseTile = false;
                return;
            }
        }
        orig(player, x, y);
    }

    private static void ExtendChestLifetime(ILContext il)
    {
        var cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchCall<Player>(nameof(Player.IsInInteractionRangeToMultiTileHitbox))))
            throw new InvalidOperationException("Doll chest reach: vanilla chest lifetime gate was not found.");
        cursor.Remove();
        cursor.EmitDelegate<Func<Player, int, int, bool>>((player, x, y) =>
            player.IsInInteractionRangeToMultiTileHitbox(x, y) || player.GetModPlayer<DollCommandPlayer>().MaintainsRemoteChest(x, y));
    }

    private static void ObserveChestRequest(On_NetMessage.orig_SendData orig, int msg, int remote, int ignore, NetworkText text,
        int number, float number2, float number3, float number4, int number5, int number6, int number7)
    {
        // Observe the vanilla request; never replace its items, key consumption, or networking.
        if (Main.netMode == NetmodeID.MultiplayerClient && msg == MessageID.RequestChestOpen)
            Main.LocalPlayer.GetModPlayer<DollCommandPlayer>().AwaitRemoteChest(new(number, (int)number2));
        orig(msg, remote, ignore, text, number, number2, number3, number4, number5, number6, number7);
    }
}
