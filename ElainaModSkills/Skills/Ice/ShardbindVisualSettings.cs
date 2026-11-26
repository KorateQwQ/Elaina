using System;
using Microsoft.Xna.Framework;

namespace 伊蕾娜.ElainaModSkills.Skills.Ice;

/// <summary>
/// 冰凌锁视觉调参入口。表达式属性/方法在使用时重新求值，修改其方法体可供 C# 热重载使用。
/// 时间单位为游戏帧（60 帧/秒），距离为世界像素，角度为度：0 向右、90 向下。
/// 绘制参数下帧读取；出场表、数量、初速度、寿命在粒子生成时读取，建议下一次施法观察。
/// 伤害/冰封时间轴在 IceCycleBalance；本文件只调表现，不修改共享 KL Bloom。
/// </summary>
public static class ShardbindVisualSettings
{
    public static float DissolveEdgeHdr => 9f;

    // ==================== 1. 球体蓄光 ====================
    public static float SphereBaseHdrGain => 1.5f; // 随蓄光进度额外增加的模型亮度
    public static float SphereFresnelGain => 2.5f;
    public static Color SphereOuterColor => new(100, 200, 255, 0);
    public static Color SphereInnerColor => new(225, 245, 255, 0);
    public static Vector2 SphereOuterSize => new(.8f, 1.8f); // 初始/结束缩放
    public static Vector2 SphereInnerSize => new(.65f, 1.05f);
    public static float SphereOuterOpacity => .78f;
    public static float SphereInnerOpacity => .6f;

