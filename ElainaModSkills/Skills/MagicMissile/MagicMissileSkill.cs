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
using 伊蕾娜.ElainaModSkills.Skills.Lightning;
using 伊蕾娜.ElainaModSkills.Skills.Water;

namespace 伊蕾娜.ElainaModSkills.Skills.MagicMissile;
[SkillUIInfo(State = 0, Pixels = 100)]
public class MagicMissileSkill : ElainaSkill
{
    public override void Initialize()
    {
        MaxCD = 5;
        MaxStack = 1;
        base.Initialize();
    }

    public override bool PreUseSkill(IEntitySource source)
    {
        CurrentCD = 0.5f;
        MaxCD = 0.5f;
        
        //PrintText(DpsHelper.GetSkillDamage(GetType().Name,12));
        AnimAction animAction = new Action_SimpleShoot()
            .AddNode(new ShootActionNode(
                1,
                ModContent.ProjectileType<MagicMissile>(),
                _ => WandCenter,
                damage:DpsHelper.GetSkillDamage(GetType().Name,12),
                2,
                player => new Vector2(1, 0).RotatedBy((Main.MouseWorld - player.MountedCenter).ToRotation()) * 15f));

        Player localPlayer = Main.LocalPlayer;
        Vector2 directionToMouse = Main.MouseWorld - localPlayer.MountedCenter;

        float startRotation = directionToMouse.ToRotation()*Main.LocalPlayer.gravDir;

        localPlayer.GetModPlayer<ActionModPlayer>().StartAction(animAction,rotation: startRotation);
        
        foreach (var info in KLGameStateManager.GetBossInfos())
        {
            if (info.Value.isBoss)
            {
                Log($"{info.Value.displayName} state: {info.Value.progression} maxLevel: {PlayerLevelCapHelper.GetLevelCap(info.Value.progression)}");
            }
        }
        
        return base.PreUseSkill(source);
    }

    public override bool PreUpdateCD()
    {   
        //PrintText(CurrentCD);
        //MaxCD = 2;
        //CurrentCD = 1;
        return base.PreUpdateCD();
    }

    public override void PostDrawSkillIcon(Vector2 position, Vector2 scale, Color color,  Effect effect = null)
    {
        base.PostDrawSkillIcon(position, scale,color,  effect);
        //base.PostDrawSkillIcon(position, scale, effect);
    }
    
}