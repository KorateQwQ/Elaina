using System;
using KL.ActionsSystem;
using KL.DamageSystem;
using Terraria.DataStructures;
using 伊蕾娜.ElainaAttribute;
using 伊蕾娜.ElainaModSkills;
using 伊蕾娜.ElainaModSkills.Skills.Ice;
using 伊蕾娜.Items;

namespace 伊蕾娜.ElainaActions;

/// <summary>按住左键持续从身后随机位置发射冰锥，松手、切换或耗魔失败时收势。</summary>
public sealed class Action_IceCone : ElainaAction
{
    private const int ChannelFrame = 1;
    private const int RecoveryFrames = 20;
    // 轴线上的前后边界；用计算属性方便建立基线后热重载返回值。
    internal static float SpawnBackMin => -153f;
    internal static float SpawnBackMax => 80f;
    // 中心弧弦长的一半（不含厚度）；保持左右展开宽度，弧度单独调整。
    internal static float SpawnSideRange => 138f;
    // 完整圆心角，单位度：0 平直，90 四分之一圆弧，180 半圆弧。
    internal static float SpawnArcDegrees => 99f;
    internal static bool PreviewSpawnRange => false;
    // 沿玩家瞄准方向放置虚拟鼠标；真实鼠标远近不再参与每枚冰锥的角度修正。
    internal static float AimDistance => 160f;
    private const float MouseAimInfluence = 0.2f;
    private const float MaxMouseAimCorrection = 0.12f;

    private readonly IceConeSkill castingSkill;
    private readonly int damage;
    private readonly float manaCost;
    private readonly int intervalFrames;
    private readonly float freezeBuildup;
    private Item castingItem;
    private int castingSlot;
    private int framesSinceShot;
    private bool ending;
    private int actionToken;
    private float worldAimRotation;
    private float lastSentAimRotation;
    private int framesSinceAimSync;

    protected override void RequestActionStarBloom(float actionProgress)
    {
        // 保持在第 1 帧时已满亮，只有离开保持点后才进入 20 帧淡出。
        ElainaTrailVisualStyle.RequestBloomStar(StarCenter.Floor(), actionProgress,
            ElainaTrailVisualStyle.DefaultStarColor, fadeInEnd: 0f,
            fadeOutStart: ChannelFrame / (float)(ChannelFrame + RecoveryFrames));
    }

    protected override void DrawWandTrailVisual(Vector2[] points, float actionProgress)
    {
        ElainaTrailVisualStyle.DrawTrail(points, Vector2.Zero, 0f, actionProgress,
            ElainaTrailVisualStyle.DefaultWandTrailColor, ElainaTrailVisualStyle.DefaultTrailMaxWidth,
            ElainaTrailVisualStyle.DefaultTrailEndWidth, fadeInEnd: 0f,
            fadeOutStart: ChannelFrame / (float)(ChannelFrame + RecoveryFrames));
    }

    public Action_IceCone() : this(null, 0, 0f, 12, 0f) { }

    public Action_IceCone(IceConeSkill skill, int damage, float manaCost, int intervalFrames, float freezeBuildup)
        : base(ChannelFrame + RecoveryFrames + 1)
    {
        castingSkill = skill;
        this.damage = damage;
        this.manaCost = Math.Max(0f, manaCost);
        this.intervalFrames = Math.Max(1, intervalFrames);
        this.freezeBuildup = freezeBuildup;
        AddNode(CreateShotNode());
    }

    public override void OnStart(ActionModPlayer actionPlayer)
    {
        base.OnStart(actionPlayer);
        actionToken = actionPlayer.CurrentActionToken;
        worldAimRotation = StartRotation * owner.gravDir;
        lastSentAimRotation = worldAimRotation;
        castingItem = owner.HeldItem;
        castingSlot = ElainaSkillModPlayer.CurrentSkillIndex;
        framesSinceShot = ChannelFrame;
    }

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
        if (!ending)
            base.Update(actionPlayer, actionFrame, actionProgress);