    // ==================== 2. 光线 / 光片 ====================
    // Delay：相对蓄光开始；Length 始终固定；HalfWidth：最终半宽。
    public readonly record struct Ray(float Angle, int Delay, float Length, float HalfWidth);
    public static int RayCount => 6;
    public static Ray GetRay(int index) => index switch
    {
        0 => new(-153.5527f, 4, 1420, 410),
        1 => new(16.04282f, 12, 1250, 320),
        2 => new(106.57015f, 20, 1190, 260),
        3 => new(-53.85803f, 26, 1580, 460),
        4 => new(154.6986f, 32, 1050, 190),
        5 => new(59.58761f, 38, 1390, 280),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
    public static Vector3 RayColor => new(1.14f, 1.68f, 2.1f);
    public static float RayHdr => 1.85f;
    public static float RayOpacity => .48f;
    public static float RayInitialHalfWidth => 3f;
    public static float RayRootHalfWidth => 3f;
    public static float RayRootOffset => 12f;
    public static float RayFadeInSpeed => 3f; // 越大越快达到满 Alpha
    public static float RayEdgeSoftness => .28f;
    public static float RayTailStart => .12f; // 长度比例；从这里开始向外端渐隐
    public static float RayRootFade => .025f;
    public static int FirstRayFrame
    {
        get
        {
            int delay = int.MaxValue;
            for (int i = 0; i < RayCount; i++) delay = Math.Min(delay, GetRay(i).Delay);
            return RayCount == 0 ? IceCycleBalance.LockChargeStartFrame : IceCycleBalance.LockChargeStartFrame + delay + 1;
        }
    }

    // ==================== 3. 蓄光连续十字星 ====================
    // Delay：相对首束光可见后 StarStartDelay；Offset：球心偏移；Angle：旋转角度。
    // Gain：单次强度倍率，可用来让某次更亮或更暗，不依赖数组排列顺序。
    public readonly record struct Star(int Delay, Vector2 Offset, float Angle, float Gain);
    public static int StarStartDelay => 18; // 首束光出现后约 0.3 秒
    public static int StarCount => 5;

    public static int startTime => 22;

    public static int interval => 5;
    public static Star GetStar(int index) => index switch
    {
        0 => new(startTime, new(-140, -140), 0, 1f),
        1 => new(startTime+index*interval, new(150, 180), 0, 1f),
        2 => new(startTime+index*interval, new(270,-20), 0, 1f),
        3 => new(startTime+index*interval, new(-280, -40), 0, 1f),
        4 => new(startTime+index*interval, new Vector2(-320, 380)*0.6f, 0, 1f),

        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
    public static int StarLife => 10;
    public static Vector2 StarHdrRange => new Vector2(6f, 8.25f); // 按出场表序号从 X 到 Y 插值
    public static Vector2 StarSizeRange => new Vector2(.66f, .685f)*0.4f;
    public static Vector3 StarColor => new(.67f, .88f, 1f);
    public static float StarDecayPower => 2.5f; // 越大衰减越快
    public static float StarShrink => .65f;
    public static Vector3 StarGlowColor => new(.48f, .76f, 1f);
    public static float StarGlowHdr => .7f;
    public static float StarGlowSize => .8f;

    // ==================== 4. 终爆中心打光 / 爆点 ====================
    public static Vector2 BurstShakeDirection => new(.28f, -.96f);
    public static float BurstShakeStrength => 11f;
    public static float BurstShakeFrequency => 9f;
    public static int BurstShakeTicks => 11;
    public static float BurstBlurStrength => .0085f;
    public static int BurstBlurTicks => 12;
    public static int BurstBlurSamples => 14;
    public static int BurstFlashDelay => 0; // 相对终爆；0 为爆炸当帧
    public static int BurstFlashLife => 54;
    public static float BurstFlashHdr => 14f;
    public static float BurstFlashSize => .95f;
    public static float BurstFlashAngle => 6.87549f;
    public static Vector3 BurstFlashColor => new(.67f, .88f, 1f);
    public static float BurstFlashDecayTicks => 5f; // 指数衰减时间；越小越短促
    public static float BurstFlashShrink => .65f;
    public static Vector3 BurstGlowColor => new(.48f, .76f, 1f);
    public static float BurstGlowHdr => 1.4f; // 宽柔光强度，独立于十字爆点
    public static float BurstGlowSize => 9f;

    // ==================== 5. 终爆向外飞散粒子 ====================
    // 各组可独立调整出场 Delay、数量、寿命/随机范围、方向/扇区、速度/随机比例、尺寸/随机范围、颜色。
    // Speed 是向量分量；当前 (v,v) 保留原预览速度，实际速率为 sqrt(2)*v。
    public readonly record struct BurstGroup(int Delay, int Count, int Life, int LifeJitter,
        Vector2 Speed, float SpeedJitter, float Angle, float Arc,
        Vector2 Size, Vector2 SizeJitter, Color Color, float Radius, float RadiusJitter);
    public static BurstGroup BurstSmoke => new(
        Delay: 0, Count: 30, Life: 30, LifeJitter: 15,
        Speed: new(2), SpeedJitter: .5f, Angle: 0, Arc: 360,
        Size: new(1.5f), SizeJitter: new(.5f), Color: new(200, 255, 255, 150),
        Radius: 10, RadiusJitter: 80);
    public static BurstGroup BurstPoint => new(
        Delay: 0, Count: 1, Life: 12, LifeJitter: 0,
        Speed: Vector2.Zero, SpeedJitter: 0, Angle: 0, Arc: 360,
        Size: new(2.16f), SizeJitter: Vector2.Zero, Color: new(100, 220, 255, 0),
        Radius: 0, RadiusJitter: 0);
    public static BurstGroup BurstStreaks => new(
        Delay: 0, Count: 10, Life: 16, LifeJitter: 5,
        Speed: new(18.75f), SpeedJitter: 0, Angle: 0, Arc: 360,
        Size: new(3.96f, 1.8f), SizeJitter: Vector2.Zero, Color: new(150, 220, 255, 0),
        Radius: 0, RadiusJitter: 10);
    public static BurstGroup BurstLines => new(
        Delay: 0, Count: 10, Life: 16, LifeJitter: 5,
        Speed: new(31.25f), SpeedJitter: 0, Angle: 0, Arc: 360,
        Size: new(3.96f, .36f), SizeJitter: Vector2.Zero, Color: new(150, 220, 255, 0),
        Radius: 0, RadiusJitter: 10);
    public static BurstGroup BurstFastLines => new(
        Delay: 0, Count: 14, Life: 20, LifeJitter: 4,
        Speed: new(35), SpeedJitter: 0, Angle: 0, Arc: 360,
        Size: new(5.2f, .55f), SizeJitter: Vector2.Zero, Color: new(185, 235, 255, 0),
        Radius: 12, RadiusJitter: 12);
    public static BurstGroup BurstAfterglow => new(
        Delay: 0, Count: 1, Life: 12, LifeJitter: 0,
        Speed: Vector2.Zero, SpeedJitter: 0, Angle: 0, Arc: 360,
        Size: new(6.3f), SizeJitter: Vector2.Zero, Color: new(100, 220, 255, 0),
        Radius: 0, RadiusJitter: 0);
    // 亮线使用冰凌锁专属绘制，不改所有技能共用的 LineSparkle。
    public static Vector2 BurstLineHdrRange => new(0f, 10f); // 寿命末端 / 出生峰值
    public static float BurstLineFadePower => 1f;
    public static float BurstLineDrag => .92f;

    // ==================== 6. 终爆冰碎 ====================
    public static int ShardDelay => 0;
    public static int ShardGroups => 3; // 每组 27 片
    public static float ShardAngle => 0f;
    public static float ShardSize => 2.5f;
    public static float ShardSparkSize => 1.45f;
    public static float ShardSpeed => 3.1f;
    public static float ShardLifeScale => 1.1f;
    public static float ShardMaxLifeSeconds => .84f;

    // 最晚的延迟发射结束后，控制器再保留 10 帧；粒子自行存活与衰减。
    public static int ControllerLifetime => IceCycleBalance.LockBurstFrame + 10 + Math.Max(0,
        Math.Max(Math.Max(BurstFlashDelay, ShardDelay), Math.Max(
            Math.Max(BurstSmoke.Delay, BurstPoint.Delay), Math.Max(
                Math.Max(BurstStreaks.Delay, BurstLines.Delay), Math.Max(BurstFastLines.Delay, BurstAfterglow.Delay)))));
}
