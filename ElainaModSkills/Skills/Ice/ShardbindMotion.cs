using System;
namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

// 与冰锥术相同的连续后撤/冲刺速度曲线；冰凌锁取冲刺的前五步。
internal sealed class ShardbindMotion
{
    Vector3 forwardVelocity, velocity;
    int time;
    const int BackwardFlyTime = 25, BurstFlyTime = 8;
    const float BackwardSpeedMultiplier = .5f;
    private void UpdateMovement()
    {
        if (forwardVelocity == Vector3.Zero)
        {
            return;
        }

        float fixedSpeed = forwardVelocity.Length();
        Vector3 forwardDirection = Vector3.Normalize(forwardVelocity);

        if (time < BackwardFlyTime)
        {
            float backwardProgress = time / (float)(BackwardFlyTime - 1);
            float currentSpeed = MathHelper.Lerp(fixedSpeed * BackwardSpeedMultiplier, 0f, backwardProgress);
            velocity = -forwardDirection * currentSpeed;
            return;
        }

        if (time < BackwardFlyTime + BurstFlyTime)
        {
            float burstProgress = (time - BackwardFlyTime) / (float)BurstFlyTime;
            float currentSpeed = MathHelper.Lerp(fixedSpeed * 4, fixedSpeed, burstProgress);
            velocity = forwardDirection * currentSpeed;
            return;
        }

        //velocity = forwardDirection * fixedSpeed;
    }
    public Vector3 Evaluate(Vector3 forward, int tick)
    {
        forwardVelocity = forward; time = tick; UpdateMovement(); return velocity;
    }
    public static (float Retreat, float Flight, float LastFlight) Measure()
    {
        var motion = new ShardbindMotion();
        float retreat = 0, flight = 0;
        for (int t = 0; t < BackwardFlyTime; t++) retreat -= motion.Evaluate(Vector3.UnitX, t).X;
        for (int t = 0; t < 5; t++) flight += motion.Evaluate(Vector3.UnitX, BackwardFlyTime + t).X;
        return (retreat, flight, motion.Evaluate(Vector3.UnitX, BackwardFlyTime + 4).X);
    }
}
