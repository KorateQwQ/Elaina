using Terraria.UI;

namespace 伊蕾娜.ElainaModSkills.CutIn;

/// <summary>
/// 奥义特写的游戏接入：加载贴图、在界面层最上方（鼠标指针之下）插入绘制层、锁定玩家位置。
/// 绘制本身在 <see cref="ElainaCutInDrawing"/>，不依赖 Terraria，可由 Tools/CutInPreview 的 FNA 宿主直接链接。
/// </summary>
public sealed class ElainaCutInSystem : ModSystem
{
    private static Asset<Texture2D>[] frames;
    private static Asset<Texture2D> whitePixel, portalLine, starFlare, thinRing;
    private static BasicEffect effect;

    public override void PostSetupContent()
    {
        if (Main.dedServ) return;
        // 人像帧按源帧号索引（CutIn_01..21），共约 43 MB 显存（21 × 960×540 RGBA），异步加载；未就绪的帧只画特写带。
        frames = new Asset<Texture2D>[ElainaCutIn.MaxFrame + 1];
        for (int i = 1; i <= ElainaCutIn.MaxFrame; i++)
            frames[i] = ModContent.Request<Texture2D>($"伊蕾娜/ElainaModSkills/CutIn/Frames/CutIn_{i:00}", AssetRequestMode.AsyncLoad);
        whitePixel = ModContent.Request<Texture2D>("伊蕾娜/Effects/Tex/Baked/WhitePixel");
        portalLine = ModContent.Request<Texture2D>("伊蕾娜/ElainaModSkills/Skills/Recall/Assets/PortalLine");
        starFlare = ModContent.Request<Texture2D>("伊蕾娜/ElainaModSkills/UICore/Assets/StarFlare");
        thinRing = ModContent.Request<Texture2D>("伊蕾娜/ElainaModSkills/UICore/Assets/ThinRing");
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        if (Main.dedServ || Main.gameMenu || !ElainaCutIn.Active) return;
        int cursor = layers.FindIndex(layer => layer.Name == "Vanilla: Cursor");
        layers.Insert(cursor < 0 ? layers.Count : cursor, new LegacyGameInterfaceLayer("伊蕾娜: Cut-In", DrawLayer, InterfaceScaleType.None));
    }

    /// <summary>InterfaceScaleType.None：物理屏幕像素；人像大小在绘制中按 Main.UIScale 计算。</summary>
    private static bool DrawLayer()
    {
        // 暂停（单人自动暂停的菜单等）时画面定格，不推进动画
        ElainaCutIn.Advance(paused: Main.gamePaused);
        if (!ElainaCutIn.Active || !whitePixel.IsLoaded || !portalLine.IsLoaded || !starFlare.IsLoaded || !thinRing.IsLoaded)
            return true;

        GraphicsDevice device = Main.graphics.GraphicsDevice;
        effect ??= new BasicEffect(device);
        if (ElainaCutIn.PlayerHeadY == null)
        {
            Player player = Main.LocalPlayer;
            Vector2 center = Vector2.Transform(player.Center - Main.screenPosition, Main.GameViewMatrix.ZoomMatrix);
            ElainaCutIn.PlayerHeadY = center.Y - player.height * .5f * Main.GameViewMatrix.Zoom.Y;
        }

        float t = ElainaCutIn.Time;
        Asset<Texture2D> portrait = frames[ElainaCutIn.FrameAt(t)];
        var textures = new ElainaCutInDrawing.Textures(whitePixel.Value, portalLine.Value, starFlare.Value, thinRing.Value,
            portrait.IsLoaded ? portrait.Value : null);
        Viewport viewport = device.Viewport;
        var frame = new ElainaCutInDrawing.Frame(viewport.Width, viewport.Height, Main.UIScale, ElainaCutIn.PlayerHeadY.Value,
            t, ElainaCutIn.StrikeTime, ElainaCutIn.ExitStart, ElainaCutIn.ExitDuration);

        Main.spriteBatch.End();
        ElainaCutInDrawing.Draw(device, effect, textures, frame);
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, Matrix.Identity);
        return true;
    }

    public override void OnWorldUnload() => ElainaCutIn.Stop();

    public override void Unload()
    {
        ElainaCutIn.Stop();
        frames = null;
        whitePixel = portalLine = starFlare = thinRing = null;
        BasicEffect toDispose = effect;
        effect = null;
        if (toDispose != null) Main.QueueMainThreadAction(toDispose.Dispose);
    }
}
