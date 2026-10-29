using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;

namespace 伊蕾娜.ElainaModAlchemy.Rain;

/// <summary>投掷与瞄准共用穿墙弹道和云层展开位置；只有展开后的雨水检查地形遮挡。</summary>
internal static class RainThrowTrajectory
{
    internal const int BottleWidth = 16, BottleHeight = 24;
    internal const float Gravity = .18f;

    internal static Vector2 Origin(Player player) => player.RotatedRelativePoint(player.MountedCenter);

    // ai[2] 在飞行时保存带重力方向的飞行帧数；展开后复用为展开高度。
    internal static Vector2 Aim(Vector2 origin, Vector2 target, float gravityDirection, out int flight)
    {
        target = Vector2.Clamp(target, new Vector2(16), new Vector2(Main.maxTilesX * 16 - 16, Main.maxTilesY * 16 - 16));
        int ticks = Math.Clamp((int)MathF.Ceiling(Vector2.Distance(origin, target) / 9f), 8, 60);
        flight = gravityDirection < 0 ? -ticks : ticks;
        Vector2 velocity = (target - origin) / ticks;
        // 每步先加重力再移动，因此总重力位移为 g*n*(n+1)/2。
        velocity.Y -= Math.Sign(flight) * Gravity * (ticks + 1) * .5f;
        return velocity;
    }

    internal static void Step(ref Vector2 center, ref Vector2 velocity, int flight)
    {
        velocity.Y += flight < 0 ? -Gravity : Gravity;
        center += velocity;
    }

    internal static Vector2 Predict(Vector2 origin, Vector2 velocity, int flight, List<Vector2> path)
    {
        path.Clear();
        path.Add(origin);
        Vector2 center = origin;
        for (int tick = 0; tick < Math.Abs(flight); tick++)
        {
            Step(ref center, ref velocity, flight);
            path.Add(center);
        }
        return center;
    }

    internal static Vector2 CloudCenter(Vector2 impact)
    {
        Vector2 center = impact;
        for (int i = 0; i < 12; i++)
        {
            Vector2 next = center - new Vector2(0, 8);
            if (next.Y < 48 || Collision.SolidCollision(next - new Vector2(24, 12), 48, 24)) break;
            center = next;
        }
        return center;
    }
}
