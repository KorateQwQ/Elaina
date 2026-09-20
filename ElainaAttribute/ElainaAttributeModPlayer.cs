using System;
using KL.AttributeSystem;
using KL.SkillSystem;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader.IO;
using 伊蕾娜.ElainaModSkills;
using 伊蕾娜.ElainaModSkills.Skills.AshenWitch;

namespace 伊蕾娜.ElainaAttribute;

/// <summary>
/// 在通用 RPG 属性组件中接入伊蕾娜魔力规则，上限由原版魔力和灰之魔女牺牲的生命换算。
/// 急速与当前魔力共用继承得到的组件，本类只负责伊蕾娜的专属业务。
/// </summary>
public class ElainaAttributeModPlayer : RPGAttributeModPlayer
{
    private float loadedMagicPoint;
    private double recoveryRemainder;
    private int battleTicksRemaining;
    private int recoveryPauseTicks;
    private bool hasLoadedMagicPoint;
    private bool magicPointInitialized;

    /// <summary>
    /// 伊蕾娜的魔力属性
    /// </summary>
    public float MagicPoint
    {
        get => GetAttributeValue(ElainaMagicAttributes.MagicPoint);
        private set => Attributes.SetBase(ElainaMagicAttributes.MagicPoint, value);
    }
    

    /// <summary>独特魔力上限由原版最大魔力与灰之魔女减少的生命上限换算，不作为独立的 KL 属性保存。</summary>
    public float MaxMagicPoint
    {
        get
        {
            float maximum = Player.statManaMax2 * ElainaMagicAttributes.VanillaManaToMagicPointRatio;
            if (!Player.GetModPlayer<ElainaModplayer>().Elaina)
            {
                return 0;
            }

            ElainaSkillModPlayer skillPlayer = Player.GetModPlayer<ElainaSkillModPlayer>();
            if (skillPlayer.TryGetUnlockedModSkill(out AshenWitchSkill skill))
            {
                maximum += Math.Max(0, skill.LostMaxLife)
                    * ElainaMagicAttributes.LostMaxLifeToMagicPointRatio;
            }

            return maximum;
        }
    }

    public override void Initialize()
    {
        base.Initialize();
        AttributeChangeEvent magicEvent = Attributes.GetAttributeChangeEvent(ElainaMagicAttributes.MagicPoint);
        magicEvent.PreAttributeChange -= OnPreMagicPointChange;
        magicEvent.PreAttributeChange += OnPreMagicPointChange;
    }

    /// <summary>恢复、消耗和加载存档统一经过此钩子，按原版最大魔力换算出的上限裁剪。</summary>
    private void OnPreMagicPointChange(object sender, PreAttributeChangeEventArgs args)
    {
        args.BaseValue = Math.Clamp(args.BaseValue, 0f, MaxMagicPoint);
    }

    public bool InBattle { get; private set; }

    /// <summary>
    /// 灰之魔女已解锁且开启时，伊蕾娜才实际使用独特魔力。
    /// </summary>
    public bool UniqueMagicEnabled
    {
        get
        {
            if (!Player.GetModPlayer<ElainaModplayer>().Elaina)
            {
                return false;
            }

            ElainaSkillModPlayer skillPlayer = Player.GetModPlayer<ElainaSkillModPlayer>();
            return skillPlayer.TryGetUnlockedModSkill(out AshenWitchSkill skill)
                && skill.BasicStatus == Skill.SKillBasicStatus.UnLock
                && skill.IsEnabled;
        }
    }

