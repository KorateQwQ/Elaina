using System.Diagnostics;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

// A body-owned monotonic clock, independent of Terraria's update cadence.
internal sealed class ConstellationUIClock
{
    private readonly Stopwatch _timer = new();
    private long _lastSampleTicks;
    private static int _visibilityVersion;
    private int _sampleVersion;

    // Hidden UI receives no draw callbacks, so the client lifecycle invalidates
    // sampling on both sides of a visibility/focus transition.
    internal static void InvalidateSamples() => _visibilityVersion++;

    internal void Restart()
    {
        _timer.Restart();
        _lastSampleTicks = 0;
        _sampleVersion = _visibilityVersion;
    }

    internal void Stop()
    {
        _timer.Reset();
        _lastSampleTicks = 0;
    }

    internal float Sample()
    {
        if (!_timer.IsRunning) return 0;
        if (_sampleVersion != _visibilityVersion) { Restart(); return 0; }
        long now = _timer.ElapsedTicks;
        long elapsed = now - _lastSampleTicks;
        _lastSampleTicks = now;
        return (float)(elapsed / (double)Stopwatch.Frequency);
    }

    // Inactive frames stop the clock; the first resumed frame establishes a new origin.
    internal float Sample(bool active)
    {
        if (!active) { Stop(); return 0; }
        if (!_timer.IsRunning) { Restart(); return 0; }
        return Sample();
    }
}
