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

    public override void Initialize()
    {
        MaxCD = -1;
        MagicPointCost = -1;
        KLGameStateManager.OnBossLoot += OnKillBoss;
        base.Initialize();
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