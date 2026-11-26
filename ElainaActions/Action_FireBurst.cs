using System;
using KL.ActionsSystem;
using KL.Drawing;
using 伊蕾娜.ElainaModSkills.CutIn;
using 伊蕾娜.ElainaModSkills.Skills.Fire;

namespace 伊蕾娜.ElainaActions;

/// <summary>
/// 炎爆领域的施法动作：举杖聚火 → 火核挑向玩家上方 → 火种在空中成环 → 挥杖下指，火种坠向玩家脚下 → 星光散开并生成领域。
/// 时间轴为 FireBurstCastVisuals.Timeline 的设计帧，AnimationSpeed 缩放整段动作；总帧数随动作启动同步，其他端据此还原同一时间轴。
/// 火种全程以玩家为参考系：离杖点、悬停点与落点都相对玩家中心，玩家移动时整条轨迹跟着平移、形状不变（不贴地面）；
/// 爆燃那一帧玩家所在位置即领域中心（各端各自跟踪）。
/// 领域只由拥有者在爆燃帧生成，爆燃特效各端在同一动作帧各自生成。
/// 拥有者在指定动作帧通过回调播放奥义特写（ElainaCutIn）；特写时长固定，不跟随动作时间轴。
/// 魔杖拖尾不记录世界坐标历史，而是按手臂姿势在时间轴上回溯采样、以玩家为参照重建，弧线固定且不随移动变形。
/// </summary>
public sealed class Action_FireBurst : ElainaAction
{
    /// <summary>动画速度：1 为设计节奏（60 帧），大于 1 更快。决定动作总帧数，改后下次施放生效。</summary>
    public static float AnimationSpeed => 0.5f;

    // 手臂姿势：面朝右时的角度（弧度，y 向下，负值朝上），面朝左时镜像。热重载后立即生效。
    private static float RaiseAngle => -1.95f;    // 举杖过头，略向身后
    private static float FlickAngle => -1.72f;    // 挑出火核
    private static float StrikeAngle => 0.95f;    // 挥杖下指
    private static float SettleAngle => 0.75f;    // 爆燃后回落
    private static float RecoverAngle => 0.7f;

    // 魔杖拖尾：回溯的设计帧数（越旧越细越淡，到此帧数完全消失）与采样点数
    private static float WandTrailTicks => 14f;
    private static int WandTrailPointCount => 96;
    private static float WandTrailTaperLength => 12f;

    private static readonly Random MoteRandom = new();

    internal static int GetTotalFrames() =>
        Math.Max(1, (int)MathF.Round(FireBurstCastVisuals.Timeline.Total / Math.Max(0.05f, AnimationSpeed)));

    private readonly int damage;
    private readonly Action<Projectile> configureDomain;
    // center/feet 在爆燃前每帧跟随玩家；skyOffset、launchOffset 相对玩家中心。
    private Vector2 center, feet, skyOffset, launchOffset;
    private bool launched, impacted;
    private int facing = 1;
    private float aimRight;
    private FireBurstCastMote[] wandMotes, skyMotes;

    /// <summary>由引导射线时的右键快速施放发起（仅拥有端）；动作期间右键仍归快速施法，不打开技能轮盘。</summary>
    internal bool UsesQuickCastInput { get; }

    public Action_FireBurst() : this(0, null) { }

    /// <param name="damage">领域的伤害。</param>
    /// <param name="configureDomain">拥有者生成领域后、首个同步包发送前的配置。</param>
    /// <param name="quickCast">是否由快速施放发起。</param>
    public Action_FireBurst(int damage, Action<Projectile> configureDomain, bool quickCast = false) : base(GetTotalFrames())
    {
        this.damage = damage;
        this.configureDomain = configureDomain;
        UsesQuickCastInput = quickCast;
        AddNode(new CallbackActionNode(20, (_, _, _) =>
        {
            if (!Main.dedServ)
                ElainaCutIn.Play();
        }));
    }

    // 杖尖光效由施法演出自己绘制；拖尾按姿势重建（见 BuildWandTrail），不用基类的世界坐标历史
    protected override bool DrawActionStar => false;
    protected override bool DrawWandTrail => false;

