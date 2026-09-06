using System;
using KL.SkillSystem;
using KL.SkillSystem.SilkyUI;
using KL.Utils;
using Terraria.ID;
using 伊蕾娜.ElainaAttribute;

namespace 伊蕾娜.ElainaModSkills.Skills.AshenWitch;

[SkillUIInfo(State = 0, Pixels = 0)]
public class AshenWitchSkill: ElainaSkill
{
    //public const int BonusMana = 40;

    public override bool IsPassiveSkill => true;
    public override bool IsToggleable => true;

    public override SkillUnlockCondition UnlockCondition { get; set; } =
        SkillUnlockCondition.None;

    //每步至少消耗血量的百分比
    private const float LifePercentPerStep = 1f;
    //每步可恢复的魔力的懂百分比
    private const float MagicPointPercentPerStep = 1f;

    public override void Initialize()
    {
        MaxCD = -1;
        MagicPointCost = -1;
        KLGameStateManager.OnBossLoot += OnKillBoss;
        ElainaAttributeModPlayer.MagicPointInsufficient -= OnMagicPointInsufficient;
        ElainaAttributeModPlayer.MagicPointInsufficient += OnMagicPointInsufficient;
        base.Initialize();
    }

    private bool OnMagicPointInsufficient(ElainaAttributeModPlayer attributePlayer, float cost, bool consume)
    {
        Player player = attributePlayer.Player;
        if (!CanConvertLifeToMagicPoint(player, attributePlayer, cost)) return false;

        if (consume)
        {
            float lackMagicPoint = cost - attributePlayer.MagicPoint;
            int lifeCost = GetLifeCost(player, attributePlayer, cost);
            PrintText($"灰之魔女：魔力不足，需求魔力：{cost:0.#}，缺少魔力：{lackMagicPoint:0.#}，消耗生命：{lifeCost}");
            player.statLife -= lifeCost;
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetMessage.SendData(MessageID.PlayerLifeMana, -1, -1, null, player.whoAmI);
            }
            attributePlayer.MagicPoint += GetMagicPointRecovery(attributePlayer, cost);
        }

