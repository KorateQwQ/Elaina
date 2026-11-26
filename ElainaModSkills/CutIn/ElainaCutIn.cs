using System;
using 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

namespace 伊蕾娜.ElainaModSkills.CutIn;

/// <summary>
/// 奥义特写：斜向特写带 + 伊蕾娜人像帧。只在本地玩家施放时播放，绘制在所有界面层之上（鼠标指针之下）。
/// 时长固定，与施法动作无关：按 Frames 顺序播放全部人像帧，播到 StrikeFrame 时冲击（闪白、杖头星芒），播完收拢退场。
/// 时间由绘制阶段的独立单调时钟（<see cref="ConstellationUIClock"/>）累计，不读 GameTime 或游戏 tick；
/// 游戏暂停时不累计，失焦、隐藏界面、回到菜单的间隔也不计入（由时钟的采样作废处理）。
/// 设计稿与参数对照见 Tools/CutInPreview（HTML 预览），人像帧由 Tools/CutInBake 烘焙。
/// </summary>
internal static class ElainaCutIn
{
    /// <summary>总开关。关闭时 Play 不播放，施法动作不会触发特写。</summary>
    internal static bool Enabled => true;

    /// <summary>人像相对源帧的放大倍率 = 基准 × 游戏 UI 缩放。</summary>
    internal static float BaseScale => 0.8f;

    // ---- 时间轴（秒，热重载后下次播放生效）----
    // 全部人像帧（源帧号, 秒），按顺序播放：1-5 转头抬眼，6-13 举杖，14 挥杖指向镜头，15-20 收势，21 定格。
    // 帧号即 ElainaModSkills/CutIn/png/frame-NNN.png 的序号；源帧 22-28 与 21 相同，烘焙时已去重，不要引用。
    internal static (int Frame, float Duration)[] Frames => new[]
    {
        (1, .05f), (2, .05f), (3, .05f), (4, .05f), (5, .05f),
        (6, .05f), (7, .05f), (8, .05f), (9, .05f), (10, .05f), (11, .05f), (12, .05f), (13, .15f),
        (14, .06f),
        (15, .05f), (16, .05f), (17, .05f), (18, .05f), (19, .05f), (20, .05f),
        (21, .10f),
    };
    internal static float IntroStart => .10f;       // 带子张开后开始播放人像帧
    internal static int StrikeFrame => 14;          // 播到这一帧时冲击：闪白、杖头星芒、人像锚点切换
    internal static float ExitDuration => .33f;     // 最后一帧播完后收拢退场

    /// <summary>已烘焙的人像帧号上限（CutIn_01 .. CutIn_NN）。</summary>
    internal const int MaxFrame = 21;

    private static readonly ConstellationUIClock Clock = new();
    private static bool playing;
    private static float time;

    internal static bool Active => playing;

    /// <summary>首次绘制时锁定的玩家头顶屏幕纵坐标（物理像素），避免镜头抖动带着带子晃。</summary>
    internal static float? PlayerHeadY;

    /// <summary>已播放的时间（秒），不含暂停、隐藏的时间。</summary>
    internal static float Time => time;

    /// <summary>冲击时刻 = StrikeFrame 开始显示的时间。</summary>
    internal static float StrikeTime => FrameStart(StrikeFrame);

    /// <summary>退场开始 = 全部帧播完。</summary>
    internal static float ExitStart => FrameStart(-1);

    internal static float TotalDuration => ExitStart + ExitDuration;

    /// <summary>开始播放（重新开始）。</summary>
    internal static void Play()
    {
        if (!Enabled) return;
        playing = true;
        time = 0;
        PlayerHeadY = null;
        Clock.Restart();
    }

    internal static void Stop()
    {
        playing = false;
        time = 0;
        Clock.Stop();
    }

    /// <summary>每次绘制前调用：从独立时钟取本次绘制间隔推进；暂停时只消耗采样、不累计。</summary>
    internal static void Advance(bool paused) => Advance(Clock.Sample(), paused);

    /// <summary>可测试的推进：elapsed 为两次绘制之间的真实秒数。</summary>
    internal static void Advance(float elapsed, bool paused)
    {
        if (!playing) return;
        if (!paused) time += Math.Max(0, elapsed);
        if (time >= TotalDuration) Stop();
    }

    /// <summary>某一时刻应显示的人像帧号（时间轴的纯函数）；开始前显示第一帧，播完后保持最后一帧。</summary>
    internal static int FrameAt(float t)
    {
        float acc = IntroStart;
        (int Frame, float Duration)[] frames = Frames;
        foreach ((int frame, float duration) in frames)
        {
            acc += duration;
            if (t < acc) return frame;
        }
        return frames[^1].Frame;
    }

    /// <summary>某帧开始显示的时间；frame 为 -1 时返回全部帧播完的时间。帧号不在列表中时抛出异常。</summary>
    internal static float FrameStart(int frame)
    {
        float acc = IntroStart;
        foreach ((int f, float duration) in Frames)
        {
            if (f == frame) return acc;
            acc += duration;
        }
        if (frame == -1) return acc;
        throw new InvalidOperationException($"Cut-in frame {frame} is not in ElainaCutIn.Frames");
    }
}
