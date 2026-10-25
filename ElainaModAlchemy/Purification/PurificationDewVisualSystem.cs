using System;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModAlchemy.Purification;

public sealed class PurificationDewVisualSystem : ModSystem
{
    private static PurificationDewRenderer renderer;
    private static bool failed;
    internal static PurificationDewRenderer GetRenderer()
    {
        if (Main.dedServ || failed) return null;
        if (renderer != null) return renderer;
        try
        {
            Effect effect = ModContent.Request<Effect>("伊蕾娜/Effects/Content/PurificationDew", AssetRequestMode.ImmediateLoad).Value;
            // 与用户提供的贴图逐像素一致，复用已有 KL 依赖中的正式资源。
            Texture2D trail = ModContent.Request<Texture2D>("KL/Effects/Tex/Trail/LightTrail2", AssetRequestMode.ImmediateLoad).Value;
            return renderer = new PurificationDewRenderer(effect, trail);
        }
        catch (Exception exception)
        {
            failed = true;
            ModContent.GetInstance<PurificationDewVisualSystem>().Mod.Logger.Error("Purification Dew shader could not load.", exception);
            return null;
        }
    }
    public override void Unload()
    {
        renderer = null; failed = false;
    }
}
