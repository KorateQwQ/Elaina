using System;
using KL.SkillSystem;
using Terraria.ModLoader;
using 伊蕾娜.Items.消耗品;

namespace 伊蕾娜.ElainaModSkills;

/// <summary>
/// 忘却之书的结算：消耗一本，把已习得技能退回 Lv.1（仍保持习得），返还 Lv.2 起每次进修的研习点。
/// 进修消耗的材料不返还；首次研习的花费不返还。
/// </summary>
internal static class SkillForgetting
{
    internal static int BookType => ModContent.ItemType<BookOfOblivion>();

    /// <summary>每次只消耗一本，沿用 KL 的背包计数与扣除规则。</summary>
    private static SkillUnlockCondition Cost => SkillUnlockCondition.ByItems(new SkillUnlockItem(BookType, 1));

    internal static bool HasBook(KLSkillModPlayer player) => player != null && Cost.CanUnlock(player, null);

    /// <summary>背包中的数量，与 KL 物品条件的统计范围一致。</summary>
    internal static int Books(KLSkillModPlayer player)
    {
        if (player?.Player?.inventory == null) return 0;
        long total = 0;
        foreach (var item in player.Player.inventory)
            if (item != null && !item.IsAir && item.type == BookType) total += item.stack;
        return (int)Math.Min(int.MaxValue, total);
    }

    /// <summary>当前等级各次进修花费的研习点之和，按技能现有的进修条件计算。</summary>
    internal static int Refund(ElainaSkill skill, int level)
    {
        long total = 0;
        for (int next = 2; next <= level; next++)
            total += Points(skill.GetConstellationUpgradeCondition(next));
        return (int)Math.Min(int.MaxValue, total);
    }

    private static long Points(SkillUnlockCondition condition) => condition switch
    {
        SkillPointUnlockCondition points => points.SkillPointCost,
        CompositeSkillUnlockCondition composite => Sum(composite),
        _ => 0
    };

    private static long Sum(CompositeSkillUnlockCondition composite)
    {
        long total = 0;
        foreach (var child in composite.Conditions) total += Points(child);
        return total;
    }

    internal static bool CanForget(KLSkillModPlayer player, Skill owned)
        => owned?.ModSkill is ElainaSkill && owned.Level > 1 && HasBook(player);

    /// <summary>结算成功时返回 true；不满足条件时不扣书、不改等级。</summary>
    internal static bool TryForget(KLSkillModPlayer player, Skill owned, out int refund)
    {
        refund = 0;
        if (!CanForget(player, owned)) return false;
        refund = Refund((ElainaSkill)owned.ModSkill, owned.Level);
        if (!Cost.TryUnlock(player, owned.ModSkill)) { refund = 0; return false; }
        owned.Level = 1;
        player.SkillPoint = (int)Math.Min(int.MaxValue, (long)player.SkillPoint + refund);
        return true;
    }
}
