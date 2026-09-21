using System;
using Microsoft.Xna.Framework;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

// The HTML's shared travel timeline and critically damped, reversible paper curl.
internal sealed class ConstellationBookMotion
{
    internal const float OpenSeconds = .560f, CloseSeconds = .440f;
    internal float Progress { get; private set; }
    internal bool TargetOpen { get; private set; }
    internal float CurlDirection { get; private set; } = 1;
    internal float CurlVelocity { get; private set; }
    internal bool Moving => Progress != (TargetOpen ? 1 : 0);
    internal bool IsOpen => TargetOpen && Progress == 1;
    internal bool IsClosed => !TargetOpen && Progress == 0;

    internal void Request(bool open)
    {
        if (TargetOpen == open) return;
        if (Progress is 0 or 1) { CurlDirection = open ? 1 : -1; CurlVelocity = 0; }
        TargetOpen = open;
    }

    internal void Advance(float seconds)
    {
        if (!Moving || !float.IsFinite(seconds) || seconds <= 0) return;
        float direction = TargetOpen ? 1 : -1;
        float offset = CurlDirection - direction, impulse = CurlVelocity + 32 * offset;
        float decay = MathF.Exp(-32 * seconds);
        CurlDirection = direction + (offset + impulse * seconds) * decay;
        CurlVelocity = (CurlVelocity - 32 * impulse * seconds) * decay;
        Progress = Math.Clamp(Progress + direction * seconds / (TargetOpen ? OpenSeconds : CloseSeconds), 0, 1);
        if (!Moving) Finish();
    }

    internal void Finish()
    {
        Progress = TargetOpen ? 1 : 0;
        CurlDirection = TargetOpen ? 1 : -1;
        CurlVelocity = 0;
    }

    internal void Reset() { TargetOpen = false; Finish(); }
    internal static float Ease(float value) { float t = Math.Clamp(value, 0, 1); return t * t * (3 - 2 * t); }
    internal float Turn => Ease((Progress - .26f) / .64f);
    internal float SceneAlpha => Ease(Progress / .04f);
    internal float PageAlpha => Ease((Progress - .3f) / .5f);
    internal float CoverAlpha => Progress == 1 ? 0 : 1 - Ease((Progress - .78f) / .2f);
    internal float ShadowAlpha => MathF.Sin(MathF.PI * Turn) * .45f;
    internal float BackdropAlpha => Ease((Progress - .08f) / .72f);
    internal float DockGlow => MathF.Sin(MathF.PI * Math.Min(1, Progress / .26f)) * .7f;

    // Everything is measured in 1100 x 800 design units, before the final UI scale.
    internal Matrix Pose(Vector2 dock, float dockScale)
    {
        float travel = Ease(Progress / .82f), tucked = 1 - travel;
        float lift = Math.Min(100, dock.Length() * .16f);
        float scale = dockScale + (1 - dockScale) * MathF.Pow(travel, 1.45f);
        var center = new Vector3(550, 400, 0);
        var perspectiveOrigin = new Vector3(385, 384, 0);
        Matrix perspective = Matrix.Identity;
        perspective.M34 = -1f / 5000;
        return Rotation(dock) * Matrix.CreateTranslation(-perspectiveOrigin) * perspective
            * Matrix.CreateTranslation(perspectiveOrigin) * Matrix.CreateTranslation(-center)
            * Matrix.CreateScale(scale, scale, 1) * Matrix.CreateTranslation(center)
            * Matrix.CreateTranslation(dock.X * tucked, dock.Y * tucked - lift * 4 * tucked * (1 - tucked), 0);
    }

    internal Matrix Rotation(Vector2 dock)
    {
        float tucked = 1 - Ease(Progress / .82f);
        float bank = tucked is 0 or 1 ? 0 : MathF.Pow(MathF.Sin(MathF.PI * tucked), 2);
        var center = new Vector3(550, 400, 0);
        return Matrix.CreateTranslation(-center)
            * Matrix.CreateRotationZ(MathHelper.ToRadians(9 * bank * Math.Sign(dock.X)))
            * Matrix.CreateRotationY(MathHelper.ToRadians(-7 * bank * Math.Sign(dock.X)))
            * Matrix.CreateRotationX(MathHelper.ToRadians(5 * bank)) * Matrix.CreateTranslation(center);
    }
}

internal static class ConstellationBookLayout
{
    internal static Rectangle DockBounds(Vector2 screen, Vector2 rack)
    {
        int x = (int)Math.Clamp(rack.X + 646 - 96, 8, Math.Max(8, screen.X - 104));
        int y = (int)Math.Clamp(rack.Y - 38, 8, Math.Max(8, screen.Y - 38));
        return new Rectangle(x, y, 96, 30);
    }
}
