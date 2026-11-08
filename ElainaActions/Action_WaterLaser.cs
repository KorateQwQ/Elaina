using System;
using KL.ActionsSystem;
using Terraria.DataStructures;
using 伊蕾娜.ElainaAttribute;
using 伊蕾娜.ElainaModSkills;
using 伊蕾娜.ElainaModSkills.ElainaSkillUI;
using 伊蕾娜.ElainaModSkills.Skills.Water;
using 伊蕾娜.Items;

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
    private bool rightWasDown;

    public bool IsChanneling { get; private set; }
    // 第 0 帧尚未进入保持时也接受同帧双键；一旦松手或收势便关闭快捷入口。
    internal bool CanQuickCast => !ending && CanContinueCasting()
        && (!IsChanneling || beam?.Projectile.active == true);
    public Vector2 BeamOrigin => WandCenter;

    private static readonly Color WaterWandColor = ElainaTrailVisualStyle.WaterWandColor;

    protected override void RequestActionStarBloom(float actionProgress)
    {
        // 引导保持第 0 帧：立即显示；松手后进度走完 20 帧收势，光晕随之淡出。
        ElainaTrailVisualStyle.RequestBloomStar(StarCenter.Floor(), actionProgress,
            WaterWandColor, fadeInEnd: 0f, fadeOutStart: 0f);
    }

    protected override void DrawWandTrailVisual(Vector2[] points, float actionProgress)
    {
        ElainaTrailVisualStyle.DrawTrail(points, Vector2.Zero, 0f, actionProgress,
            WaterWandColor, ElainaTrailVisualStyle.DefaultTrailMaxWidth,
            ElainaTrailVisualStyle.DefaultTrailEndWidth, fadeInEnd: 0f, fadeOutStart: 0f);
    }

    // 动作注册器与远端重建需要真正的无参构造函数。
    public Action_WaterLaser() : this(0, 0f, 10) { }

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
        if (TryQuickCast()) return;

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
        if (TryQuickCast()) return;

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
            || !ReferenceEquals(owner.HeldItem, castingItem) || owner.HeldItem.ModItem is not ElainaWand
            || ElainaSkillModPlayer.CurrentSkillIndex != castingSkillSlot)
            return false;

        return WaterBallSkill.TryGetEquipped(owner, out var skill)
            && skill.TryQuickCast();
    }

    // 右键默认也会开技能轮盘；同帧双键发生在动作创建前，也须为快捷施法保留输入。
    internal static bool OwnsQuickCastInput(Player player)
    {
        if (!Main.mouseRight) return false;
        var current = player.GetModPlayer<ActionModPlayer>().CurrentAnimAction;
        if (current is Action_WaterBall { UsesQuickCastInput: true }) return true;
        if (!WaterBallSkill.TryGetEquipped(player, out _)) return false;
        if (current is Action_WaterLaser laser) return laser.CanQuickCast;
        if (current != null || !Main.mouseLeft) return false;
        var skills = player.GetModPlayer<ElainaSkillModPlayer>().ActiveSkill;
        int index = ElainaSkillModPlayer.CurrentSkillIndex;
        return index >= 0 && index < skills.Count && skills[index]?.ModSkill is WaterLaserSKill;
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
