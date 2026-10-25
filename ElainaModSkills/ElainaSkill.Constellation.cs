using static 伊蕾娜.Localization.UIText;
using System;
using KL.SkillSystem;
using Terraria.ID;

namespace 伊蕾娜.ElainaModSkills;

public abstract partial class ElainaSkill
{
    /// <summary>是否已启封信息；使用面板所属玩家，避免共享模板串用角色进度。</summary>
    public virtual bool IsConstellationRevealed(Player player) => true;

    /// <summary>待启封时显示的探索提示。</summary>
    public virtual string ConstellationRevealHint => string.Empty;

    /// <summary>技能图标旁显示的可选无色图案，不包含文件扩展名。</summary>
    public virtual string ConstellationUncoloredIconPath => SkillTexturePath + "_Gray";

    /// <summary>星图中的可选位置，单位为设计像素；未指定时根据 SkillUIInfo 推导。</summary>
    public virtual Vector2? ConstellationPosition => null;

    /// <summary>各等级的解锁要求。None 或 null 表示没有额外消耗，但仍会应用 MaxLevel。</summary>
    public virtual SkillUnlockCondition GetConstellationUpgradeCondition(int nextLevel) => SkillUnlockCondition.ByItemsAndSkillPoint(20);//SkillUnlockCondition.ByItemsAndSkillPoint(20,[new SkillUnlockItem(ItemID.Wood,10)]);

    /// <summary>可选的数值，在指定等级下计算，与魔力/冷却同样以大号数字两列显示。不自动推导伤害。</summary>
    public virtual (string Label, string Value, string Unit)[] GetConstellationStats(int level) => [];

    /// <summary>用于升级比较的实际当前值和下一等级值。在定义成长效果前保持为空。</summary>
    public virtual (string Label, string Current, string Next, string Unit)[] GetConstellationUpgradePreview(int nextLevel) => [];

    /// <summary>魔力不足时能否通过灰之魔女消耗生命补足施法消耗。为 false 时描述末尾自动追加通用警告。</summary>
    public virtual bool AllowsLifeConversion => true;

    /// <summary>在技能描述后追加由技能属性决定的通用说明，技能栏提示与星图详情共用。</summary>
    protected string AppendDescriptionNotes(string description) => AllowsLifeConversion
        ? description
        : description + "\n" + Language.GetTextValue("Mods.伊蕾娜.SkillInfo.Snippets.NoLifeConversion");

    public virtual string GetConstellationDescription()
    {
        string key = $"Mods.伊蕾娜.SkillInfo.SkillDesc.{GetType().Name}";
        if (!Language.Exists(key)) return LocalizeUI("MissingDescription");
        try
        {
            string text = Language.GetText(key).WithFormatArgs(SkillDescriptionArgs).Value;
            return string.IsNullOrWhiteSpace(text) ? LocalizeUI("MissingDescription") : AppendDescriptionNotes(text);
        }
        catch (FormatException)
        {
            return LocalizeUI("InvalidDescription");
        }
    }
}
