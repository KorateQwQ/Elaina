using System;
using KL.SkillSystem;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.Localization;

namespace 伊蕾娜.ElainaModSkills;

public abstract partial class ElainaSkill
{
    /// <summary>Optional uncolored artwork beside the skill icon, without the file extension.</summary>
    public virtual string ConstellationUncoloredIconPath => SkillTexturePath + "_Gray";

    /// <summary>Optional map position in design pixels; otherwise derived from SkillUIInfo.</summary>
    public virtual Vector2? ConstellationPosition => null;

    /// <summary>Per-level requirements. None or null means no additional cost; MaxLevel still applies.</summary>
    public virtual SkillUnlockCondition GetConstellationUpgradeCondition(int nextLevel) => SkillUnlockCondition.ByItemsAndSkillPoint(20,[new SkillUnlockItem(ItemID.Wood,10)]);

    /// <summary>Optional combat values, evaluated at the requested level. No inferred damage.</summary>
    public virtual (string Label, string Value)[] GetConstellationStats(int level) => [];

    /// <summary>Real current/next values for the upgrade comparison. Leave empty until growth is defined.</summary>
    public virtual (string Label, string Current, string Next, string Unit)[] GetConstellationUpgradePreview(int nextLevel) => [];

    public virtual string GetConstellationDescription()
    {
        string key = $"Mods.伊蕾娜.SkillInfo.SkillDesc.{GetType().Name}";
        if (!Language.Exists(key)) return "技能描述待补充。";
        try
        {
            string text = Language.GetText(key).WithFormatArgs(SkillDescriptionArgs).Value;
            return string.IsNullOrWhiteSpace(text) ? "技能描述待补充。" : text;
        }
        catch (FormatException)
        {
            return "技能描述参数待补充。";
        }
    }
}
