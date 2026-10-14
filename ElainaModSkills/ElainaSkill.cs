using System;
using System.Diagnostics;
using KL.DamageSystem;
using KL.Drawing;
using KL.Drawing.Snippets;
using KL.SkillSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using 伊蕾娜.ElainaAttribute;
using 伊蕾娜.ElainaModSkills.ElainaDamageClass;

namespace 伊蕾娜.ElainaModSkills;

public abstract partial class ElainaSkill : ModSkill
{
    protected Vector2 WandCenter =>Main.LocalPlayer.MountedCenter+ new Vector2(1, 0).RotatedBy(Main.LocalPlayer.itemRotation)*45f;

    protected Vector2 WandDirection => new Vector2(1, 0).RotatedBy((Main.MouseWorld-Main.LocalPlayer.MountedCenter).ToRotation());
    //图标分为底层和光效层
    public virtual Asset<Texture2D> ExtraIcon
    {
        get
        {
            return _extraIcon ??= ModContent.Request<Texture2D>(SkillTexturePath+"_Mask", AssetRequestMode.ImmediateLoad);
        }
        set { _extraIcon = value; }
    }
    protected Asset<Texture2D> _extraIcon;

    /// <summary>
    /// 释放技能消耗的魔力点数。
    /// </summary>
    public float MagicPointCost = 0;

    /// <summary>
    /// 按技能类名自动使用 JSON 配置；特殊技能可关闭或覆写对应接口。
    /// </summary>
    protected virtual bool UsesLiveBalance => true;
    protected virtual string BalanceId => GetType().Name;
    
    /// <summary>
    /// 配置存在时自动限制当前可升级等级；不裁剪已学等级，不代替首次解锁条件。
    /// </summary>
    public override int MaxLevel => GetConfiguredMaxLevel();

    protected int GetConfiguredMaxLevel()
    {
        var config = SkillBalanceSystem.Current;
        if (!UsesLiveBalance || Main.gameMenu || Player == null
            || !config.Skills.TryGetValue(BalanceId, out var entry))
            return base.MaxLevel;
        int characterLevel = Player.GetModPlayer<ElainaStatePlayer>().GetLevel();
        return Math.Max(1, config.GetSkillLevelCap(entry, characterLevel));
    }

    protected void ApplyBalanceConfiguration()
    {
        if (!UsesLiveBalance) return;
        if (SkillBalanceSystem.TryGetSkill(BalanceId, out SkillBalanceEntry entry))
        {
            MagicPointCost = (float)entry.ManaCost;
            MaxCD = (float)entry.BaseCooldown;
            MaxStack = entry.MaxCharges;
            if (Stack > MaxStack) Stack = MaxStack;
            // 上限提高时若没有在恢复，从完整冷却开始恢复，否则框架会在下一帧白给一层。
            if (Stack < MaxStack && CurrentCD <= 0)
                CurrentCD = CooldownDuration = MaxCD;
        }
        // 不修改正在进行的 CurrentCD / CooldownDuration：已开始的冷却继续，新一轮才使用新的基准 CD。
    }

    /// <summary>
    /// 按技能实际等级返回已应用装备增伤的单发伤害；未学习时预览 Lv1，可指定等级预览。
    /// </summary>
    protected int GetConfiguredDamage(int? hitCount = null, int? skillLevel = null) =>
        SkillBalanceSystem.GetPlayerDamage(Player, BalanceId, skillLevel ?? Math.Max(1, Level), ModContent.GetInstance<ElainaBasicDamage>(), hitCount);

    /// <summary>按技能实际等级读取基础周期 DPS，不含装备；可指定等级预览。</summary>
    protected float GetConfiguredDps(int? skillLevel = null) =>
        SkillBalanceSystem.GetBaseDps(skillLevel ?? Math.Max(1, Level), BalanceId);

    /// <summary>
    /// 耗蓝、周期、发数和伤害使用同一快照，便于新技能在一次施法中读取。
    /// </summary>
    protected bool TryGetConfiguredBalance(out SkillBalanceResult result, int? hitCount = null, int? skillLevel = null) =>
        SkillBalanceSystem.TryGetPlayerBalance(Player, BalanceId, skillLevel ?? Math.Max(1, Level), ModContent.GetInstance<ElainaBasicDamage>(), out result, hitCount);

    public override void Initialize()
    {
        if (UsesLiveBalance && SkillBalanceSystem.TryGetSkill(BalanceId, out SkillBalanceEntry entry))
            Stack = Math.Min(entry.InitialCharges, entry.MaxCharges);
        ApplyBalanceConfiguration();
        base.Initialize();
    }

