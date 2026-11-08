using System;
using Microsoft.Xna.Framework;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

/// <summary>
/// 操作被拒绝时让装配槽与装卸按钮左右轻晃。时间由面板的独立 UI 时钟推进，失焦时不前进。
/// </summary>
internal sealed class ConstellationDenyShake
{
    private static float Duration => .42f;
    private static float Amplitude => 4f;
    private static float Cycles => 3f;

    private float _age = float.MaxValue;
    internal int Slot { get; private set; } = -1;
    internal bool Active => _age < Duration;

    internal void Start(int slot) { Slot = slot; _age = 0; }
    internal void Advance(float seconds) { if (Active) _age += Math.Max(0, seconds); }
    internal void Reset() { Slot = -1; _age = float.MaxValue; }

    /// <summary>当前水平偏移（设计像素），衰减到 0。</summary>
    internal float Offset
    {
        get
        {
            if (!Active) return 0;
            float t = _age / Duration, fade = (1 - t) * (1 - t);
            return Amplitude * fade * MathF.Sin(t * Cycles * MathHelper.TwoPi);
        }
    }

    /// <summary>按整设备像素取偏移，晃动时保持静态元素原有的像素相位，文字和细线不会额外发糊。</summary>
    internal float SnappedOffset(float devicePerDesign) =>
        devicePerDesign > 0 ? MathF.Round(Offset * devicePerDesign) / devicePerDesign : Offset;
}
