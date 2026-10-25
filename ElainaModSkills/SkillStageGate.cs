using System;
using System.Linq;
using KL.SkillSystem;

namespace 伊蕾娜.ElainaModSkills;

/// <summary>
/// 阶段门槛：<see cref="Reachable"/> 为 false 表示当前进度下不可能满足（如等级上限不足、事件尚未开放），
/// 技能在星图中“未显现”。门槛可达但尚未满足时，按普通条件作为待研习的缺口显示。
/// </summary>
public sealed class StageGateUnlockCondition : SkillUnlockCondition
{
    private readonly Func<KLSkillModPlayer, ModSkill, bool> _met;
    private readonly Func<KLSkillModPlayer, ModSkill, bool> _reachable;
    private readonly Func<KLSkillModPlayer, ModSkill, string> _description;

    public StageGateUnlockCondition(Func<KLSkillModPlayer, ModSkill, bool> met, Func<KLSkillModPlayer, ModSkill, bool> reachable,
        Func<KLSkillModPlayer, ModSkill, string> description)
    {
        _met = met ?? throw new ArgumentNullException(nameof(met));
        _reachable = reachable ?? throw new ArgumentNullException(nameof(reachable));
        _description = description;
    }

    public override bool CanUnlock(KLSkillModPlayer skillPlayer, ModSkill modSkill) => _met(skillPlayer, modSkill);

    public bool Reachable(KLSkillModPlayer skillPlayer, ModSkill modSkill) => _reachable(skillPlayer, modSkill);

    public override string GetDescription(KLSkillModPlayer skillPlayer, ModSkill modSkill) =>
        _description?.Invoke(skillPlayer, modSkill) ?? string.Empty;

    /// <summary>展开组合条件；任一门槛在当前阶段不可达时返回 false。</summary>
    public static bool AllReachable(SkillUnlockCondition condition, KLSkillModPlayer skillPlayer, ModSkill modSkill) => condition switch
    {
        StageGateUnlockCondition gate => gate.Reachable(skillPlayer, modSkill),
        CompositeSkillUnlockCondition composite => composite.Conditions.All(c => AllReachable(c, skillPlayer, modSkill)),
        _ => true
    };
}