    public override void DeserializeData(TagCompound tag)
    {
        base.DeserializeData(tag);
        // 存档里的 MaxCD / MaxStack 是旧版本数值，以当前配置为准，只保留层数与剩余冷却。
        ApplyBalanceConfiguration();
    }

    /// <summary>
    /// 技能初始化（新角色、重置技能）时是否自动习得。
    /// </summary>
    public virtual bool LearnedOnInitialize(Player player) => false;

    /// <summary>初始化习得后默认装备的技能栏位置，-1 表示不装备。</summary>
    public virtual int InitialSkillSlot => -1;

    public override void ResetEffects(Player player)
    {
        base.ResetEffects(player);
    }

    /// <summary>
    /// 按当前技能对应的曲线获取指定进度的技能伤害。
    /// </summary>
    protected int GetSkillDamage(float bossState)
    {
        return DpsHelper.GetSkillDamage(GetType().Name, bossState);
    }

    /// <summary>
    /// 按当前技能对应的曲线获取当前默认进度的技能伤害。
    /// </summary>
    protected int GetSkillDamage()
    {
        return GetSkillDamage(1f);
    }

    public override SkillUnlockCondition UnlockCondition { get; set; } = DefaultUnlockCost;

    /// <summary>首次习得的默认消耗。</summary>
    protected static SkillUnlockCondition DefaultUnlockCost => SkillUnlockCondition.
        ByItemsAndSkillPoint(20,[new SkillUnlockItem(ItemID.Wood,10)]);// SkillUnlockCondition.ByItemsAndSkillPoint(10,[new SkillUnlockItem(ItemID.Wood,10),new SkillUnlockItem(ItemID.IronBar,10)]);

    /// <summary>习得条件：角色当前等级达到指定等级。</summary>
    protected static SkillUnlockCondition RequireCharacterLevel(int level) => SkillUnlockCondition.Custom(
        (skillPlayer, _) => skillPlayer?.Player.GetModPlayer<ElainaStatePlayer>().GetLevel() >= level,
        description: (_, _) => Language.GetTextValue("Mods.伊蕾娜.SkillInfo.UnlockConditions.CharacterLevel", level));

    /// <summary>习得条件：角色等级上限（由 Boss 进度决定）达到指定等级。</summary>
    protected static SkillUnlockCondition RequireCharacterLevelCap(int levelCap) => SkillUnlockCondition.Custom(
        (skillPlayer, _) => skillPlayer?.Player.GetModPlayer<ElainaStatePlayer>().GetMaxLevel() >= levelCap,
        description: (_, _) => Language.GetTextValue("Mods.伊蕾娜.SkillInfo.UnlockConditions.CharacterLevelCap", levelCap));

    /// <summary>习得条件：角色等级上限达到 JSON 的解锁阶段上限；每次判断和显示都读取当前配置。</summary>
    protected static SkillUnlockCondition RequireConfiguredCharacterLevelCap(string skillId, int fallbackLevelCap)
    {
        int GetRequiredCap() => SkillBalanceSystem.TryGetSkill(skillId, out var entry) ? entry.UnlockStageCap : fallbackLevelCap;
        return SkillUnlockCondition.Custom(
            (skillPlayer, _) => skillPlayer?.Player.GetModPlayer<ElainaStatePlayer>().GetMaxLevel() >= GetRequiredCap(),
            description: (_, _) => Language.GetTextValue("Mods.伊蕾娜.SkillInfo.UnlockConditions.CharacterLevelCap", GetRequiredCap()));
    }

    /// <summary>习得条件：指定技能已习得且等级达到指定等级。</summary>
    protected static SkillUnlockCondition RequireSkillLevel<TSkill>(int level) where TSkill : ElainaSkill => SkillUnlockCondition.Custom(
        (skillPlayer, _) => skillPlayer?.UnlockedSkill != null
            && skillPlayer.UnlockedSkill.TryGetValue(typeof(TSkill).Name, out Skill skill) && skill.Level >= level,
        description: (_, _) => Language.GetTextValue("Mods.伊蕾娜.SkillInfo.UnlockConditions.SkillLevel",
            Language.GetTextValue($"Mods.伊蕾娜.SkillInfo.SkillName.{typeof(TSkill).Name}"), level));

