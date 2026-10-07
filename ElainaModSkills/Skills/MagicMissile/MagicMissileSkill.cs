using System;
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
    private const float AttackIntervalSeconds = 0.3f;
    private const int FullDpsLevel = 5;
    private const int StableDpsLevel = 65;
    private const float InitialDpsRatio = 1f;
    private const float EndgameDpsRatio = 0.6f;

    public override void Initialize()
    {
        MagicPointCost = 3;
        MaxCD = AttackIntervalSeconds;
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
        MagicPointCost = 3;

        return base.CanUseSkill();
    }

    public override bool PreUseSkill(IEntitySource source)
    {
        if (!Player.GetModPlayer<ElainaAttributeModPlayer>().ConsumeMagicPoint(MagicPointCost))
            return false;

        AnimAction animAction = new Action_SimpleShoot()
            .AddNode(new ShootActionNode(
                1,
                ModContent.ProjectileType<MagicMissile>(),
                _ => WandCenter+new Vector2(0,0),
                damage:GetDamage(),
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
        PrintText(localPlayer.GetModPlayer<ElainaStatePlayer>().GetLevel());
        return base.PreUseSkill(source);
    }

    /// <summary>角色等级对应的 DPS 占比：前五级保持完整基准，之后平滑下降，65 级起保持 60%。</summary>
    public static float GetDpsRatio(int playerLevel)
    {
        float progress = Math.Clamp((Math.Max(1, playerLevel) - FullDpsLevel)
            / (float)(StableDpsLevel - FullDpsLevel), 0f, 1f);
        float smoothProgress = progress * progress * (3f - 2f * progress);
        return InitialDpsRatio + (EndgameDpsRatio - InitialDpsRatio) * smoothProgress;
    }

    /// <summary>指定角色等级的普通飞弹理论 DPS，不包含追加伤害、装备、暴击或敌方防御。</summary>
    public static float GetDps(int playerLevel)
    {
        playerLevel = Math.Max(1, playerLevel);
        return KLDpsHelper.GetLevelDps(playerLevel) * GetDpsRatio(playerLevel);
    }

    /// <summary>按指定角色等级计算一枚普通飞弹的基础伤害。</summary>
    public static int GetDamage(int playerLevel)
    {
        return Math.Max(1, KLDpsHelper.GetSingleHitDamage(
            GetDps(playerLevel), AttackIntervalSeconds, 1));
    }

    private int GetDamage()
    {
        int playerLevel = (int)Player.GetModPlayer<ElainaStatePlayer>().GetLevel();
        return GetDamage(playerLevel);
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