    /// <summary>动作帧换算为设计帧；其他端用同步的总帧数换算，与拥有者一致。</summary>
    private float DesignTime(int actionFrame) => actionFrame * FireBurstCastVisuals.Timeline.Total / TotalFrame;

    public override void OnStart(ActionModPlayer actionPlayer)
    {
        base.OnStart(actionPlayer);
        skyOffset = new Vector2(0, -FireBurstCastVisuals.SkyHeight * owner.gravDir);
        launched = impacted = false;
        TrackOwner();
        float worldAim = StartRotation * owner.gravDir;
        facing = MathF.Cos(worldAim) < 0f ? -1 : 1;
        aimRight = ToFacingRight(worldAim);
        if (!Main.dedServ)
            FireBurstCastVisuals.CreateCastMotes(MoteRandom, out wandMotes, out skyMotes);
    }

    public override void Update(ActionModPlayer actionPlayer, int actionFrame, float actionProgress)
    {
        FireBurstCastTimeline tl = FireBurstCastVisuals.Timeline;
        float t = DesignTime(actionFrame);
        if (!impacted) TrackOwner();
        ApplyPose(t);
        base.Update(actionPlayer, actionFrame, actionProgress);

        if (!launched && t >= tl.Launch)
        {
            launched = true;
            launchOffset = WandCenter - center;
        }

        if (!impacted && t >= tl.Impact)
            Impact();
    }

    /// <summary>爆燃前落点跟随玩家：领域中心取玩家当前位置，火种落在玩家脚下。</summary>
    private void TrackOwner()
    {
        center = owner.Center;
        feet = center + new Vector2(0, owner.height / 2f * owner.gravDir);
    }

    private void Impact()
    {
        impacted = true;
        FireBurstImpactDust.Emit(center, feet);
        SpawnDomain(playImpact: false);
    }

    private void SpawnDomain(bool playImpact)
    {
        if (owner.whoAmI != Main.myPlayer) return;
        ShootFromAction(owner, ModContent.ProjectileType<FireBurstDomain>(), center, Vector2.Zero, damage, 2f, projectile =>
        {
            configureDomain?.Invoke(projectile);
            ((FireBurstDomain)projectile.ModProjectile).PlayImpact = playImpact;
        });
    }

    // 未到爆燃帧就被中断或结束时立即生成领域（由领域自己播放爆燃），不浪费已消耗的魔力。
    public override void OnInterrupt(ActionModPlayer actionPlayer)
    {
        if (!impacted)
        {
            impacted = true;
            SpawnDomain(playImpact: true);
        }
        base.OnInterrupt(actionPlayer);
    }

    public override void OnFinish(ActionModPlayer actionPlayer)
    {
        if (!impacted)
        {
            impacted = true;
            SpawnDomain(playImpact: true);
        }
        base.OnFinish(actionPlayer);
    }

    /// <summary>悬停点始终在玩家中心上方：坠落时也不锁定在世界坐标，否则玩家移动时坠落轨迹会歪斜。</summary>
    private Vector2 Sky() => center + skyOffset;

    /// <summary>
    /// 火种位置只按时间判断是否已离杖。绘制取的动作帧比 Update 处理过的帧多一帧（KL 在 Update 之后才推进帧号），
    /// 绘制时间刚到 Launch 时 Update 尚未记录 launchOffset：此时火种就在当前杖尖。
    /// 不能用 launched 标记判断，否则这一帧会画在天空悬停点，看起来火种先闪到天上再回到杖尖。
    /// </summary>
    private Vector2 Seed(float t) =>
        t >= FireBurstCastVisuals.Timeline.Launch
            ? FireBurstCastVisuals.SeedPosition(t, launched ? center + launchOffset : WandCenter, Sky(), feet)
            : Sky();

    /// <summary>坠落开始前火种相对玩家中心的位置，只取决于时间，供手臂指向使用（坠落后停在悬停点）。</summary>
    private Vector2 SeedLocal(float t) =>
        FireBurstCastVisuals.SeedPosition(Math.Min(t, FireBurstCastVisuals.Timeline.Fall), launchOffset, skyOffset, Vector2.Zero);

    private float ToFacingRight(float worldAngle) => facing == 1 ? worldAngle : MathHelper.WrapAngle(MathHelper.Pi - worldAngle);

    private static float LerpAngle(float from, float to, float k) => from + MathHelper.WrapAngle(to - from) * k;

