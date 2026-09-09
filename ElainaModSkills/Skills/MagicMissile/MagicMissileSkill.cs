using KL.ActionsSystem;
using KL.ActionsSystem.TemplateActions;
using KL.Drawing;
using KL.SkillSystem;
using KL.SkillSystem.SilkyUI;
using KL.Utils;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;
using 伊蕾娜.ElainaActions;
using 伊蕾娜.ElainaAttribute;
using 伊蕾娜.ElainaModSkills.Skills.Fire;
using 伊蕾娜.ElainaModSkills.Skills.Lightning;
using 伊蕾娜.ElainaModSkills.Skills.Water;
using 伊蕾娜.ElainaModSkills.Skills.Wind;
using 伊蕾娜.ReProjs.Wind;

namespace 伊蕾娜.ElainaModSkills.Skills.MagicMissile;
[SkillUIInfo(State = 0, Pixels = 200)]
public class MagicMissileSkill : ElainaSkill
{
    public override void Initialize()
    {
        MagicPointCost = 3;
        CurrentCD = 0.3f;
        MaxCD = 0.3f;
        base.Initialize();
    }
    public override void ResetEffects(Player player)
    {
        //MagicPointCost = 5;//(int)(player.statMana*0.2)+200;
        //MaxStack = 1;
        if (player.GetModPlayer<ElainaModplayer>().Elaina)
        {
            BasicStatus = Skill.SKillBasicStatus.UnLock;
        }
        base.ResetEffects(player);
    }

    public override bool CanUseSkill()
    {
        return base.CanUseSkill();
    }

    //每秒三次攻击，再根据五发额外伤害的被动，大概得到期望dps/4.2的单发伤害
    public override bool PreUseSkill(IEntitySource source)
    {
        if (!Player.GetModPlayer<ElainaAttributeModPlayer>().ConsumeMagicPoint(MagicPointCost))
            return false;

        AnimAction animAction = new Action_SimpleShoot()
            .AddNode(new ShootActionNode(
                1,
                ModContent.ProjectileType<MagicMissile>(),
                _ => WandCenter+new Vector2(0,0),
                damage:GetDamage(),//DpsHelper.GetSkillDamage(GetType().Name,1)
                2,
                player => new Vector2(1, 0).RotatedBy((Main.MouseWorld - player.MountedCenter).ToRotation()) * 15f));
        //每帧固定回蓝时间
        /*
        animAction.PreNodeUpdateListener = (_, node, actionFrame, _) =>
        {
            /*
            if (node is ShootActionNode)
                PrintText($"MagicMissile ShootActionNode triggered at action frame {actionFrame}.");
                #1#

            return true;
        };*/

        Player localPlayer = Main.LocalPlayer;
        Vector2 directionToMouse = Main.MouseWorld - localPlayer.MountedCenter;

        float startRotation = directionToMouse.ToRotation()*Main.LocalPlayer.gravDir;

        localPlayer.GetModPlayer<ActionModPlayer>().StartAction(animAction,rotation: startRotation);
        
        /*foreach (var info in KLGameStateManager.GetBossInfos())
        {
            if (info.Value.isBoss)
            {
                Log($"{info.Value.displayName} state: {info.Value.progression} maxLevel: {PlayerLevelCapHelper.GetLevelCap(info.Value.progression)}");
            }
        }*/
        
        return base.PreUseSkill(source);
    }

    int GetDamage()
    {
        int level = 10;//角色等级
        float attackTotalTime = 0.333f * 5;//5次攻击需要的时间
        int attackCount = 7;//五次攻击触发被动，额外造成200%伤害，因此可算作7次攻击
        return KLDpsHelper.GetSingleHitDamage(KLDpsHelper.GetLevelDps(level), attackTotalTime, attackCount);
    }
    public override bool PreUpdateCD()
    {   
        //PrintText(CurrentCD);
        //MaxCD = 2;
        //CurrentCD = 1;
        return base.PreUpdateCD();
    }


    protected override object[] SkillDescriptionArgs => new object[]
    {
        GetDamage(),
    };



    public override void PostDrawSkillIcon(Vector2 position, Vector2 scale, Color color,  Effect effect = null)
    {
        base.PostDrawSkillIcon(position, scale,color,  effect);
        //base.PostDrawSkillIcon(position, scale, effect);
    }
    
}