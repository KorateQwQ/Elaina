using System;
using KL.ActionsSystem;
using 伊蕾娜.ElainaAttribute;
using 伊蕾娜.ElainaModSkills;
using 伊蕾娜.ElainaModSkills.Skills.Water;

namespace 伊蕾娜.ElainaActions;

/// <summary>按下后立即持续引导；松手、切换或耗魔失败时进入收势。</summary>
public sealed class Action_WaterLaser : ElainaAction
{
    private const int ChannelStartFrame = 0;
    public const int RecoveryFrames = 20;

    private float worldAimRotation;
    private int actionToken;
    private Item castingItem;
    private int castingSkillSlot;
    private WaterLaser beam;
    private bool ending;

    public bool IsChanneling { get; private set; }
    public Vector2 BeamOrigin => WandCenter;

    // 动作注册器与远端重建需要真正的无参构造函数。
    public Action_WaterLaser() : this(0, 0f, 12) { }

    public Action_WaterLaser(int damage, float manaPerPulse, int intervalFrames)
        : base(ChannelStartFrame + RecoveryFrames + 1)
    {
        AddNode(new ShootActionNode(
            ChannelStartFrame, ModContent.ProjectileType<WaterLaser>(), _ => WandCenter,
            damage, 2f, _ => worldAimRotation.ToRotationVector2(),
            projectile =>
            {
                if (projectile.ModProjectile is WaterLaser waterLaser)
                {
                    beam = waterLaser;
                    waterLaser.BindAction(actionToken, manaPerPulse, intervalFrames);
                }
            }));
    }

    public override void OnStart(ActionModPlayer actionPlayer)
    {
        base.OnStart(actionPlayer);
        actionToken = actionPlayer.CurrentActionToken;
        worldAimRotation = StartRotation * owner.gravDir;
        castingItem = owner.HeldItem;
        castingSkillSlot = ElainaSkillModPlayer.CurrentSkillIndex;
    }

    public override bool IsAutomaticHoldPoint(ActionModPlayer actionPlayer, int actionFrame) =>
        actionFrame == ChannelStartFrame;

    public override bool ShouldContinueHolding(ActionModPlayer actionPlayer, int actionFrame) =>
        !ending && CanContinueCasting() && beam?.Projectile.active == true;

    private bool CanContinueCasting() =>
        owner.active && !owner.dead && !owner.CCed && !owner.noItems
        && Main.mouseLeft && !owner.mouseInterface
        && ReferenceEquals(owner.HeldItem, castingItem)
        && ElainaSkillModPlayer.CurrentSkillIndex == castingSkillSlot
        && owner.GetModPlayer<ElainaAttributeModPlayer>().UniqueMagicEnabled;

    public override void Update(ActionModPlayer actionPlayer, int actionFrame, float actionProgress)
    {
        if (!ending && owner.whoAmI == Main.myPlayer
            && (!CanContinueCasting() || (actionFrame > ChannelStartFrame && beam?.Projectile.active != true)))
        {
            FinishChannel(actionPlayer);
            return;
        }

        ApplyAimPose();
        base.Update(actionPlayer, actionFrame, actionProgress);
    }

    public override void OnHoldEnter(ActionModPlayer actionPlayer, int actionFrame)
    {
        IsChanneling = !ending;
    }

    public override void OnHoldUpdate(ActionModPlayer actionPlayer, int actionFrame, int holdTime)
    {
        ApplyAimPose();
        SyncItemRotationToArmRotation();
    }

    public override void OnHoldExit(ActionModPlayer actionPlayer, int actionFrame) => StopBeam();
    public override void OnInterrupt(ActionModPlayer actionPlayer) => StopBeam();
    public override void OnFinish(ActionModPlayer actionPlayer) => StopBeam();

    public void FinishChannel(ActionModPlayer actionPlayer)
    {
        if (ending || actionPlayer.CurrentActionToken != actionToken)
            return;

        StopBeam();
        actionPlayer.SetCurrentActionHold(false, ChannelStartFrame);
    }

    private void StopBeam()
    {
        ending = true;
        IsChanneling = false;
        if (owner.whoAmI == Main.myPlayer && beam?.Projectile.active == true)
            beam.StopChannel();
    }

    public void UpdateAimFromLaser(int sourceActionToken, float rotation)
    {
        if (ending || sourceActionToken != actionToken)
            return;

        worldAimRotation = rotation;
        ApplyAimPose();
        SyncItemRotationToArmRotation();
    }

    private void ApplyAimPose()
    {
        owner.direction = MathF.Cos(worldAimRotation) < 0f ? -1 : 1;
        float aimRotation = worldAimRotation * owner.gravDir;
        float armRotation = owner.direction == 1
            ? aimRotation - 1.8f
            : aimRotation + 1.8f - MathHelper.Pi;
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full,
            MathHelper.WrapAngle(armRotation));
    }
}
