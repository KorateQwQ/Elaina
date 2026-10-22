using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace 伊蕾娜.ElainaModSkills.Skills.Recall;

public sealed class RecallTransitionSystem : ModSystem
{
    public override void UpdateUI(GameTime gameTime)
    {
        if (Main.netMode == NetmodeID.Server) return;
        var transition = Main.LocalPlayer.GetModPlayer<RecallPortalPlayer>().Transition;
        if (Main.gameMenu || !Main.LocalPlayer.active || Main.LocalPlayer.dead) transition.Clear();
        else transition.Advance((float)gameTime.ElapsedGameTime.TotalSeconds);
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        if (Main.netMode == NetmodeID.Server || Main.gameMenu) return;
        // 在世界画面之后、HUD 之前绘制，采用物理屏幕坐标，不受 UI 缩放影响。
        layers.Insert(0, new LegacyGameInterfaceLayer("伊蕾娜: Recall Transition", () =>
        {
            Main.LocalPlayer.GetModPlayer<RecallPortalPlayer>().Transition.Draw(Main.spriteBatch,
                TextureAssets.MagicPixel.Value, Main.graphics.GraphicsDevice.Viewport.Bounds);
            return true;
        }, InterfaceScaleType.None));
    }

    public override void OnWorldUnload()
    {
        if (Main.netMode != NetmodeID.Server)
            Main.LocalPlayer.GetModPlayer<RecallPortalPlayer>().Transition.Clear();
    }
}
