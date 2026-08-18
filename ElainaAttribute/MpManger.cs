using Terraria;

namespace 伊蕾娜.ElainaAttribute;

/// <summary>
/// 用于控制规划技能如何设定蓝耗、
/// 核心逻辑1：当玩家完全不受伤，可以完全使用mp用于技能时，其dps大致为期望dps的两倍。
/// 2:只允许魔力飞弹的蓝耗小于每秒回复魔力，魔力飞弹的dps大致为期望dps的0.7倍（前期为1倍）。
/// 3：魔力回复速度只能有限手段增加，其中最重要道具为帽子+2.5，奥术水晶（需要找到微光）+2.5,魔力再生手环(需要破坏邪恶群落珠子）+2.5。其他效果：星星瓶，+1魔力再生药水+1，以及魔力强化炎+1只能提供少许加成。
/// </summary>
public static class MpManger
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