    /// <summary>
    /// 举杖 → 挑出火核 → 指向火种 → 挥杖下指 → 回落。火种相对玩家的位置只随时间变化，姿势是时间的纯函数，拖尾据此回溯。
    /// 指向火种的角度以玩家当前中心为参照（不能用爆燃后不再更新的 center，否则爆燃后移动时回溯的挥杖会转向）。
    /// </summary>
    private float ArmAngle(float t)
    {
        FireBurstCastTimeline tl = FireBurstCastVisuals.Timeline;
        float raiseEnd = tl.Launch - 2, flickEnd = tl.Launch + 1, trackStart = tl.Arrive - 2, strikeEnd = tl.Fall + 3, settleEnd = tl.Impact + 8;
        static float OutCubic(float x) => 1 - MathF.Pow(1 - Math.Clamp(x, 0, 1), 3);
        static float InQuad(float x) => Math.Clamp(x, 0, 1) * Math.Clamp(x, 0, 1);
        float SeedAngle() => ToFacingRight((SeedLocal(t) - (ArmCenter - owner.Center)).ToRotation());

        // 瞄准角在 [-π/2, π/2]，直接插值保证从身前举起；最短角插值在鼠标朝下较陡时会从身后绕上去
        if (t <= raiseEnd) return MathHelper.Lerp(aimRight, RaiseAngle, OutCubic(t / raiseEnd));
        if (t <= flickEnd) return LerpAngle(RaiseAngle, FlickAngle, OutCubic((t - raiseEnd) / (flickEnd - raiseEnd)));
        if (t <= trackStart) return LerpAngle(FlickAngle, SeedAngle(), MathHelper.SmoothStep(0, 1, (t - flickEnd) / (trackStart - flickEnd)));
        if (t <= tl.Fall) return SeedAngle();
        if (t <= strikeEnd) return LerpAngle(SeedAngle(), StrikeAngle, InQuad((t - tl.Fall) / (strikeEnd - tl.Fall)));
        if (t <= settleEnd) return LerpAngle(StrikeAngle, SettleAngle, MathHelper.SmoothStep(0, 1, (t - strikeEnd) / (settleEnd - strikeEnd)));
        return LerpAngle(SettleAngle, RecoverAngle, MathHelper.SmoothStep(0, 1, (t - settleEnd) / Math.Max(1, tl.Total - settleEnd)));
    }

    private float ArmRotation(float aR)
    {
        float worldAngle = facing == 1 ? aR : MathHelper.Pi - aR;
        float aimRotation = worldAngle * owner.gravDir;
        return MathHelper.WrapAngle(facing == 1 ? aimRotation - 1.8f : aimRotation + 1.8f - MathHelper.Pi);
    }

