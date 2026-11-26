using System;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

// 悬停使用独立单调时钟，确认后的世界特效使用控制器游戏时间，与齐射同步。
internal static class ReticleMotion
{
    public const double EnterSeconds = .32;
    public const double TightenSeconds = ShardbindSequence.TightenTicks / 60.0;
    public readonly record struct Pose(double Scale, double Angle, double Gap, double Alpha,
        double Presence, double Ambient, double Pulse, double Motes, double Time);

    public static double Smooth(double t)
    {
        t = Math.Clamp(t, 0, 1);
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    public static Pose Hover(double seconds)
    {
        double t = Math.Max(0, seconds), enter = Smooth(t / EnterSeconds);
        return new Pose(
            // ea33f91 的悬停收紧终值为 .86；1.6 仅用于进入，不能作为待确认常态。
            1.6 - .74 * enter + .014 * Math.Sin(t * 3.0) * enter,
            Math.PI / 4 + .20 * (1 - enter) + .055 * Math.Sin(t * 2.4) * enter,
            .52 - .14 * enter + .012 * Math.Sin(t * 3.0) * enter,
            enter, .78 + .07 * Math.Sin(t * 3.0), .018 * enter,
            (.18 + .07 * Math.Sin(t * 3.0)) * enter, .10 * enter, t);
    }

    // 确认时的形态及速度作为起点；快速点击也不先补完悬停动画。
    public static Pose Sample(double seconds, double? confirmedAt)
    {
        if (!confirmedAt.HasValue || seconds < confirmedAt.Value) return Hover(seconds);
        double confirm = confirmedAt.Value, elapsed = seconds - confirm;
        Pose from = Hover(confirm);
        const double epsilon = .00001;
        Pose before = Hover(Math.Max(0, confirm - epsilon)), after = Hover(confirm + epsilon);
        double divisor = confirm < epsilon ? confirm + epsilon : 2 * epsilon;
        double blend = Smooth(elapsed / TightenSeconds);
        double Bridge(double start, double previous, double next, double goal)
            => (start + (next - previous) / divisor * elapsed) * (1 - blend) + goal * blend;
        double collapse = TightenSeconds * .76;
        double scale = elapsed < collapse
            ? Hermite(from.Scale, (after.Scale - before.Scale) / divisor, .825, elapsed / collapse, collapse)
            : .825 + .035 * Smooth((elapsed - collapse) / (TightenSeconds - collapse));
        double fade = Smooth((ShardbindSequence.LockTicks / 60.0 - elapsed) / (8.0 / 60));
        return new Pose(scale,
            Bridge(from.Angle, before.Angle, after.Angle, 0),
            Bridge(from.Gap, before.Gap, after.Gap, .22),
            Math.Clamp(Bridge(from.Alpha, before.Alpha, after.Alpha, 1), 0, 1) * fade,
            Bridge(from.Presence, before.Presence, after.Presence, 1),
            Bridge(from.Ambient, before.Ambient, after.Ambient, .12),
            Bridge(from.Pulse, before.Pulse, after.Pulse, .6 + .4 * Math.Sin(seconds * 6)),
            Bridge(from.Motes, before.Motes, after.Motes, .55), seconds);
    }

    static double Hermite(double start, double velocity, double end, double t, double duration)
    {
        t = Math.Clamp(t, 0, 1);
        return (2*t*t*t - 3*t*t + 1)*start + (t*t*t - 2*t*t + t)*duration*velocity
            + (-2*t*t*t + 3*t*t)*end;
    }
}
