using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using 伊蕾娜.ElainaModAlchemy.item.Curios;

namespace 伊蕾娜.ElainaModAlchemy.Rain;

/// <summary>仅本机持有瓶中雨时显示；每次绘制重新读取地形，不推进任何游戏逻辑。</summary>
public sealed class RainAimPreviewSystem : ModSystem
{
    // 绘制阶段的 Main.MouseWorld 不在世界缩放坐标下，屏幕缩放时会偏离实际投掷目标；
    // 改用与 Shoot 同一 ItemCheck 阶段缓存的目标。
    private static Vector2 aimTarget;
    private static uint aimTick;
    private static bool hasAim;
    private readonly List<Vector2> path = new(61);
    private readonly List<Point> plants = new();
    private readonly HashSet<Point> roots = new();
    private readonly List<RainAimPlant> markers = new();
    private readonly float[] lengths = new float[RainCloudDrawing.Columns];

    internal static bool CanPreview(Player player) => !Main.dedServ && !Main.gameMenu
        && !Main.mapFullscreen && !Main.playerInventory && !Main.blockMouse
        && player.active && !player.dead && !player.ghost && !player.noItems && !player.CCed
        && !player.mouseInterface && player.itemAnimation == 0
        && player.HeldItem.ModItem is BottledRain;

    internal static void TrackAim(Player player)
    {
        if (player.whoAmI != Main.myPlayer) return;
        aimTarget = Main.MouseWorld;
        aimTick = Main.GameUpdateCount;
        hasAim = true;
    }

    public override void PostDrawTiles()
    {
        if (Main.dedServ || Main.gameMenu) return;
        Player player = Main.LocalPlayer;
        if (!CanPreview(player) || !hasAim || Main.GameUpdateCount - aimTick > 2) return;
        Vector2 origin = RainThrowTrajectory.Origin(player);
        Vector2 velocity = RainThrowTrajectory.Aim(origin, aimTarget, player.gravDir, out int flight);
        Vector2 impact = RainThrowTrajectory.Predict(origin, velocity, flight, path);
        Vector2 cloud = RainThrowTrajectory.CloudCenter(impact);
        RainPlantGrowth.MeasureRain(cloud, lengths);
        RainPlantGrowth.CollectCoveredPlants(cloud, plants);
        roots.Clear(); markers.Clear();
        foreach (Point plant in plants)
        {
            Point root = RainPlantGrowth.IndicatorRoot(plant);
            if (roots.Add(root))
                markers.Add(new RainAimPlant(RainPlantGrowth.IndicatorAnchor(root), RainPlantGrowth.IsInPlanter(root)));
        }
        SpriteBatch batch = Main.spriteBatch;
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
            DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        try { RainAimDrawing.Draw(batch, TextureAssets.MagicPixel.Value, Main.screenPosition, path, impact, cloud, lengths, markers); }
        finally { batch.End(); }
    }

    public override void ClearWorld() { hasAim = false; path.Clear(); plants.Clear(); roots.Clear(); markers.Clear(); }
    public override void OnWorldUnload() => ClearWorld();
    public override void Unload() => ClearWorld();
}
