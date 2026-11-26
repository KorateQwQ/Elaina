namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

// 每个准星拥有独立状态。确认时转移此对象，保留临界阻尼的位置、半径及透明度速度。
internal sealed class ReticlePresentation
{
    internal ReticleTracking Tracking { get; }
    internal double Seconds { get; private set; }
    internal double? ConfirmedAt { get; private set; }
    private Vector2? lastTarget;
    private float lastRadius;

    internal ReticlePresentation(Vector2 center, float radius, bool confirmed = false)
    {
        Tracking = confirmed ? new ReticleTracking(center, radius) : new ReticleTracking();
        lastTarget = center;
        lastRadius = radius;
        Tracking.Update(0, center, radius);
        if (confirmed)
        {
            // 远端与无悬停直接施法从常态开始，不重复播放大框入场。
            Seconds = .75;
            Tracking.Update(Seconds, center, radius);
            Confirm();
        }
    }

    internal void Advance(double delta, Vector2? target, float radius)
    {
        Seconds += delta;
        Tracking.Update(Seconds, lastTarget, lastRadius);
        Tracking.Update(Seconds, target, radius);
        lastTarget = target;
        lastRadius = radius;
    }

    internal void Confirm() => ConfirmedAt = Seconds;

    internal ReticleMotion.Pose Sample(float elapsedTicks = -1)
        => ReticleMotion.Sample(ConfirmedAt.HasValue ? ConfirmedAt.Value + elapsedTicks / 60.0 : Seconds, ConfirmedAt);
}
