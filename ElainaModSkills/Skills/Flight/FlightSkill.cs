using System;
using KL.SkillSystem;
using KL.SkillSystem.SilkyUI;
using Terraria.Localization;
using Terraria.ModLoader.IO;

namespace 伊蕾娜.ElainaModSkills.Skills.Flight;

[SkillUIInfo(State = 3, Pixels = 300)]
public class FlightSkill : ElainaSkill
{
    public static FlightBalance Balance => SkillBalanceSystem.TryGetSkill(nameof(FlightSkill), out var skill)
        ? skill.Flight ?? new() : new();

    public override bool IsPassiveSkill => true;
    // 扫帚加速与冲刺扣魔力时不转换生命（见 扫帚.cs），描述据此追加通用警告。
    public override bool AllowsLifeConversion => false;
    public override SkillUnlockCondition UnlockCondition { get; set; } =
        new CompositeSkillUnlockCondition(DefaultUnlockCost, RequireConfiguredCharacterLevelCap(nameof(FlightSkill), 15),
            RequireConfiguredCharacterLevel(nameof(FlightSkill), 11));
    public override void Initialize()
    {
        base.Initialize();
        MaxCD = -1;
        MagicPointCost = 0;
    }
    public override void DeserializeData(TagCompound tag)
    {
        base.DeserializeData(tag);
        MaxCD = -1;
        MagicPointCost = 0;
        CurrentCD = CooldownDuration = 0;
    }
    public override bool PreUpdateCD() => false;
    public override bool CanUseSkill() => false;
    protected override object[] SkillDescriptionArgs
    {
        get
        {
            var b = FlightSkill.Balance;
            return [(b.Speed(Math.Max(1, Level)) / b.BaseSpeed - 1f) * 100,
                b.BoostMultiplier, b.BoostManaPerSecond, b.DashManaCost, b.DashCooldownSeconds];
        }
    }
    public override bool TryGetToolTip(ref string name, ref string level, ref string desc)
    {
        MaxCD = -1;
        MagicPointCost = 0;
        return base.TryGetToolTip(ref name, ref level, ref desc);
    }
    private static string SpeedLabel => Language.GetTextValue("Mods.伊蕾娜.SkillInfo.FlightStats.Speed");
    private static string BoostLabel => Language.GetTextValue("Mods.伊蕾娜.SkillInfo.FlightStats.Boost");
    public override (string Label, string Value, string Unit)[] GetConstellationStats(int level)
    {
        var b = FlightSkill.Balance;
        return [(SpeedLabel, b.Speed(level).ToString("0.##"), ""),
            (BoostLabel, (b.Speed(level) * b.BoostMultiplier).ToString("0.##"), "")];
    }
    public override (string Label, string Current, string Next, string Unit)[] GetConstellationUpgradePreview(int nextLevel)
    {
        var b = FlightSkill.Balance;
        return [(SpeedLabel, b.Speed(nextLevel - 1).ToString("0.##"), b.Speed(nextLevel).ToString("0.##"), ""),
            (BoostLabel, (b.Speed(nextLevel - 1) * b.BoostMultiplier).ToString("0.##"),
                (b.Speed(nextLevel) * b.BoostMultiplier).ToString("0.##"), "")];
    }
}
