using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModAlchemy.Rain;

/// <summary>客户端按同步后的雨云/图块重建标记，不写图块或额外发送网络消息。</summary>
public sealed class RainPlantIndicatorSystem : ModSystem
{
    private sealed class Marker
    {
        internal int SeenAt;
        internal float Opacity;
    }

    private Dictionary<Point, Marker> markers;
    private List<Point> plants;
    private List<Point> removals;
    private int ticks;

    public override void PostUpdateProjectiles()
    {
        if (Main.dedServ) return;
        if (Main.gameMenu) { Reset(); return; }
        markers ??= new Dictionary<Point, Marker>();
        plants ??= new List<Point>();
        removals ??= new List<Point>();
        ticks++;
        if (ticks % 10 == 1)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.ModProjectile is not BottledRainCloud
                    || projectile.ai[0] != 1 || projectile.ai[1] < 30) continue;
                RainPlantGrowth.CollectCoveredPlants(projectile.Center, plants);
                foreach (Point plant in plants)
                {
                    Point root = RainPlantGrowth.IndicatorRoot(plant);
                    if (!markers.TryGetValue(root, out Marker marker)) markers[root] = marker = new Marker();
                    marker.SeenAt = ticks;
                }
            }
        }

        removals.Clear();
        foreach (var pair in markers)
        {
            bool alive = RainPlantGrowth.CanReceiveGrowth(pair.Key.X, pair.Key.Y);
            if (!alive) { removals.Add(pair.Key); continue; }
            bool covered = ticks - pair.Value.SeenAt <= 10;
            pair.Value.Opacity = MathHelper.Clamp(pair.Value.Opacity + (covered ? .08f : -.15f), 0, 1);
            if (!covered && pair.Value.Opacity <= 0) removals.Add(pair.Key);
        }
        foreach (Point point in removals) markers.Remove(point);
    }

    public override void PostDrawTiles()
    {
        if (Main.dedServ || Main.gameMenu || markers == null || markers.Count == 0) return;
        SpriteBatch batch = Main.spriteBatch;
        Matrix transform = Main.GameViewMatrix.TransformationMatrix;
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
            DepthStencilState.None, RasterizerState.CullNone, null, transform);
        try
        {
            foreach (var pair in markers)
            {
                if (!RainPlantGrowth.CanReceiveGrowth(pair.Key.X, pair.Key.Y)) continue;
                Vector2 at = RainPlantGrowth.IndicatorAnchor(pair.Key) - Main.screenPosition;
                Vector2 screen = Vector2.Transform(at, transform);
                if (screen.X < -48 || screen.Y < -48 || screen.X > Main.screenWidth + 48 || screen.Y > Main.screenHeight + 48) continue;
                RainPlantIndicatorDrawing.Draw(batch, TextureAssets.MagicPixel.Value, at, ticks,
                    pair.Value.Opacity, pair.Key.X * 17 + pair.Key.Y * 31);
            }
        }
        finally { batch.End(); }
    }

    private void Reset()
    {
        markers?.Clear(); plants?.Clear(); removals?.Clear(); ticks = 0;
    }
    public override void ClearWorld() => Reset();
    public override void OnWorldUnload() => Reset();
    public override void Unload() => Reset();
}
