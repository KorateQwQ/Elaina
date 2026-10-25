using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace 伊蕾娜.Mounts;

/// <summary>暂停时 Player 不更新，但 UI 仍更新；失焦/暂停必须丢弃待释放的点按。</summary>
public sealed class BroomInputSystem : ModSystem
{
    public static bool InputBlocked => !Main.hasFocus || Main.gameMenu || Main.gamePaused
        || Main.blockInput || Main.drawingPlayerChat || Main.editSign || Main.editChest;

    public override void UpdateUI(GameTime gameTime)
    {
        if (!Main.dedServ && InputBlocked && Main.LocalPlayer?.active == true)
            Main.LocalPlayer.GetModPlayer<扫帚.扫帚ModPlayer>().FlightState.Cancel();
    }
}
