using KL.ActionsSystem;
using KL.SkillSystem.SilkyUI;
using KL.Utils;
using Terraria.DataStructures;
using 伊蕾娜.ElainaActions;
using 伊蕾娜.ElainaAttribute;

namespace 伊蕾娜.ElainaModSkills.Skills.MagicMissile;

[SkillUIInfo(State = 1, Pixels = 200)]
public class MultiMissileSkill : ElainaSkill
{
    private const int MissileCount = 5;
    private const int ReservationTimeout = 120;
    private const float Radius = 120f;

    // ShootActionNode并非立即生成弹幕，因此弹幕生成前也必须保持位置预留。
    private readonly bool[] slotOccupied = new bool[MissileCount];
    private readonly int[] spawnerIds = new int[MissileCount];
    private readonly int[] reservationTimers = new int[MissileCount];

    // 用于识别超时后才执行的旧回调，避免旧动作占用已被重新分配的位置。
    private readonly int[] reservationVersions = new int[MissileCount];

    public override void Initialize()
    {
        MaxCD = 0.15f;
        MaxStack = 1;
        MagicPointCost = 10;

        for (int i = 0; i < MissileCount; i++)
        {
            slotOccupied[i] = false;
            spawnerIds[i] = -1;
            reservationTimers[i] = 0;
            reservationVersions[i] = 0;
        }

        base.Initialize();
    }

    public override bool CanUseSkill()
    {
        // 即使技能状态曾被重置，也不允许场上该玩家的spawner总数超过上限。
        if (CountActiveSpawners() >= MissileCount)
        {
            return false;
        }

        // 找到最右边（索引最小）的空闲位置
        int emptySlot = -1;
        for (int i = 0; i < MissileCount; i++)
        {
            if (!slotOccupied[i])
            {
                emptySlot = i;
                break;
            }
        }
        // 如果所有位置都被占满，无法使用技能
        if (emptySlot == -1)
        {
            return false;
        }

        return base.CanUseSkill();
    }

    public override bool PreUseSkill(IEntitySource source)
    {
        if (!Player.GetModPlayer<ElainaAttributeModPlayer>().ConsumeMagicPoint(MagicPointCost))
            return false;
        
        UpdateSpawnerSlots();

        // 找到最右边（索引最小）的空闲位置
        int emptySlot = -1;
        for (int i = 0; i < MissileCount; i++)
        {
            if (!slotOccupied[i])
            {
                emptySlot = i;
                break;
            }
        }

        int slotIndex = emptySlot;
        int reservationVersion = ++reservationVersions[slotIndex];

        // 必须在启动延迟动作前预留，否则快速释放会重复选中同一位置。
        slotOccupied[slotIndex] = true;
        spawnerIds[slotIndex] = -1;
        reservationTimers[slotIndex] = ReservationTimeout;

        Vector2 spawnPosition = GetSlotPosition(Player, slotIndex);

        // 创建动作并生成单个spawner
        AnimAction animAction = new Action_SimpleShoot(10);
        animAction.AddNode(new ShootActionNode(
            1,
            ModContent.ProjectileType<MagicMissleSpawner>(),
            _ => spawnPosition,
            damage: 100,
            2,
            _ => Vector2.Zero,
            configureProjectile: projectile =>
                OnSpawnerCreated(projectile, slotIndex, reservationVersion)));

        float startRotation = (Main.MouseWorld - Player.MountedCenter).ToRotation();
        Player.GetModPlayer<ActionModPlayer>().StartAction(animAction, rotation: startRotation);

        return base.PreUseSkill(source);
    }

    internal static Vector2 GetSlotPosition(Player player, int slotIndex)
    {
        // 与旧版五重飞弹一致：使用不受Player.fullRotation影响的固定半圆偏移。
        float angle = MathHelper.Lerp(0, -MathHelper.Pi,
            slotIndex / (float)(MissileCount - 1));
        Vector2 offset = angle.ToRotationVector2() * Radius;

        return player.VisualCenter() + offset;
    }

    // 当spawner被创建时的回调
    private void OnSpawnerCreated(Projectile projectile, int slotIndex, int reservationVersion)
    {
        bool reservationIsCurrent =
            reservationVersions[slotIndex] == reservationVersion &&
            slotOccupied[slotIndex] &&
            spawnerIds[slotIndex] == -1;

        if (projectile == null || !reservationIsCurrent)
        {
            // 已超时的动作若迟到，直接移除，不能让它与新spawner重叠。
            if (projectile?.active == true)
            {
                projectile.Kill();
            }
            return;
        }

        spawnerIds[slotIndex] = projectile.whoAmI;
        reservationTimers[slotIndex] = 0;

        // ai[1]记录位置（加1以区分默认值0），便于状态重建和联机同步。
        projectile.ai[1] = slotIndex + 1;
        projectile.netUpdate = true;
    }

    public override bool PreUpdateCD()
    {
        UpdateSpawnerSlots();
        return base.PreUpdateCD();
    }

    private void UpdateSpawnerSlots()
    {
        int spawnerType = ModContent.ProjectileType<MagicMissleSpawner>();

        for (int i = 0; i < MissileCount; i++)
        {
            if (!slotOccupied[i])
            {
                continue;
            }

            int spawnerId = spawnerIds[i];
            if (spawnerId == -1)
            {
                if (--reservationTimers[i] <= 0)
                {
                    ReleaseSlot(i);
                }
                continue;
            }

            if (spawnerId < 0 || spawnerId >= Main.maxProjectiles)
            {
                ReleaseSlot(i);
                continue;
            }

            Projectile projectile = Main.projectile[spawnerId];
            bool isTrackedSpawner =
                projectile.active &&
                projectile.owner == Player.whoAmI &&
                projectile.type == spawnerType;

            // ai[0]: 0=Spawn, 1=Normal, 2=Dead
            if (!isTrackedSpawner || projectile.ai[0] == 2)
            {
                ReleaseSlot(i);
            }
        }

        // 技能实例重建时，从弹幕携带的位置编号恢复占用状态。
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            Projectile projectile = Main.projectile[i];
            if (!projectile.active ||
                projectile.owner != Player.whoAmI ||
                projectile.type != spawnerType ||
                projectile.ai[0] == 2)
            {
                continue;
            }

            int slotIndex = (int)projectile.ai[1] - 1;
            if (slotIndex >= 0 && slotIndex < MissileCount && !slotOccupied[slotIndex])
            {
                slotOccupied[slotIndex] = true;
                spawnerIds[slotIndex] = projectile.whoAmI;
                reservationTimers[slotIndex] = 0;
            }
        }
    }

    private void ReleaseSlot(int slotIndex)
    {
        slotOccupied[slotIndex] = false;
        spawnerIds[slotIndex] = -1;
        reservationTimers[slotIndex] = 0;
    }

    private int CountActiveSpawners()
    {
        int count = 0;
        int spawnerType = ModContent.ProjectileType<MagicMissleSpawner>();

        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            Projectile projectile = Main.projectile[i];
            if (projectile.active &&
                projectile.owner == Player.whoAmI &&
                projectile.type == spawnerType &&
                projectile.ai[0] != 2)
            {
                count++;
            }
        }

        return count;
    }

    public override void PostDrawSkillIcon(Vector2 position, Vector2 scale, Color color, Effect effect = null)
    {
        base.PostDrawSkillIcon(position, scale, color, effect);
    }
}