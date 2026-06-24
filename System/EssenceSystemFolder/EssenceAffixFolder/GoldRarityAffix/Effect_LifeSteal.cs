using KL.Utils;
using KL.Utils.Net;
using Terraria.ID;

namespace 伊蕾娜.System.EssenceSystemFolder.EssenceAffixFolder.GoldRarityAffix;

public sealed class Effect_LifeSteal : IEssenceAffixEffect
{
    public static readonly Effect_LifeSteal Instance = new();

    public void UpdateAccessory(Player player, in EssenceAffixEffectContext ctx)
    {
        player.GetModPlayer<LifeStealModPlayer>().LifeSteal+= ctx.GetLinearFactor()*5;
    }
    
    public string GetDescription(in EssenceAffixEffectContext ctx)
    {
        float value = ctx.GetLinearFactor()*5;
        return Language.GetText("Mods.伊蕾娜.EssenceSystem.CommonItemTooltip.IncreasedLifeSteal").WithFormatArgs(value).Value;
    }
    
    class LifeStealModPlayer : KLModPlayer
    {
        //生命偷取，10生命偷取代表着10%造成伤害的回复量。
        public float LifeSteal = 0;
        
        //每帧只有首次命中才能享受全额吸血，其余命中只有5%的效能，从而限制aoe伤害。
        
        //当前帧记录的吸血量
        float healRecord = 0;
        public override void ResetEffects()
        {
            LifeSteal = 0;

            if (healRecord > 1&&Main.myPlayer==Player.whoAmI)
            {
                //获取整数部分进行治疗
                int healInteger = (int)healRecord;
                //保留小数部分继续累积
                float decimalPart = healRecord - healInteger;
                
                if (healInteger > 0)
                {
                    RPC("HealPlayer", [Main.myPlayer, healInteger],KLNetModule.NetSendType.ClientToAll);
                }
                //只重置为小数部分
                healRecord = decimalPart;
            }

            //Player.ConsumedLifeCrystals = 20;
            base.ResetEffects();
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (LifeSteal > 0&&target.canGhostHeal&&!target.immortal)
            {
                float healAmount = hit.Damage*LifeSteal/100f;
                if (proj.maxPenetrate != 1 || proj.penetrate != 1)
                {
                    healAmount *= 0.05f;
                }

                healRecord += healAmount;
            }
            base.OnHitNPCWithProj(proj, target, hit, damageDone);
        }

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (LifeSteal > 0&&target.canGhostHeal&&!target.immortal)
            {
                float healAmount = hit.Damage*LifeSteal/100f;
                healRecord += healAmount;
            }
            base.OnHitNPCWithItem(item, target, hit, damageDone);
        }
        

        public void HealPlayer(int playerID, float healAmount)
        {
            if(playerID<0||playerID>=Main.maxPlayers)return;
            Player player = Main.player[playerID];
            if (player is { active: true, dead: false })
            {
                player.Heal((int)healAmount);
            }
        }
    }
    
}