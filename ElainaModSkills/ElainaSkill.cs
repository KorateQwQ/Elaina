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
using 伊蕾娜.ElainaAttribute;

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
    /// 是否已经装备在技能栏中，可以用来判定是否显示cd。
    /// </summary>
    public bool SelectedInSkillBar = false;
    
    /// <summary>
    /// 释放技能消耗的魔力点数。
    /// </summary>
    public int MagicPointCost = 0;

    /// <summary>按技能类名自动使用 JSON 配置；特殊技能可关闭或覆写对应接口。</summary>
    protected virtual bool UsesLiveBalance => true;
    protected virtual string BalanceId => GetType().Name;
    protected virtual DamageClass BalanceDamageClass => DamageClass.Magic;

    /// <summary>配置存在时自动限制当前可升级等级；不裁剪已学等级，不代替首次解锁条件。</summary>
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
            MagicPointCost = entry.ManaCost;
            MaxCD = (float)entry.BaseCooldown;
        }
        // 不修改 CurrentCD / CooldownDuration：已开始的冷却继续，新一轮才使用新的基准 CD。
    }

    /// <summary>按技能实际等级返回已应用装备增伤的单发伤害；未学习时预览 Lv1，可指定等级预览。</summary>
    protected int GetConfiguredDamage(int? hitCount = null, int? skillLevel = null) =>
        SkillBalanceSystem.GetPlayerDamage(Player, BalanceId, skillLevel ?? Math.Max(1, Level), BalanceDamageClass, hitCount);

    /// <summary>按技能实际等级读取基础周期 DPS，不含装备；可指定等级预览。</summary>
    protected float GetConfiguredDps(int? skillLevel = null) =>
        SkillBalanceSystem.GetBaseDps(skillLevel ?? Math.Max(1, Level), BalanceId);

    /// <summary>耗蓝、周期、发数和伤害使用同一快照，便于新技能在一次施法中读取。</summary>
    protected bool TryGetConfiguredBalance(out SkillBalanceResult result, int? hitCount = null, int? skillLevel = null) =>
        SkillBalanceSystem.TryGetPlayerBalance(Player, BalanceId, skillLevel ?? Math.Max(1, Level), BalanceDamageClass, out result, hitCount);

    public override void Initialize()
    {
        ApplyBalanceConfiguration();
        base.Initialize();
    }

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

    public override SkillUnlockCondition UnlockCondition { get; set; } = SkillUnlockCondition.
        ByItemsAndSkillPoint(20,[new SkillUnlockItem(ItemID.Wood,10)]);// SkillUnlockCondition.ByItemsAndSkillPoint(10,[new SkillUnlockItem(ItemID.Wood,10),new SkillUnlockItem(ItemID.IronBar,10)]);

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
        EndBeginDrawUI(ss:SamplerState.LinearClamp);
        if (SelectedInSkillBar&&CurrentCD > 0&&ElainaSkillManager.ShowCD)
        {
            DynamicSpriteFont font = FontManager.NotoSerifSC.Value;
            string text = $"{CurrentCD:F1}";
            Main.spriteBatch.DrawString(font, text, position, Color.White,0,
                font.MeasureString(text)*new Vector2(0.5f,0.5f),22f / 48f,SpriteEffects.None,0);
        }
        SelectedInSkillBar = false;
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
        string magicPointCost = MagicPointCost < 0 ? "--" : $"{MagicPointCost} {magicPointName}";
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
