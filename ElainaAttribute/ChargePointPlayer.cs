using KL.AttributeSystem;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace 伊蕾娜.ElainaAttribute;

/// <summary>随角色保存的充能点和当前一格的恢复进度，在角色拥有端结算。</summary>
public sealed class ChargePointPlayer : ModPlayer
{
    /// <summary>可由装备、Buff 或技能加成的每秒充能点数。</summary>
    public static readonly AttributeDefinition RecoveryPerSecond = new(minValue: 0f);
    public static float BaseRecoveryPerSecond => (float)(1 / ElainaModSkills.SkillBalanceConfig.BaseSecondsPerEnergyPoint);

    public ChargePoints Points { get; private set; }
    public override void Initialize() => Points = new();

    public override void SaveData(TagCompound tag)
    {
        tag["points"] = Points.Current;
        tag["recoveryUnits"] = Points.RecoveryUnits;
    }

    public override void LoadData(TagCompound tag) =>
        Points.Load(tag.GetInt("points"), tag.GetDouble("recoveryUnits"));
}
