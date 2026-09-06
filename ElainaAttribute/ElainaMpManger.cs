using Terraria;

namespace 伊蕾娜.ElainaAttribute;

/// <summary>
/// 已弃用！！！
/// </summary>
public class ElainaMpManger : ModSystem
{
    public static float GetAdditionalMagicPointRecovery(Player player)
    {
        float recovery = 0f;

        if (player.usedArcaneCrystal)
            recovery += 2.5f;

        if (player.manaRegenBuff)
            recovery += 1f;

        if (player.nebulaLevelMana > 0)
            recovery += player.nebulaLevelMana;

        return recovery;
    }
}