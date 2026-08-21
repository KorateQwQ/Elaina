using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using 伊蕾娜.ElainaAttribute;
using 伊蕾娜.ElainaModSkills.ElainaSkillUI;

namespace 伊蕾娜.ElainaModSkills;

public class ElainaSkillManager : ModSystem
{
    public static bool ShowCD = false;

    public override void UpdateUI(GameTime gameTime)
    {
        UpdateElainaResourceDisplaySet();
    }

    private static void UpdateElainaResourceDisplaySet()
    {
        if (Main.dedServ || Main.LocalPlayer == null)
        {
            return;
        }

        bool isElaina = Main.LocalPlayer.active && Main.LocalPlayer.GetModPlayer<ElainaModplayer>().Elaina;
        string elainaDisplaySetKey = ModContent.GetInstance<ElainaPlayerResourceDisplaySet>().ConfigKey;

        if (isElaina)
        {
            CycleToResourceDisplaySet(elainaDisplaySetKey);
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