    public override void PostUpdate()
    {
        base.PostUpdate();

        // 等原版完成本帧属性计算后恢复资源，避免在每帧重置的中间阶段初始化或裁剪。
        float maximum = MaxMagicPoint;
        if (!magicPointInitialized && maximum > 0f)
        {
            MagicPoint = hasLoadedMagicPoint ? loadedMagicPoint : maximum;
            magicPointInitialized = true;
        }
        else if (magicPointInitialized && MagicPoint > maximum)
        {
            // 卸下装备等情况会降低上限，即使没有消耗或恢复也需要裁剪；仍通过 KL 写入和通知。
            MagicPoint = maximum;
        }

        if (battleTicksRemaining > 0)
        {
            battleTicksRemaining--;
        }
        else
        {
            InBattle = false;
        }

        if (recoveryPauseTicks > 0)
        {
            recoveryPauseTicks--;
        }

        if (!Player.dead && magicPointInitialized && recoveryPauseTicks <= 0 && MaxMagicPoint > 0f)
        {
            float recoveryPerSecond = GetMagicPointRecovery();
            recoveryRemainder += recoveryPerSecond / 60f;
            float recoveryThisTick = (float)recoveryRemainder;
            if (recoveryThisTick > 0f)
            {
                MagicPoint += recoveryThisTick;
                recoveryRemainder -= recoveryThisTick;
            }
        }

    }

    public override void PostHurt(Player.HurtInfo info)
    {
        InBattleState();
        PauseMagicRecovery();
        base.PostHurt(info);
    }

    public override void OnHitAnything(float x, float y, Entity victim)
    {
        InBattleState(victim is NPC npc && npc.immortal ? 180 : -1);
        base.OnHitAnything(x, y, victim);
    }

    public void RegenMagicPoint(float recovery)
    {
        if (recovery > 0f)
        {
            MagicPoint += recovery;
        }
    }

    public void RegenPercentMagicPoint(float recoveryPercent, bool basedOnMissingMagic = false)
    {
        float recoveryBase = basedOnMissingMagic
            ? MaxMagicPoint - MagicPoint
            : MaxMagicPoint;
        RegenMagicPoint(recoveryBase * Math.Max(0f, recoveryPercent) / 100f);
    }

    /// <summary>
    /// 检查并按需支付独特魔力。先使用药水槽，不足时再允许灰之魔女进行生命转化。
    /// </summary>
    public bool ConsumeMagicPoint(float cost, bool consume = true, bool allowLifeConversion = true)
    {
        if (consume && Player.whoAmI == Main.myPlayer && Main.netMode != NetmodeID.Server)
        {
            PrintText($"[伊蕾娜] 本次施法需要消耗 {cost:0.##} 点[klicon/16,0:伊蕾娜/Snippets/MagicStarIcon][c/4C91FFFF:魔力]");
        }

        if (float.IsNaN(cost) || float.IsInfinity(cost) || cost <= 0f)
        {
            return true;
        }

        if (Player.dead || !UniqueMagicEnabled || MaxMagicPoint <= 0f || cost > MaxMagicPoint)
        {
            return false;
        }

        if (MagicPoint >= cost)
        {
            if (consume)
            {
                MagicPoint -= cost;
                InBattleState();
            }

            return true;
        }

        // 预检查不得修改资源；只有药水槽与生命转化足以支付本次费用时才提交消耗。
        var elixirPlayer = Player.GetModPlayer<ElainaManaElixirPlayer>();
        float magicBefore = MagicPoint;
        float magicAfterElixirs = elixirPlayer.PreviewRecovery(magicBefore, cost, out int chargesNeeded);
        int lifeCost = 0;
        float magicRecovery = 0f;
        if (magicAfterElixirs < cost
            && (!allowLifeConversion || !TryGetLifeConversion(cost, magicAfterElixirs, out lifeCost, out magicRecovery)))
        {
            return false;
        }

        if (!consume)
        {
            return true;
        }

        Player.statLife -= lifeCost;
        // 扣费与补充一起结算，仅对最终余额裁剪，避免先回蓝造成提前溢出。
        MagicPoint = magicAfterElixirs - cost + magicRecovery;
        float elixirRecovery = Math.Min(magicAfterElixirs - magicBefore,
            cost + MaxMagicPoint - magicBefore);
        elixirPlayer.ConsumeCharges(chargesNeeded, elixirRecovery);
        InBattleState();

        if (lifeCost > 0 && Main.netMode == NetmodeID.MultiplayerClient)
        {
            NetMessage.SendData(MessageID.PlayerLifeMana, -1, -1, null, Player.whoAmI);
        }

        return true;
    }

