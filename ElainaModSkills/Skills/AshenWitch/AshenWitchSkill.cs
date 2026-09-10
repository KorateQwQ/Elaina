using System;
using KL.SkillSystem;
using KL.SkillSystem.SilkyUI;
using KL.Utils;
using Terraria;
using Terraria.ID;
using 伊蕾娜.ElainaAttribute;

namespace 伊蕾娜.ElainaModSkills.Skills.AshenWitch;

[SkillUIInfo(State = 0, Pixels = 0)]
public class AshenWitchSkill : ElainaSkill
{
    public int LostMaxLife { get; private set; }

    public override bool IsPassiveSkill => true;
    public override bool IsToggleable => true;

    public override SkillUnlockCondition UnlockCondition { get; set; } =
        SkillUnlockCondition.None;

    public override void Initialize()
    {
        MaxCD = -1;
        MagicPointCost = -1;
        KLGameStateManager.OnBossLoot += OnKillBoss;
        base.Initialize();
    }

    public override void ResetEffects(Player player)
    {
        LostMaxLife = 0;
        if (player.GetModPlayer<ElainaModplayer>().Elaina)
        {
            BasicStatus = Skill.SKillBasicStatus.UnLock;
            if (Level < 1)
            {
                Level = 1;
            }
        }

        base.ResetEffects(player);
    }

    public override void UpdateEquips(Player player)
    {
        if (!IsEnabled)
        {
            return;
        }

        player.buffImmune[BuffID.ManaSickness] = true;

        // Vanilla recomputes statLifeMax2 before skill effects. Retain only the configured
        // portion of life gained above the 100 base life while the toggle is enabled.
        int extraLife = Math.Max(0, player.statLifeMax2 - 100);
        float retentionRatio = Math.Clamp(ElainaMagicAttributes.ExtraLifeRetentionRatio, 0f, 1f);
        int retainedExtraLife = (int)(extraLife * retentionRatio);
        int reducedLifeMax = 100 + retainedExtraLife;
        LostMaxLife = Math.Max(0, player.statLifeMax2 - reducedLifeMax);
        if (player.statLifeMax2 > reducedLifeMax)
        {
            player.statLife = Math.Min(player.statLife, reducedLifeMax);
        }

        player.statLifeMax2 = reducedLifeMax;
        base.UpdateEquips(player);
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

    public override void OnRightClickInSkillPanel()
    {
        base.OnRightClickInSkillPanel();
    }
}