    /// <summary>
    /// 判断技能是否可以从技能面板拖入技能栏。被动技能不能拖入技能栏。
    /// </summary>
    public override bool CanDragInSkillPanel()
    {
        if(IsPassiveSkill) return false;
        return base.CanDragInSkillPanel();
    }

    /// <summary>
    /// 在技能冷却时间更新前调用，用于决定是否继续更新冷却时间。
    /// </summary>
    public override bool PreUpdateCD()
    {
#if DEBUG
        // 配置只在 Debug 下热重载，同步耗蓝、基准 CD 与层数上限；Release 配置加载后不变。
        ApplyBalanceConfiguration();
#endif
        return base.PreUpdateCD();
    }

    /// <summary>
    /// 判断技能是否可以使用，并在使用前消耗所需的魔力点数。
    /// </summary>
    public override bool CanUseSkill()
    {
        if (UsesLiveBalance && !SkillBalanceSystem.TryGetSkill(BalanceId, out _)) return false;
        ElainaAttributeModPlayer attributePlayer = Player.GetModPlayer<ElainaAttributeModPlayer>();
        if (MagicPointCost > 0 && !attributePlayer.ConsumeMagicPoint(MagicPointCost, false))
        {
            return false;
        }

        return base.CanUseSkill();
    }

    /// <summary>
    /// 在绘制技能图标后绘制冷却时间等附加信息。
    /// </summary>
    public override void PostDrawSkillIcon(Vector2 position, Vector2 scale,Color color, Effect effect = null)
    {
        //BasicStatus = Skill.SKillBasicStatus.Learned;
        /*if (ExtraIcon != null)
        {
            EndBeginDrawUI(1,1,shader: effect);
            DrawInScreen(ExtraIcon.Value,position ,color,scale);

        }*/
        // 冷却倒计时与层数由技能栏槽位统一绘制（SkillBarDrawing.DrawSkillState）。
        EndBeginDrawUI(ss:SamplerState.LinearClamp);
        base.PostDrawSkillIcon(position, scale,color, effect);
    }

    /// <summary>
    /// 解锁技能，并同步更新伊蕾娜玩家的技能数据。
    /// </summary>
    public override void OnUnlockSkill()
    {
        ElainaSkillModPlayer.SkillModPlayer.UnlockSkill(Skill);
        base.OnUnlockSkill();
    }

    /// <summary>
    /// 获取技能描述所需的本地化格式化参数。
    /// </summary>
    protected virtual object[] SkillDescriptionArgs => Array.Empty<object>();

    /// <summary>
    /// 获取技能的本地化名称、等级和描述文本。
    /// </summary>
    public override bool TryGetToolTip(ref string name, ref string level, ref string desc)
    {
        base.TryGetToolTip(ref name, ref level, ref desc);
        name = Language.GetText($"Mods.伊蕾娜.SkillInfo.SkillName.{GetType().Name}").Value;
        string type = Language.GetText($"Mods.伊蕾娜.SkillInfo.SkillType.Active").Value;
        if(IsPassiveSkill) type = Language.GetText($"Mods.伊蕾娜.SkillInfo.SkillType.Passive").Value;
        
        string maxCD = MaxCD <0 ? "--" : $"{MaxCD}s";
        string magicPointName = Language.GetText(
            "Mods.伊蕾娜.SkillInfo.Snippets.MagicPoint").Value;
        string magicPointCost = MagicPointCost < 0 ? "--" : $"{MagicPointCost:0.##} {magicPointName}";
        desc = Language.GetText($"Mods.伊蕾娜.SkillInfo.SkillTotalInfo").WithFormatArgs(type, maxCD, magicPointCost).Value;
        desc += Language.GetText($"Mods.伊蕾娜.SkillInfo.SkillDesc.{GetType().Name}")
            .WithFormatArgs(SkillDescriptionArgs).Value;
        //level = $"Lv. {Level}";
        /*name = GetType().Name;
        level = $"Lv. {Level}";
        desc = "造成100" +ElementType.Fire.GetIcon(offsetY:2) + "火元素伤害";
        toolTipWidth = 200;*/
        //ElementType.Fire.GetIcon(offsetY:2)
        return true;
    }

    /// <summary>
    /// 在绘制技能图标前调用，用于准备图标绘制并决定是否继续绘制。
    /// </summary>
    public override bool PreDrawSkillIcon(Vector2 position, Vector2 scale,Color color, Effect effect = null)
    {
        //EndBeginDrawUI(2,1,shader:effect);
        return base.PreDrawSkillIcon(position, scale,color, effect);
    }
    
    
}
