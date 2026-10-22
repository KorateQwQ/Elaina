using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaModSkills.Skills.Fire;

/// <summary>一颗火星的运动状态（相对龙卷中心，缩放 1）。</summary>
public struct FireTornadoSpark
{
    public float Angle, AngularSpeed;
    public float HeightRatio, Rise;
    public float RadiusScale, Outward;
    public float Age, Life;
    public float Size, Brightness;
    public bool Hot;
}

/// <summary>
/// 火焰龙卷周围螺旋上升、飞舞的火星的运动与绘制。只依赖 XNA，游戏中的 FireTornadoSparkDust
/// 与离线预览 Tools/FireTornadoLookPreview 共用。粒子绕龙卷竖直轴旋转（与贴图旋转方向一致：正面向右），
/// 半径沿高度跟随龙卷轮廓；背面的粒子应在龙卷本体之前绘制、被本体遮挡，正面的粒子在之后绘制。
/// </summary>
public static class FireTornadoSparks
{
    /// <summary>龙卷完全显现时每帧生成的火星数量</summary>
    public const float SpawnPerFrame = 6f;

    // 龙卷可见轮廓（相对弹幕中心，像素，缩放 1）：底部、顶部高度与半径
    private const float BottomY = 320f;
    private const float Height = 770f;
    private const float BottomRadius = 110f;
    private const float TopRadiusGain = 120f;
    // 轨道椭圆的纵向倾斜，正面略低，与气流圈的俯视角一致
    private const float OrbitTilt = 0.12f;

    public static FireTornadoSpark Create(Random random)
    {
        float Next() => (float)random.NextDouble();
        float Range(float min, float max) => min + (max - min) * Next();

        float r = Next();
        return new FireTornadoSpark
        {
            Angle = Next() * MathHelper.TwoPi,
            // 正值使正面粒子向右移动，与龙卷贴图转向一致
            AngularSpeed = MathHelper.TwoPi * Range(0.55f, 1.2f) / 60f,
            // 偏向下半部生成，随后螺旋上升
            HeightRatio = r * r * 0.85f,
            Rise = Range(0.004f, 0.011f),
            RadiusScale = Range(1.0f, 1.55f),
            Outward = Range(0.002f, 0.008f),
            Life = Range(45f, 95f),
            // 少量大颗火星
            Size = Next() < 0.1f ? Range(4.5f, 6.5f) : Range(2f, 4f),
            Brightness = Range(0.55f, 1f),
            Hot = Next() < 0.15f,
        };
    }

    /// <summary>推进一帧，返回火星是否仍存活。</summary>
    public static bool Step(ref FireTornadoSpark spark)
    {
        spark.Age++;
        spark.Angle += spark.AngularSpeed;
        spark.HeightRatio += spark.Rise;
        spark.RadiusScale += spark.Outward;
        // 越往上越容易被甩出去
        spark.Outward *= 1.015f;
        return spark.Age < spark.Life && spark.HeightRatio <= 1.25f;
    }

    /// <summary>是否位于龙卷正面（朝向镜头）。</summary>
    public static bool IsFront(in FireTornadoSpark spark) => MathF.Cos(spark.Angle) >= 0;

    /// <summary>相对龙卷中心的位置（缩放 1）。</summary>
    public static Vector2 Offset(in FireTornadoSpark spark) => Project(spark.Angle, spark.HeightRatio, spark.RadiusScale);

    /// <summary>
    /// 绘制一颗火星。anchor 为龙卷中心的绘制坐标（世界坐标减屏幕位置），dot 为实心圆点贴图（FireTornadoSparkDust.png）。
    /// 主体为实心圆点；颜色不超过 1，HDR 倍率由调用方的 shader（如 KL ReColor）乘上，
    /// 外圈光晕完全交给 Bloom，主体本身保持清晰。建议 Additive 混合。
    /// </summary>
    public static void Draw(SpriteBatch batch, Texture2D dot, Vector2 anchor, in FireTornadoSpark spark, float scale = 1f)
    {
        float depth = MathF.Cos(spark.Angle);
        Vector2 position = Project(spark.Angle, spark.HeightRatio, spark.RadiusScale);
        // 拖尾长度约为 0.8 帧的位移，保持圆点感
        Vector2 previous = Project(spark.Angle - spark.AngularSpeed * 0.8f, spark.HeightRatio - spark.Rise * 0.8f,
            spark.RadiusScale - spark.Outward * 0.8f);
        Vector2 motion = position - previous;

        float life = spark.Age / spark.Life;
        float fade = MathHelper.Clamp(life / 0.12f, 0f, 1f) * MathHelper.Clamp((1f - life) / 0.45f, 0f, 1f);
        // 背面的火星较暗较小
        float near = 0.55f + 0.45f * (depth * 0.5f + 0.5f);
        float alpha = fade * spark.Brightness * near;
        if (alpha <= 0.01f)
            return;

        // 乘上较高的 HDR 倍率后核心截断为偏黄，溢出部分经 Bloom 保留橙色；变暗时自然退为橙红
        Color color = spark.Hot ? new Color(1f, 0.6f, 0.25f) : new Color(1f, 0.38f, 0.08f);
        float thickness = spark.Size * (0.8f + 0.2f * near) * scale;
        batch.Draw(dot, anchor + position * scale, null, color * alpha, MathF.Atan2(motion.Y, motion.X),
            new Vector2(dot.Width / 2f, dot.Height / 2f),
            new Vector2((motion.Length() * scale + thickness) / dot.Width, thickness / dot.Height), SpriteEffects.None, 0f);
    }

    private static Vector2 Project(float angle, float heightRatio, float radiusScale)
    {
        float h = Math.Max(heightRatio, 0f);
        float radius = (BottomRadius + TopRadiusGain * MathF.Pow(h, 1.5f)) * radiusScale;
        float depth = MathF.Cos(angle);
        return new Vector2(MathF.Sin(angle) * radius, BottomY - heightRatio * Height + depth * radius * OrbitTilt);
    }
}
