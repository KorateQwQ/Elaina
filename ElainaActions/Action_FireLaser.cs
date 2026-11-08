using System;
using KL.ActionsSystem;
using Terraria.DataStructures;
using 伊蕾娜.ElainaAttribute;
using 伊蕾娜.ElainaModSkills;
using 伊蕾娜.ElainaModSkills.ElainaSkillUI;
using 伊蕾娜.ElainaModSkills.Skills.Fire;
using 伊蕾娜.Items;

namespace 伊蕾娜.ElainaActions;

/// <summary>按下后进入引导（射线弹幕先蓄力再爆开）；松手、切换或耗魔失败时进入收势。</summary>
public sealed class Action_FireLaser : ElainaAction
{
    private const int ChannelStartFrame = 0;
    public const int RecoveryFrames = 20;

    private float worldAimRotation;
    private int actionToken;
    private Item castingItem;
    private int castingSkillSlot;
    private FireLaser beam;
    private bool ending;
    private bool rightWasDown;
    private readonly FireLaserSkill castingSkill;

    public bool IsChanneling { get; private set; }
    internal bool CanQuickCast => !ending && CanContinueCasting()
        && (!IsChanneling || beam?.Projectile.active == true);
    internal bool SustainsDomain => !ending && IsChanneling && CanContinueCasting()
        && beam?.Projectile.active == true && beam.IsFiring;
    public Vector2 BeamOrigin => WandCenter;

    private static readonly Color FireWandColor = ElainaTrailVisualStyle.FireWandColor;

    protected override void RequestActionStarBloom(float actionProgress)
    {
        // 引导保持第 0 帧：立即显示；松手后进度走完 20 帧收势，光晕随之淡出。
        ElainaTrailVisualStyle.RequestBloomStar(StarCenter.Floor(), actionProgress,
            FireWandColor, fadeInEnd: 0f, fadeOutStart: 0f);
    }

    protected override void DrawWandTrailVisual(Vector2[] points, float actionProgress)
    {
        ElainaTrailVisualStyle.DrawTrail(points, Vector2.Zero, 0f, actionProgress,
            FireWandColor, ElainaTrailVisualStyle.DefaultTrailMaxWidth,
            ElainaTrailVisualStyle.DefaultTrailEndWidth, fadeInEnd: 0f, fadeOutStart: 0f);
    }

    // 动作注册器与远端重建需要真正的无参构造函数。
    public Action_FireLaser() : this(0, 0f, 10) { }

    public Action_FireLaser(int damage, float manaPerPulse, int intervalFrames, FireLaserSkill skill = null,
        float domainDamageMultiplier = 1f)
        : base(ChannelStartFrame + RecoveryFrames + 1)
    {
        castingSkill = skill;
        AddNode(new ShootActionNode(
            ChannelStartFrame, ModContent.ProjectileType<FireLaser>(), _ => WandCenter,
            damage, 2f, _ => worldAimRotation.ToRotationVector2(),
            projectile =>
            {
                if (projectile.ModProjectile is FireLaser fireLaser)
                {
                    beam = fireLaser;
                    fireLaser.BindAction(actionToken, manaPerPulse, intervalFrames, domainDamageMultiplier);
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
        TryQuickCast();
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
        TryQuickCast();
        ApplyAimPose();
        SyncItemRotationToArmRotation();
    }

    private bool TryQuickCast()
    {
        if (owner.whoAmI != Main.myPlayer) return false;
        bool pressed = Main.mouseRight && !rightWasDown;
        rightWasDown = Main.mouseRight;
        if (!pressed || !CanQuickCast
            || !Main.hasFocus || Main.gameMenu || Main.gamePaused || Main.drawingPlayerChat
            || Main.ingameOptionsWindow || Main.inFancyUI || Main.editSign || Main.editChest
            || owner.mouseInterface || ElainaSkillWheel.IsShowing || ElainaHealingWheel.OwnsPointer
            || ElainaModSkills.Skills.DollControl.DollCommandWheel.OwnsPointer
            || owner.HeldItem.ModItem is not ElainaWand)
            return false;

        return FireBurstSkill.TryGetEquipped(owner, out var skill) && skill.TryQuickCast();
    }

    internal static bool OwnsQuickCastInput(Player player)
    {
        if (!Main.mouseRight || !FireBurstSkill.TryGetEquipped(player, out _)) return false;
        var current = player.GetModPlayer<ActionModPlayer>().CurrentAnimAction;
        if (current is Action_FireLaser laser) return laser.CanQuickCast;
        if (current != null || !Main.mouseLeft) return false;
        var skills = player.GetModPlayer<ElainaSkillModPlayer>().ActiveSkill;
        int index = ElainaSkillModPlayer.CurrentSkillIndex;
        return index >= 0 && index < skills.Count && skills[index]?.ModSkill is FireLaserSkill;
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
        if (ending) return;
        ending = true;
        if (owner.whoAmI == Main.myPlayer) castingSkill?.EndChannel();
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

