using static 伊蕾娜.Localization.UIText;
using System;
using KL.ActionsSystem;
using KL.SkillSystem;
using KL.SkillSystem.SilkyUI;
using KL.Utils;
using Terraria.DataStructures;
using 伊蕾娜.ElainaActions;
using 伊蕾娜.ElainaAttribute;
using 伊蕾娜.ElainaModSkills.ElainaDamageClass;

namespace 伊蕾娜.ElainaModSkills.Skills.MagicMissile;

[SkillUIInfo(State = 2, Pixels = 200)]
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

    // 上一次占用的位置；补球从它的下一个位置开始循环查找，保证按 1→5 顺序轮换。
    private int lastSlot = -1;

    public override Type[] PrerequisiteSkills =>new[] { typeof(MagicMissileSkill)};

    // 魔力飞弹需达到 5 级
    public override SkillUnlockCondition UnlockCondition { get; set; } =
        new CompositeSkillUnlockCondition(DefaultUnlockCost, RequireSkillLevel<MagicMissileSkill>(5));

    public override (string Label, string Current, string Next, string Unit)[] GetConstellationUpgradePreview(int nextLevel)
    {
        // 与技能描述一致，使用含装备增伤的单枚法球伤害
        int current = GetConfiguredDamage(skillLevel: Math.Max(1, nextLevel - 1));
        int next = GetConfiguredDamage(skillLevel: nextLevel);
        return [(LocalizeUI("Damage"), current.ToString(), next.ToString(), "")];
    }

    public override void Initialize()
    {
        // 储存层数由 SkillBalance.json 的 maxCharges / initialCharges 决定
        for (int i = 0; i < MissileCount; i++)
        {
            slotOccupied[i] = false;
            spawnerIds[i] = -1;
            reservationTimers[i] = 0;
            reservationVersions[i] = 0;
        }
        lastSlot = -1;

        base.Initialize();
    }

    public override bool CanUseSkill()
    {
        return Player.whoAmI == Main.myPlayer
            && !Player.GetModPlayer<ActionModPlayer>().HasAction
            && CanSummonSpawner()
            && base.CanUseSkill();
    }

    private bool CanSummonSpawner()
    {
        // 即使技能状态曾被重置，也不允许场上该玩家的spawner总数超过上限。
        if (CountActiveSpawners() >= MissileCount)
        {
            return false;
        }

        // 如果所有位置都被占满，无法使用技能
        return FindNextSlot() != -1;
    }

    /// <summary>
    /// 从上次位置的下一个开始循环查找空位。若取编号最小的空位，1 号发射与 5 号补位只要差一帧，
    /// 补位就会抢回 1 号，5 号永远空着。
    /// </summary>
    private int FindNextSlot()
    {
        // 场上全空时从最右侧重新开始。
        int start = Array.IndexOf(slotOccupied, true) == -1 ? 0 : lastSlot + 1;
        for (int i = 0; i < MissileCount; i++)
        {
            int slot = (start + i) % MissileCount;
            if (!slotOccupied[slot])
                return slot;
        }
        return -1;
    }

    public override bool PreUseSkill(IEntitySource source)
    {
        var actionPlayer = Player.GetModPlayer<ActionModPlayer>();
        bool continuing = actionPlayer.CurrentAnimAction is Action_MultiMissile channel
            && channel.IsChannelingSkill(this);
        if (Player.whoAmI != Main.myPlayer || Stack <= 0
            || (continuing ? !CanSummonSpawner() : !CanUseSkill()))
            return false;

        UpdateSpawnerSlots();
        int slotIndex = FindNextSlot();
        if (slotIndex == -1
            || !Player.GetModPlayer<ElainaAttributeModPlayer>().ConsumeMagicPoint(MagicPointCost))
            return false;

        lastSlot = slotIndex;
        int reservationVersion = ++reservationVersions[slotIndex];

        // 必须在启动延迟动作前预留，否则快速释放会重复选中同一位置。
        slotOccupied[slotIndex] = true;
        spawnerIds[slotIndex] = -1;
        reservationTimers[slotIndex] = ReservationTimeout;

        Vector2 spawnPosition = GetSlotPosition(Player, slotIndex);

        // 首枚仍在第 1 帧生成，后续补球沿用射击钩子，但不重复启动物品和挥杖动作。
        var shot = new ShootActionNode(
            1,
            ModContent.ProjectileType<MagicMissleSpawner>(),
            _ => spawnPosition,
            damage: GetConfiguredDamage(),
            2,
            _ => Vector2.Zero,
            configureProjectile: projectile =>
                OnSpawnerCreated(projectile, slotIndex, reservationVersion));

        if (continuing)
        {
            shot.Update(actionPlayer, 1, 0f);
        }
        else
        {
            var animAction = new Action_MultiMissile(this);
            animAction.AddNode(shot);
            float startRotation = (Main.MouseWorld - Player.MountedCenter).ToRotation() * Player.gravDir;
            actionPlayer.StartAction(animAction, interruptCurrentAction: false, rotation: startRotation);
        }

        return base.PreUseSkill(source);
    }

    internal bool UpdateChannelShot(out bool fired)
    {
        fired = false;
        // 满槽或尚未恢复层数时只保持动作，不扣魔力，也不重置冷却。
        if (Stack <= 0 || !CanSummonSpawner())
            return true;
        if (!base.CanUseSkill())
            return false;

        int previousStack = Stack;
        Skill.UseSkill(null, Player.GetModPlayer<ElainaSkillModPlayer>().Attributes);
        fired = Stack < previousStack;
        return fired;
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

    protected override object[] SkillDescriptionArgs => new object[]
    {
        MissileCount,
        GetConfiguredDamage(),
    };

    public override void PostDrawSkillIcon(Vector2 position, Vector2 scale, Color color, Effect effect = null)
    {
        base.PostDrawSkillIcon(position, scale, color, effect);
    }
}
