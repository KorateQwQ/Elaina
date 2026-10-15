using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaModAlchemy.Buffs;

/// <summary>共鸣按 NPC、攻击者独立计时；重复鞭击续时，多目标互不覆盖。</summary>
public sealed class ResonanceGlobalNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;
    private Dictionary<int, ulong> expiresAt;

    public override void SetDefaults(NPC entity) => expiresAt = null;

    // tML 的玩家弹幕命中和伤害修正在攻击者客户端结算，因此标记也在该端记录。
    // 不使用全员共享的 NPC Buff，避免其他玩家错误地得到增伤。
    public void Mark(int playerIndex)
    {
        expiresAt ??= new Dictionary<int, ulong>();
        expiresAt[playerIndex] = Main.GameUpdateCount + 3 * 60;
    }

    public bool IsMarkedBy(int playerIndex) => expiresAt != null
        && expiresAt.TryGetValue(playerIndex, out ulong expiry) && Main.GameUpdateCount < expiry;
}
