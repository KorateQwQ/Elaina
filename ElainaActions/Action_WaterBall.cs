using System;
using KL.ActionsSystem;
using 伊蕾娜.ElainaModSkills.Skills.Water;

namespace 伊蕾娜.ElainaActions;

/// <summary>
/// 水球的长按施法动作。第 60 帧执行完毕后进入保持，解除后播放 20 帧收势。
/// </summary>
public sealed class Action_WaterBall : ElainaAction
{
    public const int HoldFrame = 60;
    public const int RecoveryFrame = 20;

    private float worldAimRotation;
    private bool freezeAimRotation;
    private int actionToken;

    public Action_WaterBall() : base(HoldFrame + RecoveryFrame + 1)
    {
        AddNode(new ShootActionNode(
            1,
            ModContent.ProjectileType<WaterBall>(),
            _ => WandCenter,
            damage: 10,
            knockback: 2f,
            player => player.DirectionTo(Main.MouseWorld) * 15f,
            projectile =>
            {
                if (projectile.ModProjectile is WaterBall waterBall)
                {
                    waterBall.BindAction(actionToken);
                }
            }));
    }

    public override void OnStart(ActionModPlayer actionPlayer)
    {
        base.OnStart(actionPlayer);
        actionToken = actionPlayer.CurrentActionToken;
        worldAimRotation = StartRotation * owner.gravDir;
        freezeAimRotation = false;
    }

    public override bool IsAutomaticHoldPoint(ActionModPlayer actionPlayer, int actionFrame)
    {
        return actionFrame == HoldFrame;
    }

    public override bool ShouldContinueHolding(ActionModPlayer actionPlayer, int actionFrame)
    {
        return Main.mouseLeft;
    }

    public override void Update(ActionModPlayer actionPlayer, int actionFrame, float actionProgress)
    {
        ApplyAimPose();
        base.Update(actionPlayer, actionFrame, actionProgress);
    }

    public override void OnHoldUpdate(ActionModPlayer actionPlayer, int actionFrame, int holdTime)
    {
        ApplyAimPose();
        SyncItemRotationToArmRotation();
    }

    public override void OnHoldExit(ActionModPlayer actionPlayer, int actionFrame)
    {
        freezeAimRotation = true;
    }

    /// <summary>
    /// 由与本动作绑定的水球提供其 HeldInfo 已计算出的实时朝向。
    /// </summary>
    public void UpdateAimFromWaterBall(int sourceActionToken, float waterBallRotation)
    {
        if (freezeAimRotation || sourceActionToken != actionToken)
        {
            return;
        }

        worldAimRotation = waterBallRotation;
    }

    private void ApplyAimPose()
    {
        owner.direction = MathF.Cos(worldAimRotation) < 0f ? -1 : 1;
        float aimRotation = worldAimRotation * owner.gravDir;
        float armRotation = owner.direction == 1
            ? aimRotation - 1.8f
            : aimRotation + 1.8f - MathHelper.Pi;

        owner.SetCompositeArmFront(
            true,
            Player.CompositeArmStretchAmount.Full,
            MathHelper.WrapAngle(armRotation));
    }
}
