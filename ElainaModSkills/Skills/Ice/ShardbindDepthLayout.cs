using System;
using KL.Drawing.ThreeD;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

internal static class ShardbindDepthLayout
{
    // 以正式球面布局为基准，仅四枚微调角度，其余保持原方向。
    // 此处仅决定原穿刺方向与终点；准备位置的外扩单独计算。
    public static Vector3 AdjustOffset(Vector3 offset, int index)
    {
        Vector2 angles = index switch
        {
            0 => new(.06f, 0f),
            7 => new(-.05f, .03f),
            12 => new(.05f, -.04f),
            14 => new(-.04f, 0f),
            _ => Vector2.Zero
        };
        Vector3 adjusted = Vector3.TransformNormal(offset, Matrix.CreateFromYawPitchRoll(angles.X, angles.Y, 0f));
        if (Math.Abs(adjusted.Length() - offset.Length()) > .001f
            || Vector3.Dot(Vector3.Normalize(offset), Vector3.Normalize(adjusted)) < MathF.Cos(MathHelper.ToRadians(6)))
            throw new InvalidOperationException("角度微调改变了原球面半径，或偏转超过6度。");
        return adjusted;
    }

    public static Vector3 ExpandSpawnOffset(Vector3 offset)
    {
        float depthFacing = MathF.Abs(Vector3.Normalize(offset).Z);
        // 侧向适度外扩，面向屏幕前后的冰刺沿原射线拉远更多。
        // XYZ 同比扩展，让模型本身的透视产生尺寸差异，并保持穿刺方向。
        float distanceScale = 1.45f + 2.6f * depthFacing * depthFacing * depthFacing;
        return offset * distanceScale;
    }



    public static Vector3 PullTowardCamera(Vector3 position, int index, Vector3 camera)
    {
        // 四枚前景沿相机视线拉近；左下方13号略浅于另外三枚，形成错落层次。
        // 保留屏幕分布，改变真正的三维深度，不放大模型。
        float depthFraction = index switch { 0 => .70f, 5 => .63f, 6 => .73f, 13 => .65f, _ => 0f };
        if (depthFraction == 0f) return position;
        if (position.Z >= 0f) throw new InvalidOperationException("近景冰锥必须来自镜头一侧。");
        float depth = camera.Z * depthFraction;
        float distanceRatio = (depth - camera.Z) / (position.Z - camera.Z);
        Vector3 pulled = camera + (position - camera) * distanceRatio;
        return pulled;
    }
}
