using Microsoft.Xna.Framework;

namespace 伊蕾娜.ElainaModSkills.Skills.Wind;

// 修改 => 右侧的值，然后在调试器中应用 C# 热重载。
internal static class WindVortexSettings
{
    // 视觉倍率，1 为原尺寸，0 隐藏对应部分。
    internal static float BladeScale => 2f;          // 环绕风刃的半径和刃宽
    internal static float ParticleRangeScale => 2f;  // 细丝和叶片的轨迹半径
    internal static float CoreScale => 1f;           // 黑洞、折射和周围雾光

    // 粒子密度为倍率，1 对应 176 条粒子轨迹，0 关闭粒子。
    internal static float ParticleDensity => 0.5f;
    internal static Color ParticleColor => new(255, 150, 255);
    internal static Color LeafColor => new(243, 161, 200);
    // 同时控制粒子亮芯和外围柔光。0 保留粒子原色并关闭柔光。
    internal static float ParticleBloomStrength => 5.5f;

    internal static string BladeTexture => "KL/Effects/Tex/Trail/Eff_Trail_1692";
    internal static float TextureDensity => 1f;
    internal static float TextureFlow => .52f;
    internal static float InnerBrightness => 1.8f;
    internal static float CoreTension => 0.85f;
    internal static float RotationSpeed => -8f;
    internal static float DistortionFrequency => 4f;
    internal static float BloomStrength => 2f;

    // 以黑洞为圆心的渐变暗底，让亮背景下也有明暗层次。透明度为 0 时关闭。
    internal static float DarkBaseRadius => 760f;    // 黑洞局部像素，随 CoreScale 和开合缩放
    internal static float DarkBaseOpacity => .7f;    // 圆心处的遮盖度
    internal static float DarkBaseFalloff => 1.6f;   // 越小边缘越宽，越大越集中在圆心
    internal static Color DarkBaseColor => new(9, 3, 10);
}
