using System;
using Microsoft.Xna.Framework;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

// 目标交接：解析临界阻尼，保留位置/尺寸/透明度的速度，允许中途反向。
internal sealed class ReticleTracking
{
    public Vector2 Center { get; private set; }
    public float Radius { get; private set; }
    public float Opacity { get; private set; }
    Vector2 velocity, goal;
    float radiusVelocity, alphaVelocity, goalRadius;
    double previous;
    bool initialized;

    internal ReticleTracking() { }
    internal ReticleTracking(Vector2 center, float radius)
    {
        Center = goal = center;
        Radius = goalRadius = radius;
        Opacity = 1;
        initialized = true;
    }

    public void Update(double seconds, Vector2? target, float radius)
    {
        if (!initialized)
        {
            Center = goal = target ?? Vector2.Zero;
            Radius = goalRadius = radius;
            previous = seconds;
            initialized = true;
        }
        double dt = seconds - previous;
        if (dt < 0) throw new InvalidOperationException("目标动画时间倒退；重播应新建追踪状态。");
        previous = seconds;
        if (target.HasValue) { goal = target.Value; goalRadius = radius; }
        float x = Center.X, y = Center.Y, r = Radius, a = Opacity;
        Advance(ref x, ref velocity.X, goal.X, dt, 24);
        Advance(ref y, ref velocity.Y, goal.Y, dt, 24);
        Advance(ref r, ref radiusVelocity, goalRadius, dt, 22);
        Advance(ref a, ref alphaVelocity, target.HasValue ? 1 : 0, dt, 26);
        Center = new Vector2(x, y); Radius = r; Opacity = a;
    }

    static void Advance(ref float value, ref float velocity, float goal, double dt, double omega)
    {
        double offset = value - goal, b = velocity + omega * offset, decay = Math.Exp(-omega * dt);
        value = (float)(goal + (offset + b * dt) * decay);
        velocity = (float)((velocity - omega * b * dt) * decay);
    }

}
