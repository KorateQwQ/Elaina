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
using 伊蕾娜.ElainaModSkills.Skills.Fire;
using 伊蕾娜.ElainaModSkills.Skills.Lightning;
using 伊蕾娜.ElainaModSkills.Skills.Water;
using 伊蕾娜.ElainaModSkills.Skills.Wind;
using 伊蕾娜.ReProjs.Wind;

namespace 伊蕾娜.ElainaModSkills.Skills.MagicMissile;
[SkillUIInfo(State = 0, Pixels = 100)]
public class MagicMissileSkill : ElainaSkill
{
    public override void Initialize()
    {
        MagicPointCost = 5;
        CurrentCD = 0.3f;
        MaxCD = 0.3f;
        base.Initialize();
    }
    public override void ResetEffects(Player player)
    {
        MagicPointCost = 5;

        if (player.GetModPlayer<ElainaModplayer>().Elaina)
        {
            BasicStatus = Skill.SKillBasicStatus.UnLock;
            if (Level < 1) Level = 1;
        }
        base.ResetEffects(player);
    }

    //每秒三次攻击，再根据五发额外伤害的被动，大概得到期望dps/4.2的单发伤害
    public override bool PreUseSkill(IEntitySource source)
    {
        CurrentCD = 0.1f;
        MaxCD = 0.1f;
        
        //PrintText((int)(KLGameStateManager.GetLevelDps(5)));
        AnimAction animAction = new Action_SimpleShoot()
            .AddNode(new ShootActionNode(
                1,
                ModContent.ProjectileType<LightningModelTest>(),
                _ => Main.MouseWorld,
                damage:(int)(KLGameStateManager.GetLevelDps(5)/4.2f),//DpsHelper.GetSkillDamage(GetType().Name,1)
                2,
                player => new Vector2(1, 0).RotatedBy((Main.MouseWorld - player.MountedCenter).ToRotation()) * 15f));

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

    public override bool PreUpdateCD()
    {   
        //PrintText(CurrentCD);
        //MaxCD = 2;
        //CurrentCD = 1;
        return base.PreUpdateCD();
    }


    protected override object[] SkillDescriptionArgs => new object[]
    {
        GetSkillDamage(),
        15
    };



    public override void PostDrawSkillIcon(Vector2 position, Vector2 scale, Color color,  Effect effect = null)
    {
        base.PostDrawSkillIcon(position, scale,color,  effect);
        //base.PostDrawSkillIcon(position, scale, effect);
    }
    
}