    /// <summary>
    /// 仅从当前独特魔力中扣除资源，不允许生命转化。用于魔力护盾。
    /// </summary>
    public float ConsumeAvailableMagicPoint(float amount)
    {
        if (amount <= 0f || !magicPointInitialized)
        {
            return 0f;
        }

        float consumed = -Attributes.AddBase(ElainaMagicAttributes.MagicPoint, -amount);
        if (consumed <= 0f)
        {
            return 0f;
        }

        InBattleState();
        PauseMagicRecovery();
        return consumed;
    }

    public float GetMagicPointRecovery()
    {
        if (Player.dead || recoveryPauseTicks > 0 || MaxMagicPoint <= 0f)
        {
            return 0f;
        }

        if (!InBattle)
        {
            float refillSeconds = Math.Max(0.01f,
                ElainaMagicAttributes.OutOfCombatRefillSeconds);
            return MaxMagicPoint / refillSeconds;
        }

        return Math.Max(0f, ElainaMagicAttributes.CombatRecoveryPerSecond)
            + MaxMagicPoint * Math.Max(0f,
                ElainaMagicAttributes.CombatMaxMagicPointRecoveryPercentPerSecond);
    }

    public void PauseMagicRecovery()
    {
        recoveryPauseTicks = Math.Max(recoveryPauseTicks,
            ElainaMagicAttributes.ToTicks(ElainaMagicAttributes.HurtRecoveryPauseSeconds));
        recoveryRemainder = 0d;
    }

    /// <summary>
    /// 进入战斗。传入负数时使用属性中的默认战斗时长。
    /// </summary>
    public void InBattleState(int battleTime = -1)
    {
        int duration = battleTime < 0
            ? ElainaMagicAttributes.ToTicks(
                ElainaMagicAttributes.CombatStateDurationSeconds)
            : Math.Max(0, battleTime);
        InBattle = duration > 0;
        battleTicksRemaining = Math.Max(battleTicksRemaining, duration);
    }

    private bool TryGetLifeConversion(float cost, float availableMagic, out int lifeCost, out float magicRecovery)
    {
        lifeCost = 0;
        magicRecovery = 0f;

        if (!UniqueMagicEnabled || Player.dead || MaxMagicPoint <= 0f || cost <= availableMagic)
        {
            return false;
        }

        float lifePercent = Math.Max(0f,
            ElainaMagicAttributes.LifePercentPerConversionStep);
        float magicPercent = Math.Max(0f,
            ElainaMagicAttributes.MagicPointPercentPerConversionStep);
        float magicPerStep = MaxMagicPoint * magicPercent / 100f;
        if (lifePercent <= 0f || magicPerStep <= 0f)
        {
            return false;
        }

        int stepCount = Math.Max(1, (int)MathF.Ceiling((cost - availableMagic) / magicPerStep));
        int lifePerStep = Math.Max(1, (int)MathF.Ceiling(Player.statLifeMax2 * lifePercent / 100f));
        lifeCost = lifePerStep * stepCount;
        magicRecovery = magicPerStep * stepCount;

        return lifeCost > 0 && lifeCost < Player.statLife;
    }

    public override void SaveData(TagCompound tag)
    {
        tag["magicPoint"] = MagicPoint;
        base.SaveData(tag);
    }

    public override void LoadData(TagCompound tag)
    {
        if (tag.TryGet("magicPoint", out float savedMagicPoint))
        {
            loadedMagicPoint = savedMagicPoint;
            hasLoadedMagicPoint = true;
        }

        base.LoadData(tag);
    }
}
