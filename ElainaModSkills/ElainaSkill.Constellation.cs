using System;
using KL.SkillSystem;
using Terraria.ID;

namespace 伊蕾娜.ElainaModSkills;

public abstract partial class ElainaSkill
{
    /// <summary>技能图标旁显示的可选无色图案，不包含文件扩展名。</summary>
    public virtual string ConstellationUncoloredIconPath => SkillTexturePath + "_Gray";

    /// <summary>星图中的可选位置，单位为设计像素；未指定时根据 SkillUIInfo 推导。</summary>
    public virtual Vector2? ConstellationPosition => null;

    /// <summary>各等级的解锁要求。None 或 null 表示没有额外消耗，但仍会应用 MaxLevel。</summary>
    public virtual SkillUnlockCondition GetConstellationUpgradeCondition(int nextLevel) => SkillUnlockCondition.ByItemsAndSkillPoint(20,[new SkillUnlockItem(ItemID.Wood,10)]);

    /// <summary>可选的战斗数值，在指定等级下计算。不自动推导伤害。</summary>
    public virtual (string Label, string Value)[] GetConstellationStats(int level) => [];

    /// <summary>用于升级比较的实际当前值和下一等级值。在定义成长效果前保持为空。</summary>
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
