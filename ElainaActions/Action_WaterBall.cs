using System;
using KL.ActionsSystem;
using 伊蕾娜.ElainaModSkills.Skills.Water;

namespace 伊蕾娜.ElainaActions;

/// <summary>
/// 水球蓄满后可以继续瞄准，松开施法键时发射并收势。
/// </summary>
public sealed class Action_WaterBall : ElainaAction
{
    public const int HoldFrame = 60;
    public const int RecoveryFrame = 20;

    private float worldAimRotation;
    private bool freezeAimRotation;
    private int actionToken;
    internal bool UsesQuickCastInput { get; }
    private readonly int chargeTicks;
    private bool released;

    protected override void RequestActionStar(float actionProgress)
    {
        ElainaTrailVisualStyle.RequestStar(StarCenter.Floor(), actionProgress,
            ElainaTrailVisualStyle.WaterWandColor, drawHalo: false);
    }

    protected override ElainaTrailSnapshot CreateWandTrailVisual(Vector2[] points, float actionProgress)
    {
        return new ElainaTrailSnapshot(points, Vector2.Zero, 0f, actionProgress,
            ElainaTrailVisualStyle.WaterWandColor, ElainaTrailVisualStyle.DefaultTrailMaxWidth,
            ElainaTrailVisualStyle.DefaultTrailEndWidth, ElainaTrailVisualStyle.DefaultTrailFadeInEnd,
            ElainaTrailVisualStyle.DefaultTrailFadeOutStart, useBloom: false);
    }

    public Action_WaterBall() : this(10, 1.5f) { }

    public Action_WaterBall(int damage, float burningDamageMultiplier, bool quickCast = false)
        : this(damage, burningDamageMultiplier, quickCast, WaterBallSkill.ChargeTicks) { }

    private Action_WaterBall(int damage, float burningDamageMultiplier, bool quickCast, int chargeTicks)
        : base(Math.Min(HoldFrame, chargeTicks) + RecoveryFrame + 1)
    {
        this.chargeTicks = chargeTicks;
        UsesQuickCastInput = quickCast;
        AddNode(new ShootActionNode(
            1,
            ModContent.ProjectileType<WaterBall>(),
            _ => WandCenter,
            damage: damage,
            knockback: 2f,
            player => player.DirectionTo(Main.MouseWorld) * 15f,
            projectile =>
            {
                if (projectile.ModProjectile is WaterBall waterBall)
                {
                    waterBall.BindAction(actionToken, chargeTicks);
                    waterBall.BurningDamageMultiplier = burningDamageMultiplier;
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
        return actionFrame == TotalFrame - RecoveryFrame - 1;
    }

    public override bool ShouldContinueHolding(ActionModPlayer actionPlayer, int actionFrame)
    {
        return !ShouldRelease(actionFrame + actionPlayer.CurrentActionHoldTime);
    }

    // 动作与弹幕共用一次松手判定，进入收势后重新按键不会扣住已发射的水球。
    internal bool ShouldRelease(int elapsedTicks)
    {
        if (elapsedTicks >= chargeTicks && !Main.mouseLeft && !(UsesQuickCastInput && Main.mouseRight))
            released = true;
        return released;
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
    /// 由于本动作绑定的水球提供其 HeldInfo 已计算出的实时朝向。
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