        // 发射节点按冰锥的偏转速度改变 direction；恢复鼠标瞄准的姿势再计算杖尖。
        ApplyPose();
        SyncItemRotationToArmRotation();
    }

    public override void OnHoldUpdate(ActionModPlayer actionPlayer, int actionFrame, int holdTime)
    {
        UpdateLocalAim();
        ApplyPose();
        SyncItemRotationToArmRotation();
        if (owner.whoAmI != Main.myPlayer || ending || !CanContinueCasting())
            return;

        if (++framesSinceShot < intervalFrames)
            return;

        if (!owner.GetModPlayer<ElainaAttributeModPlayer>().ConsumeMagicPoint(manaCost))
        {
            FinishChannel(actionPlayer);
            return;
        }

        CreateShotNode().Update(actionPlayer, ChannelFrame, 0f);
        ApplyPose();
        SyncItemRotationToArmRotation();
        framesSinceShot = 0;
    }

    private ShootActionNode CreateShotNode()
    {
        Vector2 spawnPosition = default;
        bool hasSpawnPosition = false;
        Vector2 GetSpawnPosition()
        {
            if (!hasSpawnPosition)
            {
                spawnPosition = GetRandomSpawnPosition();
                hasSpawnPosition = true;
            }

            return spawnPosition;
        }

        return new ShootActionNode(
            ChannelFrame,
            ModContent.ProjectileType<IceConeProj>(),
            _ => GetSpawnPosition(),
            damage,
            2f,
            _ => GetShotDirection(worldAimRotation.ToRotationVector2(), GetSpawnPosition(),
                owner.MountedCenter + worldAimRotation.ToRotationVector2() * AimDistance) * 35f,
            projectile => projectile.GetGlobalProjectile<ElementalGlobalProjectile>().FreezeBuildup = freezeBuildup);
    }

    // mouseWorld 接收调用方构造的固定距离虚拟目标，保留原有轻微偏转和上限。
    internal static Vector2 GetShotDirection(Vector2 forward, Vector2 spawnPosition, Vector2 mouseWorld)
    {
        forward = forward.SafeNormalize(Vector2.UnitX);
        Vector2 toMouse = (mouseWorld - spawnPosition).SafeNormalize(forward);
        float difference = MathHelper.WrapAngle(toMouse.ToRotation() - forward.ToRotation());
        float correction = MathHelper.Clamp(difference * MouseAimInfluence,
            -MaxMouseAimCorrection, MaxMouseAimCorrection);
        return forward.RotatedBy(correction);
    }

    private Vector2 GetRandomSpawnPosition()
    {
        Vector2 offset = GetSpawnOffset(Main.rand.NextFloat(), Main.rand.NextFloat());
        Vector2 forward = worldAimRotation.ToRotationVector2();
        Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
        return owner.MountedCenter - forward * offset.X + side * offset.Y;
    }

    // X 为瞄准方向的后方距离，Y 为垂直方向；生成和预览共用此映射。
    // 两个独立均匀输入按面积分布在弧形带内，避免内缘过密。
    internal static Vector2 GetSpawnOffset(float across, float depth)
    {
        float near = SpawnBackMin;
        float far = Math.Max(near, SpawnBackMax);
        float halfWidth = Math.Max(0f, SpawnSideRange);
        double halfAngle = MathHelper.ToRadians(MathHelper.Clamp(SpawnArcDegrees, 0f, 180f)) * 0.5;
        across = MathHelper.Clamp(across, 0f, 1f);
        depth = MathHelper.Clamp(depth, 0f, 1f);
        if (halfAngle < 0.000001 || halfWidth <= 0f)
            return new Vector2(MathHelper.Lerp(near, far, depth), MathHelper.Lerp(-halfWidth, halfWidth, across));

        double radius = halfWidth / Math.Sin(halfAngle);
        double halfDepth = (far - near) * 0.5;
        double middle = (near + far) * 0.5;
        // 极窄且很厚的扇形，内缘最多收至圆心，不能越过圆心翻折。
        double innerOffset = Math.Max(-halfDepth, -radius);
        double innerDelta = innerOffset * (2.0 * radius + innerOffset);
        double outerDelta = halfDepth * (2.0 * radius + halfDepth);
        double delta = innerDelta + (outerDelta - innerDelta) * depth;
        double sampledRadius = Math.Sqrt(Math.Max(0.0, radius * radius + delta));
        // 避免很平的弧形中相近大数相减造成精度损失。
        double radialOffset = delta / (sampledRadius + radius);
        double angle = (across * 2.0 - 1.0) * halfAngle;
        double bend = 2.0 * radius * Math.Pow(Math.Sin(angle * 0.5), 2.0);
        return new Vector2((float)(middle - bend + radialOffset * Math.Cos(angle)),
            (float)((radius + radialOffset) * Math.Sin(angle)));
    }

    internal static void DrawSpawnRange(Vector2 center, float rotation)
    {
        Vector2 forward = rotation.ToRotationVector2();
        Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
        Vector2 origin = center - Main.screenPosition;
        float zoom = Math.Max(0.1f, Main.GameViewMatrix.Zoom.X);
        float thickness = 2f / zoom;
        Texture2D pixel = Terraria.GameContent.TextureAssets.MagicPixel.Value;
        Rectangle source = new(0, 0, 1, 1);
        Color border = new(100, 220, 255, 210);
        int segments = Math.Clamp((int)Math.Ceiling(Math.Max(0f, SpawnSideRange) * zoom / 3f), 24, 256);
        // 四条边使用同一映射：两条圆弧与两条径向边；不切换当前 Game 层的 SpriteBatch。
        for (int edge = 0; edge < 4; edge++)
        {
            int count = edge < 2 ? segments : 1;
            for (int i = 0; i < count; i++)
            {
                float t0 = i / (float)count;
                float t1 = (i + 1f) / count;
                Vector2 a = edge < 2 ? GetSpawnOffset(t0, edge) : GetSpawnOffset(edge - 2, t0);
                Vector2 b = edge < 2 ? GetSpawnOffset(t1, edge) : GetSpawnOffset(edge - 2, t1);
                Vector2 start = origin - forward * a.X + side * a.Y;
                Vector2 end = origin - forward * b.X + side * b.Y;
                Vector2 line = end - start;
                Main.spriteBatch.Draw(pixel, start, source, border, line.ToRotation(),
                    new Vector2(0f, 0.5f), new Vector2(line.Length(), thickness), SpriteEffects.None, 0f);
            }
        }

        Vector2 virtualMouse = center + forward * AimDistance;
        Vector2 marker = virtualMouse - Main.screenPosition;
        Color aimColor = new(255, 205, 105, 220);
        Main.spriteBatch.Draw(pixel, marker, source, aimColor, 0f, new Vector2(0.5f),
            new Vector2(14f / zoom, thickness), SpriteEffects.None, 0f);
        Main.spriteBatch.Draw(pixel, marker, source, aimColor, MathHelper.PiOver2, new Vector2(0.5f),
            new Vector2(14f / zoom, thickness), SpriteEffects.None, 0f);
        for (int i = 0; i < 9; i++)
        {
            Vector2 offset = GetSpawnOffset(i / 8f, 0.5f);
            Vector2 spawn = center - forward * offset.X + side * offset.Y;
            Vector2 direction = GetShotDirection(forward, spawn, virtualMouse);
            float length = Math.Max(0f, Vector2.Dot(virtualMouse - spawn, forward)
                / Math.Max(0.01f, Vector2.Dot(direction, forward)));
            Main.spriteBatch.Draw(pixel, spawn - Main.screenPosition, source, aimColor * 0.25f,
                direction.ToRotation(), new Vector2(0f, 0.5f), new Vector2(length, 1f / zoom), SpriteEffects.None, 0f);
        }
    }

    private void UpdateLocalAim()
    {
        if (ending || owner.whoAmI != Main.myPlayer)
            return;

        worldAimRotation = (Main.MouseWorld - owner.MountedCenter).ToRotation();
        if (Main.netMode == Terraria.ID.NetmodeID.SinglePlayer)
            return;

        framesSinceAimSync++;
        if (framesSinceAimSync < 3
            || MathF.Abs(MathHelper.WrapAngle(worldAimRotation - lastSentAimRotation)) < 0.04f)
            return;

        framesSinceAimSync = 0;
        lastSentAimRotation = worldAimRotation;
        owner.GetModPlayer<ElainaActionsModPlayer>().SyncIceConeAim(actionToken, worldAimRotation);
    }

    internal void ApplySyncedAim(int sourceActionToken, float rotation)
    {
        if (!ending && sourceActionToken == actionToken && owner.whoAmI != Main.myPlayer)
            worldAimRotation = rotation;
    }

    private void ApplyPose()
    {
        owner.direction = MathF.Cos(worldAimRotation) < 0f ? -1 : 1;
        float aimRotation = worldAimRotation * owner.gravDir;
        float armRotation = owner.direction == 1
            ? aimRotation - 1.8f
            : aimRotation + 1.8f - MathHelper.Pi;
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full,
            MathHelper.WrapAngle(armRotation));
    }

    public override void OnHoldExit(ActionModPlayer actionPlayer, int actionFrame) => ending = true;
    public override void OnInterrupt(ActionModPlayer actionPlayer) => ending = true;
    public override void OnFinish(ActionModPlayer actionPlayer) => ending = true;

    private void FinishChannel(ActionModPlayer actionPlayer)
    {
        if (ending || actionPlayer.CurrentActionToken != actionToken)
            return;

        ending = true;
        actionPlayer.SetCurrentActionHold(false, ChannelFrame);
    }
}
