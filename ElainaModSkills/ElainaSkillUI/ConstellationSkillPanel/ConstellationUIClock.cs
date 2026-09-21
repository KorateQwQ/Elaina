using System.Diagnostics;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

// A body-owned monotonic clock, independent of Terraria's update cadence.
internal sealed class ConstellationUIClock
{
    private readonly Stopwatch _timer = new();
    private long _lastSampleTicks;

    internal void Restart()
    {
        _timer.Restart();
        _lastSampleTicks = 0;
    }

    internal void Stop()
    {
        _timer.Reset();
        _lastSampleTicks = 0;
    }

    internal float Sample()
    {
        if (!_timer.IsRunning) return 0;
        long now = _timer.ElapsedTicks;
        long elapsed = now - _lastSampleTicks;
        _lastSampleTicks = now;
        return (float)(elapsed / (double)Stopwatch.Frequency);
    }
}
