using System;
using KL.ActionsSystem;
using Terraria.DataStructures;
using 伊蕾娜.ElainaAttribute;
using 伊蕾娜.ElainaModSkills;
using 伊蕾娜.ElainaModSkills.Skills.MagicMissile;

namespace 伊蕾娜.ElainaActions;

/// <summary>保持一次挥杖动作，由原技能的层数与冷却驱动持续补球。</summary>
public sealed class Action_MultiMissile : ElainaAction
{
    private const int ChannelFrame = 1;
    // 补球最短间隔（帧），实际间隔取它与技能冷却中较长者。写成属性便于热重载微调。
    private static int MinimumShotInterval => 10;
    // 朝向联机同步的节流：角度变化超过阈值且距上次发送足够久才发包。
    private const float AimSyncThreshold = 0.04f;
    private const int AimSyncInterval = 3;
    private readonly MultiMissileSkill castingSkill;
    private Item castingItem;
    private int castingSlot;
    private int framesSinceShot;
    private bool ending;
    private int actionToken;
    private float worldAimRotation;
    private float lastSentAimRotation;
    private int framesSinceAimSync;

    // 引导停在第 1 帧，默认淡入会让星芒与杖尾一直半亮；改为立即全亮，松手后在收势中淡出。
    protected override void RequestActionStarBloom(float actionProgress)
    {
        ElainaTrailVisualStyle.RequestBloomStar(StarCenter.Floor(), actionProgress,
            ElainaTrailVisualStyle.DefaultStarColor, fadeInEnd: 0f, fadeOutStart: GetProgress(ChannelFrame));
    }

    protected override void DrawWandTrailVisual(Vector2[] points, float actionProgress)
    {
        ElainaTrailVisualStyle.DrawTrail(points, Vector2.Zero, 0f, actionProgress,
            ElainaTrailVisualStyle.DefaultWandTrailColor, ElainaTrailVisualStyle.DefaultTrailMaxWidth,
            ElainaTrailVisualStyle.DefaultTrailEndWidth, fadeInEnd: 0f, fadeOutStart: GetProgress(ChannelFrame));
    }

    public Action_MultiMissile() : this(null) { }

    public Action_MultiMissile(MultiMissileSkill skill) : base(10)
    {
        castingSkill = skill;
    }

    public override void OnStart(ActionModPlayer actionPlayer)
    {
        base.OnStart(actionPlayer);
        actionToken = actionPlayer.CurrentActionToken;
        worldAimRotation = StartRotation * owner.gravDir;
        lastSentAimRotation = worldAimRotation;
        castingItem = owner.HeldItem;
        castingSlot = ElainaSkillModPlayer.CurrentSkillIndex;
        // 首枚在释放当帧预留位置，第 0 帧到保持点已过去 1 帧；间隔从预留时算起，与旧版挥杖节奏一致。
        framesSinceShot = ChannelFrame;
    }

    internal bool IsChannelingSkill(MultiMissileSkill skill) =>
        !ending && ReferenceEquals(castingSkill, skill)
        && owner.GetModPlayer<ActionModPlayer>().IsActionHolding && CanContinueCasting();

    private bool CanContinueCasting()
    {
        var skills = owner.GetModPlayer<ElainaSkillModPlayer>().ActiveSkill;
        return owner.active && !owner.dead && !owner.CCed && !owner.noItems
            && Main.mouseLeft && !owner.mouseInterface
            && ReferenceEquals(owner.HeldItem, castingItem)
            && ElainaSkillModPlayer.CurrentSkillIndex == castingSlot
            && castingSlot >= 0 && castingSlot < skills.Count
            && ReferenceEquals(skills[castingSlot]?.ModSkill, castingSkill)
            && owner.GetModPlayer<ElainaAttributeModPlayer>().UniqueMagicEnabled;
    }

    public override bool IsAutomaticHoldPoint(ActionModPlayer actionPlayer, int actionFrame) =>
        !ending && actionFrame == ChannelFrame;

    public override bool ShouldContinueHolding(ActionModPlayer actionPlayer, int actionFrame) =>
        !ending && CanContinueCasting();

    public override void Update(ActionModPlayer actionPlayer, int actionFrame, float actionProgress)
    {
        if (!ending && owner.whoAmI == Main.myPlayer && !CanContinueCasting())
            ending = true;

        UpdateLocalAim();
        ApplyPose();
        // 松手或换物品发生在首枚延迟节点前时，不再执行该节点。
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

        if (++framesSinceShot < MinimumShotInterval)
            return;
        if (!castingSkill.UpdateChannelShot(out bool fired))
            ending = true;
        if (fired)
            framesSinceShot = 0;
    }

    public override void OnHoldExit(ActionModPlayer actionPlayer, int actionFrame) => ending = true;
    public override void OnInterrupt(ActionModPlayer actionPlayer) => ending = true;
    public override void OnFinish(ActionModPlayer actionPlayer) => ending = true;

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
