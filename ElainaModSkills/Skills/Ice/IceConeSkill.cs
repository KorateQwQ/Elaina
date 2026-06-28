using System;
using System.Collections.Generic;
using KL.ActionsSystem;
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
        ShootIceCone3D(localPlayer, Main.MouseWorld);
        //ShootIceCone(localPlayer, Main.MouseWorld);

        return false;
    }
    

    public void ShootIceCone3D(Player player, Vector2 targetWorldPosition, bool spawnFromOutsideScreen = true)
    {
        Vector2 aimDirection = (targetWorldPosition - player.MountedCenter).SafeNormalize(Vector2.UnitX * (player.direction == 0 ? 1 : player.direction));
        List<IceConeSpawnData> spawnData = BuildIceCone3DSpawnData(targetWorldPosition, 500f);

        AnimAction animAction = new Action_Cast();
        foreach (IceConeSpawnData spawnDataEntry in spawnData)
        {
            IceConeSpawnData cachedSpawnData = spawnDataEntry;
            animAction.AddNode(new CallbackActionNode(cachedSpawnData.TriggerFrame,
                (_, _, _) => SpawnIceCone3D(player, cachedSpawnData, targetWorldPosition)));
        }

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

    private void SpawnIceCone3D(Player player, IceConeSpawnData spawnData, Vector2 targetWorldPosition)
    {
        if (player.whoAmI != Main.myPlayer)
        {
            return;
        }

        Vector2 shootVelocity = (targetWorldPosition - spawnData.SpawnWorldPosition)
            .SafeNormalize(Vector2.UnitX * player.direction) * GetIceCone3DSpeed();

        int projectileIndex = Projectile.NewProjectile(
            player.GetSource_FromThis(),
            spawnData.SpawnWorldPosition,
            shootVelocity,
            ModContent.ProjectileType<IceCone3DProj>(),
            GetIceCone3DDamage(),
            2f,
            player.whoAmI,
            targetWorldPosition.X,
            targetWorldPosition.Y);

        if (projectileIndex < 0 || projectileIndex >= Main.maxProjectiles)
        {
            return;
        }

        Projectile projectile = Main.projectile[projectileIndex];
        projectile.localAI[0] = spawnData.StartDepth;
        projectile.localAI[1] = 0f;
        projectile.netUpdate = true;
    }

    private int GetIceCone3DDamage()
    {
        return 100;
    }

    private float GetIceCone3DSpeed()
    {
        return 58f;
    }

    private static List<IceConeSpawnData> BuildIceCone3DSpawnData(Vector2 targetWorldPosition, float sphereRadius)
    {
        List<IceConeSpawnData> result = new();
        List<IceConeRotationConfig> rotations = BuildIceConeFixedRotations();
        int triggerFrame = 1;

        foreach (IceConeRotationConfig rotation in rotations)
        {
            Vector3 sphereDirection = GetIceConeSphereDirection(rotation);
            Vector3 spawnOffset3D = sphereDirection * sphereRadius;
            Vector2 spawnWorldPosition = targetWorldPosition + new Vector2(spawnOffset3D.X, spawnOffset3D.Y);
            result.Add(new IceConeSpawnData(triggerFrame, spawnWorldPosition, spawnOffset3D.Z));
            triggerFrame += 5;
        }

        return result;
    }

    private static List<IceConeRotationConfig> BuildIceConeFixedRotations()
    {
        //0，0正后方，
        return new List<IceConeRotationConfig>
        {
            new(-0.0f, 0.2f),
            new(-1.9f, 0.2f),
            new(0.4f, 0.3f),
            new(0.6f, -0.3f),
            new(0.9f, -0.3f),
            new(2.5f, 0.3f),
            new(3.1f, -0.8f),
            new(-0.5f, 0.3f),
            new(-0.3f, -0.6f),
            new(0.2f, 0.6f),
            new(0.3f, -0.6f),
            new(-0.3f, 0.6f),
            new(-0.8f, 0.6f),
            new(-1.2f, -0.4f),

            /*new(-2.9f, 0.2f),
            new(-0.15f, 0.3f),
            new(0.65f, -2.6f),
            new(-0.2f, -0.4f),
            new(1.5f, 0.05f),
            new(2.5f, -0.8f),
            new(-1.9f, 0.2f),
            new(0.5f, 1.8f),
            new(3.0f, 0.5f),
            new(1.8f, -0.85f),
            new(2.5f, 0f),
            new(-3.5f, -0.2f),
            new(0.7f, 0.3f),*/
        };
    }

    private static Vector3 GetIceConeSphereDirection(IceConeRotationConfig rotation)
    {
        Vector3 baseDirection = Vector3.UnitZ;
        Matrix rotationMatrix = Matrix.CreateFromYawPitchRoll(rotation.Yaw, rotation.Pitch, rotation.Roll);
        Vector3 sphereDirection = Vector3.TransformNormal(baseDirection, rotationMatrix);
        return sphereDirection.LengthSquared() > 0.0001f
            ? Vector3.Normalize(sphereDirection)
            : Vector3.UnitZ;
    }

    private readonly struct IceConeRotationConfig
    {
        public IceConeRotationConfig(float yaw, float pitch, float roll = 0f)
        {
            Yaw = yaw;
            Pitch = pitch;
            Roll = roll;
        }

        public float Yaw { get; }
        public float Pitch { get; }
        public float Roll { get; }
    }

    private readonly record struct IceConeSpawnData(int TriggerFrame, Vector2 SpawnWorldPosition, float StartDepth);

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

