using System;
using KL.ActionsSystem;
using Terraria.DataStructures;
using 伊蕾娜.ElainaAttribute;
using 伊蕾娜.ElainaModSkills;
using 伊蕾娜.ElainaModSkills.Skills.MagicMissile;

namespace 伊蕾娜.ElainaActions;

/// <summary>保持挥杖姿势，按施法时的间隔依次生成整组法球。</summary>
public sealed class Action_MultiMissile : ElainaAction
{
    private const int ChannelFrame = 1;
    private static int ShotInterval => 10;
    // 朝向联机同步的节流：角度变化超过阈值且距上次发送足够久才发包。
    private const float AimSyncThreshold = 0.04f;
    private const int AimSyncInterval = 3;
    private readonly int shotInterval;
    private readonly int damage;
    internal bool UsesQuickCastInput { get; }
    private Item castingItem;
    private int castingSlot;
    private int shotsFired;
    private bool ending;
    private int actionToken;
    private float worldAimRotation;
    private float lastSentAimRotation;
    private int framesSinceAimSync;

    // 引导停在第 1 帧，默认淡入会让星芒与杖尾一直半亮；改为立即全亮，松手后在收势中淡出。
    protected override void RequestActionStar(float actionProgress)
    {
        ElainaTrailVisualStyle.RequestStar(StarCenter.Floor(), actionProgress,
            ElainaTrailVisualStyle.DefaultStarColor, fadeInEnd: 0f, fadeOutStart: GetProgress(ChannelFrame));
    }

    protected override ElainaTrailSnapshot CreateWandTrailVisual(Vector2[] points, float actionProgress)
    {
        return new ElainaTrailSnapshot(points, Vector2.Zero, 0f, actionProgress,
            ElainaTrailVisualStyle.DefaultWandTrailColor, ElainaTrailVisualStyle.DefaultTrailMaxWidth,
            ElainaTrailVisualStyle.DefaultTrailEndWidth, fadeInEnd: 0f, fadeOutStart: GetProgress(ChannelFrame));
    }

    public Action_MultiMissile() : this(0, false) { }

    public Action_MultiMissile(int damage, bool quickCast) : base(10)
    {
        this.damage = damage;
        UsesQuickCastInput = quickCast;
        shotInterval = Math.Max(1, ShotInterval);
    }

    public override void OnStart(ActionModPlayer actionPlayer)
    {
        base.OnStart(actionPlayer);
        actionToken = actionPlayer.CurrentActionToken;
        worldAimRotation = StartRotation * owner.gravDir;
        lastSentAimRotation = worldAimRotation;
        castingItem = owner.HeldItem;
        castingSlot = ElainaSkillModPlayer.CurrentSkillIndex;
    }

    private bool CanContinueCasting()
    {
        return owner.active && !owner.dead && !owner.CCed && !owner.noItems
            && ReferenceEquals(owner.HeldItem, castingItem)
            && ElainaSkillModPlayer.CurrentSkillIndex == castingSlot
            && owner.GetModPlayer<ElainaAttributeModPlayer>().UniqueMagicEnabled;
    }

    public override bool IsAutomaticHoldPoint(ActionModPlayer actionPlayer, int actionFrame) =>
        !ending && actionFrame == ChannelFrame;

    public override bool ShouldContinueHolding(ActionModPlayer actionPlayer, int actionFrame) =>
        !ending && shotsFired < MultiMissileSkill.MissileCount && CanContinueCasting();

    public override void Update(ActionModPlayer actionPlayer, int actionFrame, float actionProgress)
    {
        if (!ending && owner.whoAmI == Main.myPlayer && !CanContinueCasting())
            ending = true;

        UpdateLocalAim();
        ApplyPose();
        if (!ending && actionFrame == ChannelFrame && owner.whoAmI == Main.myPlayer)
            SpawnNextOrb();
        if (!ending)
            base.Update(actionPlayer, actionFrame, actionProgress);
        else
            SyncItemRotationToArmRotation();
    }

    public override void OnHoldUpdate(ActionModPlayer actionPlayer, int actionFrame, int holdTime)
    {
        UpdateLocalAim();
        ApplyPose();
        SyncItemRotationToArmRotation();
        if (owner.whoAmI != Main.myPlayer || ending || !CanContinueCasting())
            return;

        if ((holdTime + 1) % shotInterval == 0)
            SpawnNextOrb();
    }

    private void SpawnNextOrb()
    {
        int slot = shotsFired++;
        ShootFromAction(owner, ModContent.ProjectileType<MagicMissleSpawner>(),
            MultiMissileSkill.GetSlotPosition(owner, slot), Vector2.Zero, damage, 2f,
            projectile => projectile.ai[1] = slot + 1);
    }

    public override void OnHoldExit(ActionModPlayer actionPlayer, int actionFrame) => ending = true;
    public override void OnInterrupt(ActionModPlayer actionPlayer)
    {
        ending = true;
        base.OnInterrupt(actionPlayer);
    }
    public override void OnFinish(ActionModPlayer actionPlayer)
    {
        ending = true;
        base.OnFinish(actionPlayer);
    }

    /// <summary>引导期间魔杖跟随鼠标；收势时停在最后的朝向。</summary>
    private void UpdateLocalAim()
    {
        if (ending || owner.whoAmI != Main.myPlayer)
            return;

        worldAimRotation = (Main.MouseWorld - owner.MountedCenter).ToRotation();
        if (Main.netMode == Terraria.ID.NetmodeID.SinglePlayer)
            return;

        framesSinceAimSync++;
        if (framesSinceAimSync < AimSyncInterval
            || MathF.Abs(MathHelper.WrapAngle(worldAimRotation - lastSentAimRotation)) < AimSyncThreshold)
            return;

        framesSinceAimSync = 0;
        lastSentAimRotation = worldAimRotation;
        owner.GetModPlayer<ElainaActionsModPlayer>().SyncMultiMissileAim(actionToken, worldAimRotation);
    }

    /// <summary>远端按拥有者同步的朝向更新姿势。</summary>
    internal void ApplySyncedAim(int sourceActionToken, float rotation)
    {
        if (ending || sourceActionToken != actionToken || owner.whoAmI == Main.myPlayer)
            return;

        worldAimRotation = rotation;
    }

    private void ApplyPose()
    {
        // 与水流术一致按瞄准方向转身；法球仍自行寻找目标。
        owner.direction = MathF.Cos(worldAimRotation) < 0f ? -1 : 1;
        float aimRotation = worldAimRotation * owner.gravDir;
        float armRotation = owner.direction == 1
            ? aimRotation - 1.8f
            : aimRotation + 1.8f - MathHelper.Pi;
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full,
            MathHelper.WrapAngle(armRotation));
    }
}
