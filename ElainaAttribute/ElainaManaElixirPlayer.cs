using Terraria;
using Terraria.ModLoader;

namespace 伊蕾娜.ElainaAttribute;

/// <summary>月露合剂每秒恢复最大独特魔力的 1%，不影响原版魔力。</summary>
public sealed class ElainaManaElixirPlayer : ModPlayer
{
    public bool Active;
    private int recoveryTicks;

    public override void ResetEffects() => Active = false;

    public override void UpdateDead()
    {
        Active = false;
        ResetRecovery();
    }

    private void ResetRecovery()
    {
        recoveryTicks = 0;
    }

    public override void PostUpdate()
    {
        var attributes = Player.GetModPlayer<ElainaAttributeModPlayer>();
        // 仅由所属玩家结算；独特魔力未启用、效果结束或死亡时清空计时。
        if (!Active || Player.dead || Player.whoAmI != Main.myPlayer
            || !attributes.UniqueMagicEnabled)
        {
            ResetRecovery();
            return;
        }

        if (++recoveryTicks < 60) return;
        recoveryTicks = 0;

        attributes.RegenPercentMagicPoint(1f);
    }
}
