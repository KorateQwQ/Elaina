using System;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModAlchemy.Rain;

public sealed class RainCloudVisualSystem : ModSystem
{
    private static RainCloudShaderRenderer renderer;
    private static bool unavailable;

    // 从 PreDraw 首次调用，在绘制线程加载并共享；专用服务器不加载 Shader 或分配纹理。
    internal static RainCloudShaderRenderer GetRenderer()
    {
        if (Main.dedServ || unavailable) return null;
        if (renderer != null) return renderer;
        try
        {
            Effect material = ModContent.Request<Effect>("伊蕾娜/Effects/Content/BottledRainCloud", AssetRequestMode.ImmediateLoad).Value;
            Texture2D white = ModContent.Request<Texture2D>("伊蕾娜/Effects/Tex/Baked/WhitePixel", AssetRequestMode.ImmediateLoad).Value;
            Texture2D noise = ModContent.Request<Texture2D>("伊蕾娜/ElainaModAlchemy/Rain/Assets/CloudNoise", AssetRequestMode.ImmediateLoad).Value;
            renderer = new RainCloudShaderRenderer(material, white, noise);
            return renderer;
        }
        catch (Exception exception)
        {
            unavailable = true;
            ModContent.GetInstance<RainCloudVisualSystem>().Mod.Logger.Error("Bottled Rain shader unavailable; using the original sprite cloud.", exception);
            return null;
        }
    }

    public override void Unload()
    {
        renderer = null;
        unavailable = false;
    }
}
