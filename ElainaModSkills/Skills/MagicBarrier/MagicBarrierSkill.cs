using System;
using KL.Extensions;
using KL.SkillSystem;
using KL.SkillSystem.SilkyUI;
using 伊蕾娜.ElainaAttribute;
using 伊蕾娜.Items;

namespace 伊蕾娜.ElainaModSkills.Skills.MagicBarrier;

[SkillUIInfo(State = 0, Pixels = 100)]
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
        //魔力屏障：10%魔力以下生效最小值，80%魔力时完全生效，最小0%伤害减免，最大70%伤害减免
        float minimumMagicPointRatio = 0.1f;
        float fullReductionMagicPointRatio = 0.8f;
        float minimumDamageReduction = 0.0f;
        float fullDamageReduction = 0.7f;
        
        //ElainaAttributeModPlayer attributePlayer = player.GetModPlayer<ElainaAttributeModPlayer>();
        if (player.statMana> 0f&&
            (player.HeldItem.IsAir||!player.HeldItem.IsWeapon()||player.HeldItem.type == ModContent.ItemType<ElainaWand>()))
        {
            float magicPointRatio = (player.statMana / (float)player.statManaMax2);
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
            //ElainaAttributeModPlayer attributePlayer = Player.GetModPlayer<ElainaAttributeModPlayer>();
            ElainaSkillModPlayer skillPlayer = Player.GetModPlayer<ElainaSkillModPlayer>();
            if (skillPlayer.TryGetUnlockedModSkill<MagicBarrierSkill>(out var magicBarrierSkill))
            {
                if (magicBarrierSkill.IsEnabled)
                {
                    float endurance = GetEndurance(Player);
                    float reductionDamage = endurance*info.SourceDamage;
                    if (endurance > 0)
                    {
                        float magicPointCost = Math.Min(reductionDamage, Player.statManaMax2*0.2f);
                        if (Player.statMana>magicPointCost)
                        {
                            //attributePlayer.ConsumeMagicPoint(magicPointCost);
                            Player.statMana -= (int)magicPointCost;
                        }
                        else Player.statMana = 0;

                        Player.manaRegenDelay = 30;
                        PrintText($"魔法屏障：受到 {info.SourceDamage}伤害 护盾减免率{endurance} 护盾减免 {reductionDamage} 伤害，消耗魔力：{magicPointCost:0.#}, 最终受到伤害：{info.Damage}"); 
                    }
                }
            }


            base.OnHurt(info);
        }
    }
}