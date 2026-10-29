using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModAlchemy.Revealing;

/// <summary>每个区域首次及每 60 tick 整区扫描；同一轮的重叠物块/敌人只判定、标记一次。</summary>
public sealed class RevealingDustSystem : ModSystem
{
    private delegate bool HunterOriginal(NPC npc);
    private delegate void AnimatedOriginal(TileDrawing self, int i, int j, Tile tile, ushort type,
        short frameX, short frameY, ref Color light, bool canDoDust);
    private delegate void AnimatedHook(AnimatedOriginal orig, TileDrawing self, int i, int j, Tile tile, ushort type,
        short frameX, short frameY, ref Color light, bool canDoDust);
    private readonly List<RevealingDustArea> areas = new(), due = new();
    private readonly HashSet<RevealingDustArea> previousAreas = new();
    // FNA Point 的哈希在密集网格上冲突较多；世界内线性索引使整区扫描保持便宜。
    private readonly Dictionary<int, ushort> visibleDanger = new();
    private readonly Dictionary<int, bool> scanMemo = new();
    private readonly HashSet<int> visibleWaterBolts = new();
    private static readonly Vector3 WaterBoltLight = new(.12f, .42f, .85f);

    public override void Load()
    {
        if (Main.dedServ) return;
        MonoModHooks.Add(typeof(NPC).GetMethod(nameof(NPC.CanApplyHunterPotionEffects)),
            (Func<HunterOriginal, NPC, bool>)HunterEffects);
        MonoModHooks.Add(typeof(TileDrawing).GetMethod(nameof(TileDrawing.DrawAnimatedTile_AdjustForVisionChangers)),
            (AnimatedHook)AnimatedDanger);
        // DrawEffects is too late: finalColor and the all-black early-out have already been computed.
        MonoModHooks.Modify(typeof(TileDrawing).GetMethod("DrawSingleTile", BindingFlags.Instance | BindingFlags.NonPublic),
            InstallEarlyTileVision);
    }

    private void InstallEarlyTileVision(ILContext il)
    {
        ILCursor cursor = new(il);
        if (!cursor.TryGotoNext(MoveType.After, i => i.MatchStfld<TileDrawInfo>(nameof(TileDrawInfo.tileLight))))
            throw new InvalidOperationException("Revealing Dust could not find the initial tile-light sample.");
        cursor.Emit(OpCodes.Ldarg_1);
        cursor.Emit(OpCodes.Ldarg, 6); // tileX
        cursor.Emit(OpCodes.Ldarg, 7); // tileY
        cursor.EmitDelegate<Action<TileDrawInfo, int, int>>(ApplyTileVision);
    }

    internal void ApplyTileVision(TileDrawInfo drawData, int x, int y)
    {
        if (IsHighlighted(x, y, drawData.typeCache))
            drawData.tileLight = RevealingDustDrawing.DangerColor(drawData.tileLight);
    }

    private static bool HunterEffects(HunterOriginal orig, NPC npc)
        => orig(npc) || npc.TryGetGlobalNPC<RevealingDustNPC>(out var mark) && mark.Marked;

    private void AnimatedDanger(AnimatedOriginal orig,
        TileDrawing self, int i, int j, Tile tile, ushort type, short frameX, short frameY, ref Color light, bool canDoDust)
    {
        orig(self, i, j, tile, type, frameX, frameY, ref light, canDoDust);
        if (IsHighlighted(i, j, type)) light = RevealingDustDrawing.DangerColor(light);
    }

