using System;
using KL.Extensions;
using KL.SkillSystem;
using KL.SkillSystem.SilkyUI;
using 伊蕾娜.ElainaAttribute;
using 伊蕾娜.Items;

namespace 伊蕾娜.ElainaModSkills.Skills.MagicBarrier;

[SkillUIInfo(State = 0, Pixels = 60)]
public class MagicBarrierSkill : ElainaSkill
{

    public override bool IsPassiveSkill => true;
    public override bool IsToggleable => true;

    public override void Initialize()
    {
        MaxCD = -1;
        MagicPointCost = -1;
        base.Initialize();
    }

    public override void UpdateEquips(Player player)
    {
        if (IsEnabled)
        {
            player.endurance += GetEndurance(player);
        }
        base.UpdateEquips(player);
    }

    public static float GetEndurance(Player player)
    {
        //魔力屏障：20%魔力以下生效最小值，80%魔力时完全生效，最小30%伤害减免，最大70%伤害减免
        float minimumMagicPointRatio = 0.2f;
        float fullReductionMagicPointRatio = 0.8f;
        float minimumDamageReduction = 0.3f;
        float fullDamageReduction = 0.7f;
        
        ElainaAttributeModPlayer attributePlayer = player.GetModPlayer<ElainaAttributeModPlayer>();
        if (attributePlayer.MaxMagicPoint > 0f&&
            (player.HeldItem.IsAir||!player.HeldItem.IsWeapon()||player.HeldItem.type == ModContent.ItemType<ElainaWand>()))
        {
            float magicPointRatio = attributePlayer.MagicPoint / attributePlayer.MaxMagicPoint;
            if (magicPointRatio >= minimumMagicPointRatio)
            {
                float reductionProgress = fullReductionMagicPointRatio <= minimumMagicPointRatio
                    ? 1f
                    : MathHelper.Clamp(
                        (magicPointRatio - minimumMagicPointRatio) /
                        (fullReductionMagicPointRatio - minimumMagicPointRatio), 0f, 1f);
                float reduction = MathHelper.Lerp(
                    minimumDamageReduction, fullDamageReduction, reductionProgress);
                return reduction;
            }

            return minimumDamageReduction;
        }

        return 0;
    }

    class MagicBarrierModPlayer : ModPlayer
    {

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            base.ModifyHurt(ref modifiers);
        }
        

        public override void OnHurt(Player.HurtInfo info)
        {
            ElainaAttributeModPlayer attributePlayer = Player.GetModPlayer<ElainaAttributeModPlayer>();
            ElainaSkillModPlayer skillPlayer = Player.GetModPlayer<ElainaSkillModPlayer>();
            if (skillPlayer.UnlockedSkill.TryGetValue("MagicBarrierSkill", out var skill))
            {
                if (skill.ModSkill.IsEnabled)
                {
                    float endurance = GetEndurance(Player);
                    float reductionDamage = endurance*info.SourceDamage;
                    if (endurance > 0)
                    {
                        float magicPointCost = Math.Min(reductionDamage, attributePlayer.MaxMagicPoint*0.2f);
                        if (attributePlayer.ConsumeMagicPoint(magicPointCost, false))
                        {
                            attributePlayer.ConsumeMagicPoint(magicPointCost);
                        }
                        else attributePlayer.MagicPoint = 0;
                    
                        PrintText($"魔法屏障：受到 {info.SourceDamage}伤害  护盾减免 {reductionDamage} 伤害，消耗魔力：{magicPointCost:0.#}"); 
                    }
                }
            }


            base.OnHurt(info);
        }
    }
}