    private void ApplyPose(float t)
    {
        owner.direction = facing;
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, ArmRotation(ArmAngle(t)));
    }

    /// <summary>与 SyncItemRotationToArmRotation 相同的换算：手臂姿势角 → 魔杖朝向（itemRotation）。</summary>
    private float WandRotation(float aR)
    {
        float rotation = ArmRotation(aR) * owner.gravDir;
        float armTextureDirection = facing == 1 ? 1.8f : MathHelper.Pi - 1.8f;
        return (rotation + armTextureDirection) * owner.gravDir;
    }

    /// <summary>
    /// 魔杖拖尾：回溯 WandTrailTicks 设计帧的杖尖轨迹，相对当前肩部（ArmCenter）的偏移，由新到旧。
    /// 只回溯已经发生的动作，起手不足 WandTrailTicks 时把采样均匀铺在已有时间内，避免零帧重复点把收尖挤在同一位置。
    /// 按时间等距采样：TrailEffect 的宽度与透明度按点序号收细，序号即相对年龄，挥过的部分随时间逐渐变细变淡，与普通拖尾一致；
    /// 不能按弧长重采样，否则收细只跟剩余几何有关，挥杖后整条弧保持原样、尾巴掠过时才突然消失。
    /// 手臂停住的时段会产生重合点（法线无效），沿轨迹方向错开极小距离。整条轨迹几乎不动时返回 null 不画。
    /// </summary>
    private Vector2[] BuildWandTrail(float t)
    {
        int count = Math.Max(2, WandTrailPointCount);
        float reach = 45f;          // 与 WandCenter 的杖长一致
        float minLength = 2f;       // 轨迹总长低于此值不画
        float epsilon = 0.001f;     // 重合点错开的距离（像素）
        float history = Math.Clamp(t, 0f, WandTrailTicks);
        var points = new Vector2[count];
        float total = 0;
        for (int i = 0; i < count; i++)
        {
            float ti = Math.Max(0, t - history * i / (count - 1));
            points[i] = (WandRotation(ArmAngle(ti)) + owner.fullRotation).ToRotationVector2() * reach;
            if (i > 0) total += Vector2.Distance(points[i - 1], points[i]);
        }
        if (total < minLength) return null;

        // 回退方向：第一段非零位移的方向（由新到旧）
        Vector2 direction = Vector2.Zero;
        for (int i = 1; i < count && direction == Vector2.Zero; i++)
            if (Vector2.DistanceSquared(points[i - 1], points[i]) > epsilon * epsilon)
                direction = Vector2.Normalize(points[i] - points[i - 1]);
        for (int i = 1; i < count; i++)
        {
            Vector2 step = points[i] - points[i - 1];
            if (step.LengthSquared() > epsilon * epsilon) direction = Vector2.Normalize(step);
            else points[i] = points[i - 1] + direction * epsilon;
        }
        return points;
    }

    /// <summary>保持按年龄衰减，并限制尾部的空间收尖斜率；停杖产生的重复点不能把宽度挤成平截口。</summary>
    private static float[] BuildWandTrailWidths(Vector2[] points)
    {
        var widths = new float[points.Length];
        float remaining = 0f;
        for (int i = points.Length - 2; i >= 0; i--)
        {
            remaining += Vector2.Distance(points[i], points[i + 1]);
            float ageFade = 1f - i / (float)(points.Length - 1);
            widths[i] = ElainaTrailVisualStyle.DefaultTrailMaxWidth * 0.5f * Math.Min(ageFade, remaining / WandTrailTaperLength);
        }
        return widths;
    }

    protected override void SubmitExtraVisuals(ActionModPlayer actionPlayer, int actionFrame, float actionProgress)
    {
        FireBurstCastTimeline tl = FireBurstCastVisuals.Timeline;
        float t = DesignTime(actionFrame);

        Vector2[] trail = BuildWandTrail(t);
        Vector2 trailAnchor = ArmCenter;
        if (trail != null)
            SubmitTrail(new ElainaTrailSnapshot(trail, trailAnchor, 0f, actionProgress,
                ElainaTrailVisualStyle.FireWandColor, ElainaTrailVisualStyle.DefaultTrailMaxWidth,
                ElainaTrailVisualStyle.DefaultTrailEndWidth, widths: BuildWandTrailWidths(trail)));

        // 同理，绘制时间先于 Update 到达 Impact 的那一帧爆燃还没生成：火种停在落点多画一帧，不留空白帧。
        if (impacted) return;
        t = Math.Min(t, tl.Impact - 0.001f);

        // 杖尖火核、火种与火环在玩家之前
        var frame = new FireBurstCastFrame
        {
            Time = t,
            WandTip = WandCenter - Main.screenPosition,
            Seed = Seed(t) - Main.screenPosition,
            SeedTrail = FireBurstCastVisuals.SeedTrailPath(t, x => Seed(x) - Main.screenPosition),
            Sky = Sky() - Main.screenPosition,
            WandMotes = wandMotes,
            SkyMotes = skyMotes,
        };
        LayerDrawRequestSystem.RequestBloom("Elaina:FireBurstCast", LayerDrawRequestSystem.DrawTargetLayer.OverPlayers,
            LayerDrawRequestSystem.DrawTiming.Before, () =>
            {
                // 直通 Alpha 混合：Shader 在采样后还原预乘贴图，亮度与覆盖度分开控制。
                EndBeginDraw(2, 1, ss: SamplerState.LinearClamp);
                FireBurstCastVisuals.DrawCast(Main.spriteBatch, FireBurstImpactDust.Textures, frame,
                    FireBurstImpactDust.SetCastIntensity);
                EndBeginDraw();
            }, order: LayerDrawRequestSystem.DustOrder + 2);
    }
}