        return true;
    }

    private bool CanConvertLifeToMagicPoint(Player player, ElainaAttributeModPlayer attributePlayer, float cost)
    {
        if (!player.active || player.dead) return false;
        if (!player.GetModPlayer<ElainaModplayer>().Elaina) return false;
        if (BasicStatus != Skill.SKillBasicStatus.UnLock) return false;
        // 只有在技能开启时才能转换生命为魔力
        if (!IsEnabled) return false;
        if (attributePlayer.MaxMagicPoint <= 0f || cost <= attributePlayer.MagicPoint || cost > attributePlayer.MaxMagicPoint) return false;

        int lifeCost = GetLifeCost(player, attributePlayer, cost);
        return lifeCost > 0 && player.statLife > lifeCost;
    }
    
    private static int GetLifeCost(Player player, ElainaAttributeModPlayer attributePlayer, float cost)
    {
        return (int)Math.Ceiling(player.statLifeMax2 * LifePercentPerStep / 100f) * GetConversionStepCount(attributePlayer, cost);
    }

    private static float GetMagicPointRecovery(ElainaAttributeModPlayer attributePlayer, float cost)
    {
        return attributePlayer.MaxMagicPoint * MagicPointPercentPerStep / 100f * GetConversionStepCount(attributePlayer, cost);
    }

    private static int GetConversionStepCount(ElainaAttributeModPlayer attributePlayer, float cost)
    {
        float needMagicPoint = cost - attributePlayer.MagicPoint;
        return (int)Math.Ceiling(needMagicPoint / (attributePlayer.MaxMagicPoint * MagicPointPercentPerStep / 100f));
    }

    private void OnKillBoss(NPC self, List<int> killers, int realMaxHp)
    {
        List<int> materialTypes = KLGameStateManager.FindMaterialsByBoss(self);
        List<int> weaponTypes = KLGameStateManager.FindWeaponsByBoss(self, KLGameStateManager.StateItemType.MagicWeapon);
        if (materialTypes.Count == 0 && weaponTypes.Count == 0)
        {
            return;
        }

        List<SkillUnlockItem> unlockItems = new();
        foreach (int itemType in materialTypes)
        {
            unlockItems.Add(new SkillUnlockItem(itemType, 1));
        }

        foreach (int itemType in weaponTypes)
        {
            if (!materialTypes.Contains(itemType))
            {
                unlockItems.Add(new SkillUnlockItem(itemType, 1));
            }
        }

        UnlockCondition = SkillUnlockCondition.ByItemsAndSkillPoint(0, unlockItems.ToArray());
    }

    public override void ResetEffects(Player player)
    {
        if (player.GetModPlayer<ElainaModplayer>().Elaina)
        {
            BasicStatus = Skill.SKillBasicStatus.UnLock;
            if (Level < 1) Level = 1;
        }
        base.ResetEffects(player);
    }
    public override void UpdateEquips(Player player)
    {
        // 只有在技能开启时才应用效果
        if (!IsEnabled)
        {
            return;
        }

        //player.GetModPlayer<ElainaAttributeModPlayer>().MaxMagicPoint+=1*Player.statManaMax2;
        Player.buffImmune[BuffID.ManaSickness] = true;

        int extraHP = (int)(Player.statLifeMax2 -100);
        int reduceHP = (int)(extraHP * 0.5f);
        float extraMultiplier =  15f ;
        float extraManaPercent = 1 + Math.Max(0, reduceHP * extraMultiplier * 0.001f);

        Player.statLifeMax2 = 100 + reduceHP;
        //PrintText(extraHP);
        /*if (Player.statLifeMax2 > 100 && Player.statLifeMax2 <= 400f)
        {
            Player.statLifeMax2 = 100 + (int)((Player.statLifeMax2 - 100) * 0.334f);
        }
        else if (Player.statLifeMax2 > 400)
        {
            Player.statLifeMax2 = 200 + (int)((Player.statLifeMax2 - 400) * 0.5f);
        }*/

        float extraMana = (int)(Player.statManaMax2 * extraManaPercent)-Player.statManaMax2;
        Player.statManaMax2 = (int)(Player.statManaMax2 * extraManaPercent);


        player.GetDamage<MagicDamageClass>() += ExtraDamage();
        //PrintText($"额外魔法伤害加成 {ExtraDamage()}");
        //PrintText("额外生命："+extraHP+"，减少生命："+reduceHP+"，最终生命："+Player.statLifeMax2 + " 额外魔力倍率："+extraManaPercent + "实际额外获得魔力："+extraMana);
    }

    public float ExtraDamage()
    {
        float manaForExtraDamage = Math.Max(0, Player.statManaMax2 - 700);
        float extraDamageMultiplier = manaForExtraDamage / 20f;
        return extraDamageMultiplier/100f;
    }
    protected override object[] SkillDescriptionArgs => new object[]
    {
        1.5,
        (ExtraDamage() * 100f).ToString("F1")
    };

    public override void OnRightClickInSkillPanel()
    {
        
        /*if (ElainaSkillModPlayer.SkillModPlayer.UnlockedSkill.ContainsKey(this.GetType().Name))
        {
            ElainaSkillModPlayer.SkillModPlayer.LockSkill(Skill);
        }
        else ElainaSkillModPlayer.SkillModPlayer.UnlockSkill(Skill);*/
        base.OnRightClickInSkillPanel();
    }

    public override bool TryGetToolTip(ref string name, ref string level, ref string desc)
    {
        return base.TryGetToolTip(ref name, ref level, ref desc);
    }

    public override bool PreDrawSkillIcon(Vector2 position, Vector2 scale, Color color, Effect effect = null)
    {
        return base.PreDrawSkillIcon(position, scale, color, effect);
    }

    public override void OnUnlockSkillAdded()
    {
        base.OnUnlockSkillAdded();
    }

    public override void OnLockSkill()
    {
        base.OnLockSkill();
    }
}