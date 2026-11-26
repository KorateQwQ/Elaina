using Terraria.DataStructures;
using 伊蕾娜.ElainaAttribute;

namespace 伊蕾娜.ElainaModSkills;

public abstract partial class ElainaSkill
{
    /// <summary>本次施法需要并在成功后消耗的充能点。普通和快捷施法使用同一入口。</summary>
    public virtual int ChargePointCost => SkillBalanceSystem.Current.Skills.TryGetValue(BalanceId, out var entry) ? entry.EnergyCost : 0;

    internal bool HasRequiredChargePoints()
    {
        int cost = ChargePointCost;
        return cost == 0 || Player.GetModPlayer<ChargePointPlayer>().Points.Has(cost);
    }

    public sealed override bool PreUseSkill(IEntitySource source = null)
    {
        int cost = ChargePointCost;
        if (cost == 0) return OnCast(source);
        return Player.GetModPlayer<ChargePointPlayer>().Points.TryCast(cost, source, OnCast);
    }

    protected virtual bool OnCast(IEntitySource source) => base.PreUseSkill(source);
}
