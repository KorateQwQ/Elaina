using System.Runtime.CompilerServices;

namespace 伊蕾娜.ElainaModAlchemy.Purification;

/// <summary>只改 Read() 内的数值即可调参；每帧读取，不依赖静态初始化或重建 Effect。</summary>
internal readonly record struct PurificationDewVisualSettings(
    float Width, float Height, float HeadPosition, float FadeInTicks,
    float SwaySpeed, float SwayAmplitude, float SwayFrequency,
    float ColorSpeed, float HueOffset, float Saturation, float ColorSpan, float CrossColorSpan,
    float TipWidth, float BodyWidth, float TaperExponent,
    float TrailBrightness, float CoreBrightness, float CoreSharpness,
    float HeadWidth, float HeadLength, float HeadBrightness,
    float RingWidth, float RingHeight, float RingSharpness, float RingBrightness,
    float RingHueOffset, float RingColorSpan)
{
    // 修改下面这个方法体，然后在调试器中“应用热重载”。
    // 不要改成 static readonly/const 或缓存 Read() 的返回值，否则运行中不会重新取值。
    // 本次新增参数接口需要先完整构建、加载一次；以后调这些数值无需再次编译 .fx。
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static PurificationDewVisualSettings Read() => new(
        // 绘制尺寸（像素），不改变三格净化范围。
        Width: 24f,
        Height: 280f,
        HeadPosition: .90f,       // 露滴在绘制矩形中的纵向位置，通常保持不变
        FadeInTicks: 10f,

        // 摆动：速度越大越快，幅度越大偏移越明显，频率越大沿途弯折越密。
        SwaySpeed: 10.8f,
        SwayAmplitude: .08f,     // 相对绘制半宽；0 表示不摆动
        SwayFrequency: 5f,

        // 颜色：Speed 的负数反向、0 停止流动；0.1 为每秒移动 0.1 个色相周期。
        ColorSpeed: 1.10f,        // 同时控制拖尾和光环
        HueOffset: 0f,           // 整体色相偏移，1 为一个周期
        Saturation: .38f,        // 0 白色，1 鲜艳彩虹
        ColorSpan: .72f,         // 沿长度的色相跨度
        CrossColorSpan: .10f,    // 横向色相跨度

        // 渐缩：TipWidth 控制尾尖，BodyWidth 控制前端；指数越大越早收细。
        TipWidth: .00f,
        BodyWidth: .62f,
        TaperExponent: .72f,

        // HDR 亮度影响魔力飞弹同款共享 Bloom；降低亮度也会减弱辉光。
        TrailBrightness: 3.4f,
        CoreBrightness: .55f,
        CoreSharpness: 130f,      // 越大，中心光丝越细
        HeadWidth: .24f,
        HeadLength: .026f,
        HeadBrightness: 3.2f,
        RingWidth: .84f,
        RingHeight: .014f,
        RingSharpness: 5.5f,     // 越大，环线越细
        RingBrightness: .85f,   // 0 隐藏光环
        RingHueOffset: .17f,
        RingColorSpan: .65f
    );
}