    public override void PostUpdateProjectiles()
    {
        if (Main.gameMenu) { Reset(); return; }
        areas.Clear(); due.Clear();
        ulong now = Main.GameUpdateCount;
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile.ModProjectile is not RevealingDustArea area) continue;
            areas.Add(area);
            if (!area.HasScanned || now >= area.NextScan)
            {
                area.HasScanned = true;
                area.NextScan = now + RevealingDustArea.ScanInterval;
                due.Add(area);
            }
        }

        if (!Main.dedServ && (due.Count > 0 || !previousAreas.SetEquals(areas)))
        {
            scanMemo.Clear();
            foreach (RevealingDustArea area in due) ScanTiles(area);
            visibleDanger.Clear(); visibleWaterBolts.Clear();
            foreach (RevealingDustArea area in areas)
            {
                foreach (var tile in area.DangerousTiles) visibleDanger[tile.Key] = tile.Value;
                visibleWaterBolts.UnionWith(area.WaterBoltTiles);
            }
        }
        previousAreas.Clear(); previousAreas.UnionWith(areas);

        if (Main.netMode == NetmodeID.MultiplayerClient || due.Count == 0) return;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (!RevealingDustNPC.IsHostile(npc)) continue;
            foreach (RevealingDustArea area in due)
            {
                if (!area.Contains(npc.Center)) continue;
                npc.GetGlobalNPC<RevealingDustNPC>().Refresh(npc);
                break;
            }
        }
    }

    internal void ScanTiles(RevealingDustArea area)
    {
        area.DangerousTiles.Clear();
        area.WaterBoltTiles.Clear();
        Vector2 center = area.Projectile.Center;
        int radius = RevealingDustArea.Radius;
        int minX = Math.Clamp((int)Math.Floor((center.X - radius) / 16), 0, Main.maxTilesX - 1);
        int maxX = Math.Clamp((int)Math.Floor((center.X + radius) / 16), 0, Main.maxTilesX - 1);
        int minY = Math.Clamp((int)Math.Floor((center.Y - radius) / 16), 0, Main.maxTilesY - 1);
        int maxY = Math.Clamp((int)Math.Floor((center.Y + radius) / 16), 0, Main.maxTilesY - 1);
        for (int x = minX; x <= maxX; x++)
        for (int y = minY; y <= maxY; y++)
        {
            if (!area.Contains(new Vector2(x * 16 + 8, y * 16 + 8))) continue;
            Tile tile = Main.tile[x, y];
            if (!tile.HasTile) continue;
            int point = x + y * Main.maxTilesX;
            if (IsWaterBolt(tile)) area.WaterBoltTiles.Add(point);
            if (!scanMemo.TryGetValue(point, out bool dangerous))
                scanMemo[point] = dangerous = TileDrawing.IsTileDangerous(x, y, Main.LocalPlayer);
            if (dangerous) area.DangerousTiles[point] = tile.TileType;
        }
    }

    /// <summary>书本物块第 6 种样式（frameX 90）即水矢，破坏后掉落水矢。</summary>
    internal static bool IsWaterBolt(Tile tile) => tile.HasTile && tile.TileType == TileID.Books && tile.TileFrameX == 90;

    /// <summary>当前仍存在的已标记水矢；被拿走的书本立即停止标记，不等下一次扫描。</summary>
    internal IEnumerable<Point> VisibleWaterBolts()
    {
        foreach (int point in visibleWaterBolts)
        {
            int x = point % Main.maxTilesX, y = point / Main.maxTilesX;
            if (IsWaterBolt(Main.tile[x, y])) yield return new(x, y);
        }
    }

    internal bool IsHighlighted(int x, int y, int type) => visibleDanger.TryGetValue(x + y * Main.maxTilesX, out ushort cachedType)
        && cachedType == type && Main.tile[x, y].HasTile;

    public override void PostUpdateEverything()
    {
        if (Main.dedServ || Main.gameMenu) return;
        Vector3 light = new(.65f, .38f, .10f);
        // 光照每帧提交，不能跟随每秒一次的扫描，否则光源会闪断。
        // 使用合并缓存，重叠区域只为同一物块提交一次光源。
        foreach (var entry in visibleDanger)
        {
            int x = entry.Key % Main.maxTilesX;
            int y = entry.Key / Main.maxTilesX;
            Tile tile = Main.tile[x, y];
            if (tile.HasTile && tile.TileType == entry.Value)
                Lighting.AddLight(x, y, light.X, light.Y, light.Z);
        }
        foreach (Point point in VisibleWaterBolts())
            Lighting.AddLight(point.X, point.Y, WaterBoltLight.X, WaterBoltLight.Y, WaterBoltLight.Z);

        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (npc.IsABestiaryIconDummy || !npc.TryGetGlobalNPC<RevealingDustNPC>(out var mark) || !mark.Marked) continue;
            Lighting.AddLight(npc.Center, light);
        }
    }

    private static Texture2D Texture(string path) => ModContent.Request<Texture2D>(path).Value;

    public override void PostDrawTiles()
    {
        if (Main.dedServ || Main.gameMenu) return;
        RevealingDustTextures tex = default;
        bool begun = false;
        SpriteBatch batch = Main.spriteBatch;
        Matrix transform = Main.GameViewMatrix.TransformationMatrix;
        bool Begin(Vector2 at, float margin)
        {
            Vector2 screen = Vector2.Transform(at, transform);
            if (screen.X < -margin || screen.Y < -margin || screen.X > Main.screenWidth + margin || screen.Y > Main.screenHeight + margin)
                return false;
            if (!begun)
            {
                begun = true;
                tex = new RevealingDustTextures(Texture("伊蕾娜/ElainaModAlchemy/Revealing/Assets/MarkGlow"),
                    Texture("伊蕾娜/ElainaModSkills/UICore/Assets/GlowDot"), Texture("伊蕾娜/ElainaModSkills/UICore/Assets/Star4"),
                    Texture("KL/Effects/Tex/Sparkle/T_StartFlare001"), Texture("KL/Effects/Tex/Sparkle/ShotLine"));
                batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                    DepthStencilState.None, RasterizerState.CullNone, null, transform);
            }
            return true;
        }
        try
        {
            foreach (Point point in ModContent.GetInstance<RevealingRecipeChests>().VisibleChests())
            {
                Vector2 at = new Vector2(point.X * 16 + 16, point.Y * 16 + 16) - Main.screenPosition;
                if (Begin(at, 80)) RevealingDustDrawing.DrawRecipeChest(batch, tex, at,
                    (float)(Main.GameUpdateCount % 36000), point.X + point.Y * 397);
            }
            foreach (Point point in VisibleWaterBolts())
            {
                Vector2 at = new Vector2(point.X * 16 + 8, point.Y * 16 + 8) - Main.screenPosition;
                if (Begin(at, 64)) RevealingDustDrawing.DrawWaterBolt(batch, tex, at,
                    (float)(Main.GameUpdateCount % 36000), point.X + point.Y * 397);
            }
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.ModProjectile is not RevealingDustArea) continue;
                Vector2 at = projectile.Center - Main.screenPosition;
                float age = projectile.ai[0];
                if (age < RevealingDustDrawing.BurstTicks && Begin(at, RevealingDustArea.Radius + 160))
                    RevealingDustDrawing.Draw(batch, tex, at, age, projectile.identity);
                if (Begin(at, 160))
                    RevealingDustDrawing.DrawCenter(batch, tex, at, age, RevealingDustArea.Duration, projectile.identity);
            }
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.TryGetGlobalNPC<RevealingDustNPC>(out var mark) || !mark.Marked || npc.IsABestiaryIconDummy) continue;
                Vector2 at = npc.Center - Main.screenPosition;
                if (!Begin(at, 96)) continue;
                // Draw once after terrain/NPCs, including enemies whose normal draw layer is behind tiles.
                RevealingDustDrawing.DrawMark(batch, tex.Glow, at, Math.Max(npc.width, npc.height),
                    (float)(Main.GameUpdateCount % 36000), npc.whoAmI);
            }
        }
        finally { if (begun) batch.End(); }
    }

    private void Reset()
    {
        areas.Clear(); due.Clear(); previousAreas.Clear(); visibleDanger.Clear(); scanMemo.Clear(); visibleWaterBolts.Clear();
    }
    public override void ClearWorld() => Reset();
    public override void OnWorldUnload() => Reset();
    public override void Unload() => Reset();
}
