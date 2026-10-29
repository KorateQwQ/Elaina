using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.Gameplay;

namespace 伊蕾娜.ElainaModAlchemy.Revealing;

/// <summary>Server-authoritative chest forecasts, sent privately to the dust owner.</summary>
public sealed class RevealingRecipeChests : ModSystem
{
    private readonly Dictionary<int, List<RevealingDustArea>> ownerAreas = new();
    private readonly Dictionary<int, HashSet<Point>> sent = new();
    private readonly HashSet<int> previousProjectiles = new(), currentProjectiles = new();
    private readonly HashSet<Point> visible = new();
    private ulong nextScan, expiresAt;

    public override void PostUpdateProjectiles()
    {
        if (Main.gameMenu) { Reset(); return; }
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        ownerAreas.Clear(); currentProjectiles.Clear();
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile.ModProjectile is not RevealingDustArea area
                || projectile.owner < 0 || projectile.owner >= Main.maxPlayers) continue;
            if (!ownerAreas.TryGetValue(projectile.owner, out var areas))
                ownerAreas[projectile.owner] = areas = new();
            areas.Add(area);
            currentProjectiles.Add(projectile.whoAmI);
        }
        ulong now = Main.GameUpdateCount;
        if (now < nextScan && previousProjectiles.SetEquals(currentProjectiles)) return;
        nextScan = now + RevealingDustArea.ScanInterval;
        previousProjectiles.Clear(); previousProjectiles.UnionWith(currentProjectiles);
        for (int owner = 0; owner < Main.maxPlayers; owner++)
        {
            if (!ownerAreas.TryGetValue(owner, out var areas) && !sent.ContainsKey(owner)) continue;
            Player player = Main.player[owner];
            HashSet<Point> found = new();
            if (player is { active: true, dead: false } && areas != null)
            {
                player.GetModPlayer<AlchemyRecipeDiscoveryPlayer>().ObservePossessions();
                foreach (Chest chest in Main.chest)
                {
                    if (chest == null) continue;
                    Vector2 center = new(chest.x * 16 + 16, chest.y * 16 + 16);
                    foreach (var area in areas)
                    {
                        if (!area.Contains(center)) continue;
                        if (AlchemyRecipeChestLoot.RevealsRecipe(player, chest)) found.Add(new(chest.x, chest.y));
                        break;
                    }
                }
            }
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                if (owner == Main.myPlayer) { visible.Clear(); visible.UnionWith(found); expiresAt = now + 180; }
            }
            else if (player is { active: true })
            {
                // Small private snapshots also refresh the timeout after joins or missed updates.
                var packet = Mod.GetPacket();
                packet.Write((byte)global::伊蕾娜.伊蕾娜.MessageType.RevealingRecipeChests);
                packet.Write(owner);
                packet.Write((ushort)found.Count);
                foreach (Point point in found) { packet.Write((short)point.X); packet.Write((short)point.Y); }
                packet.Send(owner);
            }
            if (areas == null || player is not { active: true }) sent.Remove(owner);
            else sent[owner] = found;
        }
    }

    internal static void HandlePacket(BinaryReader reader, int playerIndex)
    {
        // Clients cannot publish chest visibility or inspect another player's private snapshot.
        if (Main.netMode != NetmodeID.MultiplayerClient || playerIndex != Main.myPlayer) return;
        int count = reader.ReadUInt16();
        if (count > Main.maxChests || reader.BaseStream.Length - reader.BaseStream.Position < count * 4L) return;
        var system = ModContent.GetInstance<RevealingRecipeChests>();
        system.visible.Clear();
        for (int i = 0; i < count; i++)
        {
            int x = reader.ReadInt16(), y = reader.ReadInt16();
            if (WorldGen.InWorld(x, y, 0)) system.visible.Add(new(x, y));
        }
        system.expiresAt = Main.GameUpdateCount + 180;
    }

    internal IEnumerable<Point> VisibleChests()
    {
        if (Main.dedServ || Main.gameMenu || Main.LocalPlayer is not { active: true, dead: false }
            || Main.GameUpdateCount >= expiresAt) yield break;
        foreach (Point point in visible)
        {
            Tile tile = Main.tile[point.X, point.Y];
            if (!tile.HasTile || !TileID.Sets.BasicChest[tile.TileType]) continue;
            Vector2 center = new(point.X * 16 + 16, point.Y * 16 + 16);
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner != Main.myPlayer || projectile.ModProjectile is not RevealingDustArea area
                    || !area.Contains(center)) continue;
                yield return point;
                break;
            }
        }
    }

    private void Reset()
    {
        ownerAreas.Clear(); sent.Clear(); visible.Clear(); previousProjectiles.Clear(); currentProjectiles.Clear();
        nextScan = expiresAt = 0;
    }
    public override void ClearWorld() => Reset();
    public override void OnWorldUnload() => Reset();
    public override void Unload() => Reset();
}
