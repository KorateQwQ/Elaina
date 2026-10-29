using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using 伊蕾娜.ElainaAttribute;

namespace 伊蕾娜.ElainaModSkills;

public class ElainaSkillManager : ModSystem
{
    public static bool ShowCD = false;
    public static float WheelScale = .5f;
    private static float barScale = 1f;
    public static float BarScale
    {
        get => barScale;
        set => barScale = float.IsFinite(value) ? global::System.Math.Clamp(value, .5f, 1f) : 1f;
    }
    private static ElainaModplayer resourceDisplayOwner;

    internal static void RestoreResourceDisplay(ElainaModplayer player)
    {
        if (Main.dedServ || player.Player != Main.LocalPlayer) return;

        string key = ModContent.GetInstance<ElainaPlayerResourceDisplaySet>().ConfigKey;
        if (player.Elaina && player.UseElainaResourceDisplay)
            CycleToResourceDisplaySet(key);
        else if (Main.ResourceSetsManager.ActiveSet?.ConfigKey == key)
            Main.ResourceSetsManager.CycleResourceSet();

        // 只在入场时恢复。此后玩家的手动选择可以自由改变，不再每帧锁定。
        resourceDisplayOwner = player;
    }

    internal static void RecordResourceDisplayChoice(ElainaModplayer player)
    {
        // 保存退出时 gameMenu 已经为 true；用入场/卸载之间的所有者判断，
        // 避免菜单预览、创建角色、存档克隆或其他角色污染这一角色的偏好。
        if (Main.dedServ || player != resourceDisplayOwner || player.Player != Main.LocalPlayer
            || !player.Elaina || Main.ResourceSetsManager.ActiveSet == null) return;

        player.UseElainaResourceDisplay = Main.ResourceSetsManager.ActiveSet.ConfigKey
            == ModContent.GetInstance<ElainaPlayerResourceDisplaySet>().ConfigKey;
    }

    public override void OnWorldUnload() => resourceDisplayOwner = null;

    public override void Unload() => resourceDisplayOwner = null;

    public override void UpdateUI(GameTime gameTime)
    {
        UpdateElainaResourceDisplaySet();
    }

    private static void UpdateElainaResourceDisplaySet()
    {
        if (Main.dedServ || Main.gameMenu || Main.LocalPlayer == null || !Main.LocalPlayer.active)
        {
            return;
        }

        var player = Main.LocalPlayer.GetModPlayer<ElainaModplayer>();
        string elainaDisplaySetKey = ModContent.GetInstance<ElainaPlayerResourceDisplaySet>().ConfigKey;

        if (player.Elaina)
        {
            RecordResourceDisplayChoice(player);
            return;
        }

        if (Main.ResourceSetsManager.ActiveSet?.ConfigKey == elainaDisplaySetKey)
        {
            Main.ResourceSetsManager.CycleResourceSet();
        }
    }

    private static bool CycleToResourceDisplaySet(string targetConfigKey)
    {
        if (string.IsNullOrEmpty(targetConfigKey) || Main.ResourceSetsManager.GetDisplaySet(targetConfigKey) == null)
        {
            return false;
        }

        for (int i = 0; i < 64; i++)
        {
            if (Main.ResourceSetsManager.ActiveSet?.ConfigKey == targetConfigKey)
            {
                return true;
            }

            Main.ResourceSetsManager.CycleResourceSet();
        }

        return Main.ResourceSetsManager.ActiveSet?.ConfigKey == targetConfigKey;
    }
}
