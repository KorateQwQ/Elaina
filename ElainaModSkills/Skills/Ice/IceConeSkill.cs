using System;
using System.Collections.Generic;
using KL.ActionsSystem;
using KL.SkillSystem;
using KL.SkillSystem.SilkyUI;
using Terraria.DataStructures;
using 伊蕾娜.ElainaActions;
using 伊蕾娜.ElainaModSkills.Skills.Wind;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

[SkillUIInfo(State = 7, Pixels = 500)]
public class IceConeSkill : ElainaSkill
{
    public override bool IsPassiveSkill => false;


    public override void Initialize()
    {
        MaxCD = 1;
        CurrentCD = 0;
        base.Initialize();
    }

    public override void ResetEffects(Player player)
    {
        base.ResetEffects(player);
    }

    public override void OnRightClickInSkillPanel()
    {
        if (ElainaSkillModPlayer.SkillModPlayer.UnlockedSkill.ContainsKey(this.GetType().Name))
        {
            ElainaSkillModPlayer.SkillModPlayer.LockSkill(Skill);
        }
        else ElainaSkillModPlayer.SkillModPlayer.UnlockSkill(Skill);

        base.OnRightClickInSkillPanel();
    }

    public override bool PreUseSkill(IEntitySource source = null)
    {
        Player localPlayer = Main.LocalPlayer;
        ShootIceShardLock3D(localPlayer, Main.MouseWorld);
        //ShootIceCone3D(localPlayer, Main.MouseWorld);
        //ShootIceCone(localPlayer, Main.MouseWorld);

        return false;
    }
    

    public void ShootIceShardLock3D(Player player, Vector2 targetWorldPosition)
    {
        Vector2 aimDirection = (targetWorldPosition - player.MountedCenter).SafeNormalize(Vector2.UnitX * (player.direction == 0 ? 1 : player.direction));

        AnimAction animAction = new Action_Cast()
            .AddNode(new ShootActionNode(
                1,
                ModContent.ProjectileType<IceShardLockProj>(),
                _ => targetWorldPosition,
                GetIceCone3DDamage(),
                2f,
                _ => Vector2.Zero));

        float startRotation = aimDirection.ToRotation() * player.gravDir;
        player.GetModPlayer<ActionModPlayer>().StartAction(animAction, rotation: startRotation);
    }
    

    public void ShootIceCone(Player player, Vector2 aimTarget, int damage = 10, float knockback = 2f,
        float baseSpeed = 25f)
    {
        Vector2 directionToMouse = aimTarget - player.MountedCenter;
        Vector2 aimDirection = directionToMouse.SafeNormalize(Vector2.UnitX * player.direction);
        Vector2 backwardDirection = -aimDirection;

        Vector2[][] positionSets =
        [
            [
                backwardDirection * 10f,
                backwardDirection * -20f + aimDirection.RotatedBy(1.5) * 55f,
                backwardDirection * -55f - aimDirection.RotatedBy(1.5) * 25f,
            ],
            [
                backwardDirection * -20f,
                backwardDirection * -70f + aimDirection.RotatedBy(1.5) * 55f,
                backwardDirection * -55f - aimDirection.RotatedBy(1.5) * 75f,
            ],
            [
                backwardDirection * 30f,
                backwardDirection * -70f + aimDirection.RotatedBy(1.5) * 55f,
                backwardDirection * -15f - aimDirection.RotatedBy(2.5) * 55f,
            ],
        ];

        Vector2[] offsets = positionSets[Main.rand.Next(positionSets.Length)];
        Vector2[] spawnPositions = new Vector2[offsets.Length];
        for (int i = 0; i < offsets.Length; i++)
        {
            spawnPositions[i] = player.MountedCenter + offsets[i];
        }

        AnimAction animAction = new Action_SimpleSlash();
        int count = 0;
        foreach (Vector2 spawnPosition in spawnPositions)
        {
            Vector2 cachedSpawnPosition = spawnPosition;
            animAction.AddNode(new ShootActionNode(
                1 + count,
                ModContent.ProjectileType<IceConeProj>(),
                _ => cachedSpawnPosition,
                damage,
                knockback,
                _ => (aimTarget - cachedSpawnPosition).SafeNormalize(aimDirection) * baseSpeed));
            count += 3;
        }

        float startRotation = aimDirection.ToRotation() * player.gravDir;
        player.GetModPlayer<ActionModPlayer>().StartAction(animAction, rotation: startRotation);
    }

    private int GetIceCone3DDamage()
    {
        return 100;
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

