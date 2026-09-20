using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KL.SkillSystem;
using Terraria;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

internal sealed record ConstellationRequirement(string Name, long Owned, long Needed, bool Met, bool Points = false, bool Custom = false)
{
    internal string Detail => Custom ? Met ? "已满足" : "未满足" : $"持有 {Owned} / 需要 {Needed}"
        + (Met ? " · 已满足" : $" · 缺少 {Math.Max(0, Needed - Owned)}");
}

internal sealed class ConstellationRequirements
{
    internal readonly List<ConstellationRequirement> Rows = [];
    internal bool Satisfied { get; private set; }
    internal long PointCost { get; private set; }
    internal bool HasItems => Rows.Any(r => !r.Points && !r.Custom);
    internal string BlockReason => Rows.Any(r => !r.Met && !r.Points && !r.Custom)
        ? Rows.Any(r => !r.Met && r.Points) ? "材料和研习点不足" : "材料不足"
        : Rows.Any(r => !r.Met && r.Points) ? "研习点不足" : Satisfied ? "" : "条件未满足";
    internal string Tooltip(string heading) => heading + "\n" + (Rows.Count == 0 ? "无消耗"
        : string.Join("\n", Rows.Select(r => r.Name + "：" + r.Detail)));
    internal string ActionTooltip(string heading) => heading + "\n" + (Satisfied
        ? Rows.Count == 0 ? "无消耗，点击即可" : "需求已满足，点击即可"
        : string.Join("\n", Rows.Where(r => !r.Met).Select(r => r.Name + (r.Custom ? "：未满足" : $"：还缺 {Math.Max(0, r.Needed - r.Owned)}"))));
    internal static string Localize(string text) => Regex.Replace(text ?? "", @"\bSP\b", "研习点", RegexOptions.IgnoreCase);

    internal static ConstellationRequirements Read(SkillUnlockCondition condition, ElainaSkillModPlayer player, ModSkill skill)
    {
        var result = new ConstellationRequirements();
        var materials = new Dictionary<int, long>();
        var custom = new List<SkillUnlockCondition>();
        void Collect(SkillUnlockCondition current)
        {
            switch (current)
            {
                case NoSkillUnlockCondition: break;
                case ItemSkillUnlockCondition items:
                    foreach (var item in items.Items) materials[item.ItemType] = materials.GetValueOrDefault(item.ItemType) + item.Stack;
                    break;
                case SkillPointUnlockCondition points: result.PointCost += points.SkillPointCost; break;
                case CompositeSkillUnlockCondition composite:
                    foreach (var child in composite.Conditions) Collect(child);
                    break;
                case not null: custom.Add(current); break;
            }
        }
        Collect(condition);
        foreach (var (type, count) in materials)
        {
            long owned = player.Player.inventory.Where(i => i != null && !i.IsAir && i.type == type).Sum(i => (long)i.stack);
            result.Rows.Add(new(Lang.GetItemNameValue(type), owned, count, owned >= count));
        }
        if (result.PointCost > 0)
            result.Rows.Add(new("研习点", player.SkillPoint, result.PointCost, player.SkillPoint >= result.PointCost, Points: true));
        foreach (var rule in custom)
            result.Rows.Add(new(Localize(rule.GetDescription(player, skill)), 0, 0, rule.CanUnlock(player, skill), Custom: true));
        if (condition == null) result.Rows.Add(new("条件待补充", 0, 0, false, Custom: true));
        result.Satisfied = condition != null && result.Rows.All(r => r.Met) && condition.CanUnlock(player, skill);
        return result;
    }
}
