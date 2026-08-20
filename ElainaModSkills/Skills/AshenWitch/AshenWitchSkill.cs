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
        player.GetModPlayer<ElainaAttributeModPlayer>().MaxMagicPoint+=1*Player.statManaMax2;

        //PrintText("更新装备");
    }
    
    public override void OnRightClickInSkillPanel()
    {
        
        /*if (ElainaSkillModPlayer.SkillModPlayer.UnlockedSkill.ContainsKey(this.GetType().Name))
        {
            ElainaSkillModPlayer.SkillModPlayer.LockSkill(Skill);
        }
        else ElainaSkillModPlayer.SkillModPlayer.UnlockSkill(Skill);*/
        base.OnRightClickInSkillPanel();
    }

    protected virtual object[] SkillDescriptionArgs => Array.Empty<object>();
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