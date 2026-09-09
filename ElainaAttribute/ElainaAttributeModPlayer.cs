using System;
using KL.AttributeSystem;
using KL.SkillSystem;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using 伊蕾娜.ElainaModSkills;
using 伊蕾娜.ElainaModSkills.Skills.AshenWitch;

namespace 伊蕾娜.ElainaAttribute;

/// <summary>
/// 伊蕾娜的独特魔力资源。上限由最终原版最大 mana 按属性换算，当前值独立保存。
/// </summary>
public class ElainaAttributeModPlayer : ModPlayer, IAttributeProvider
{
    public AttributeComponent Attributes { get; } = new();

    public delegate void OnMagicPointChangedHandler(float oldMagicPoint, float magicPoint);

    public event OnMagicPointChangedHandler MagicPointChanged;

    private float magicPoint;
    private float maxMagicPoint;
    private float loadedMagicPoint;
    private double recoveryRemainder;
    private int battleTicksRemaining;
    private int recoveryPauseTicks;
    private bool hasLoadedMagicPoint;
    private bool magicPointInitialized;

    public float MagicPoint
    {
        get => magicPoint;
        private set
        {
            float clampedValue = Math.Clamp(value, 0f, MaxMagicPoint);
            float oldMagicPoint = magicPoint;
            magicPoint = clampedValue;
            if (Math.Abs(magicPoint - oldMagicPoint) > 0.001f)
            {
                MagicPointChanged?.Invoke(oldMagicPoint, magicPoint);
            }
        }
    }

    public float MaxMagicPoint => maxMagicPoint;

    public override void ResetEffects()
    {
        Attributes.ResetForTick();
        base.ResetEffects();
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
            return skillPlayer.TryGetUnlockedModSkill<AshenWitchSkill>(out AshenWitchSkill skill)
                && skill.BasicStatus == Skill.SKillBasicStatus.UnLock
                && skill.IsEnabled;
        }
    }

    public override void PostUpdateMiscEffects()
    {
        base.PostUpdateMiscEffects();

        float calculatedMaxMagicPoint = Math.Max(0f,
            Player.statManaMax2 * Math.Max(0f,
                ElainaMagicAttributes.VanillaManaToMagicPointRatio));
        Attributes.AddBase(ElainaMagicAttributes.UniqueMagic, calculatedMaxMagicPoint);
        maxMagicPoint = Math.Max(0f,
            Attributes.GetFinalValue(ElainaMagicAttributes.UniqueMagic));

        if (!magicPointInitialized && maxMagicPoint > 0f)
        {
            MagicPoint = hasLoadedMagicPoint
                ? loadedMagicPoint
                : maxMagicPoint;
            magicPointInitialized = true;
        }
        else if (magicPointInitialized)
        {
            MagicPoint = magicPoint;
        }
    }

    public override void PostUpdate()
    {
        Attributes.Commit();
        base.PostUpdate();

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
            ? Math.Max(0f, MaxMagicPoint - MagicPoint)
            : MaxMagicPoint;
        RegenMagicPoint(recoveryBase * Math.Max(0f, recoveryPercent) / 100f);
    }

    /// <summary>
    /// 检查并按需支付独特魔力。魔力不足时默认允许灰之魔女进行生命转化。
    /// </summary>
    public bool ConsumeMagicPoint(float cost, bool consume = true, bool allowLifeConversion = true)
    {
        if (float.IsNaN(cost) || float.IsInfinity(cost) || cost <= 0f)
        {
            return true;
        }

        if (!UniqueMagicEnabled || MaxMagicPoint <= 0f)
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

        if (!allowLifeConversion || !TryGetLifeConversion(cost, out int lifeCost, out float magicRecovery))
        {
            return false;
        }

        if (!consume)
        {
            return true;
        }

        Player.statLife -= lifeCost;
        MagicPoint += magicRecovery;
        MagicPoint -= cost;
        InBattleState();

        if (Main.netMode == NetmodeID.MultiplayerClient)
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

        float consumed = Math.Min(amount, MagicPoint);
        if (consumed <= 0f)
        {
            return 0f;
        }

        MagicPoint -= consumed;
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

    private bool TryGetLifeConversion(float cost, out int lifeCost, out float magicRecovery)
    {
        lifeCost = 0;
        magicRecovery = 0f;

        if (!UniqueMagicEnabled || Player.dead || MaxMagicPoint <= 0f || cost <= MagicPoint)
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

        int stepCount = Math.Max(1, (int)MathF.Ceiling((cost - MagicPoint) / magicPerStep));
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
