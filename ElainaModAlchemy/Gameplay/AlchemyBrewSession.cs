using System;

namespace 伊蕾娜.ElainaModAlchemy.Gameplay;

public enum AlchemyBrewPhase : byte { Idle, Reading, Stirring }

/// <summary>World-update clock. Closing the notebook does not own or cancel a brew.</summary>
public sealed class AlchemyBrewSession
{
    public const int FrameTicks = 8, ReadingFrames = 11, StirringFrames = 13;
    public const int DurationTicks = FrameTicks * StirringFrames * 2;
    public AlchemyBrewPhase Phase { get; private set; }
    public int Ticks { get; private set; }
    public bool NotebookOpen { get; private set; }
    public bool Active => Phase != AlchemyBrewPhase.Idle;
    public bool Brewing => Phase == AlchemyBrewPhase.Stirring;
    public int Frame => Ticks / FrameTicks % (Brewing ? StirringFrames : ReadingFrames);
    public float Progress => Brewing ? Math.Clamp(Ticks / (float)DurationTicks, .001f, .999f) : 0;

    public void Open()
    {
        NotebookOpen = true;
        if (!Active) SetPhase(AlchemyBrewPhase.Reading);
    }

    public void Close()
    {
        NotebookOpen = false;
        if (!Brewing) Cancel();
    }

    public bool Start()
    {
        if (Phase != AlchemyBrewPhase.Reading || !NotebookOpen) return false;
        SetPhase(AlchemyBrewPhase.Stirring);
        return true;
    }

    public bool Tick()
    {
        if (!Active) return false;
        Ticks++;
        return Brewing && Ticks == DurationTicks;
    }

    public void Complete() => SetPhase(NotebookOpen ? AlchemyBrewPhase.Reading : AlchemyBrewPhase.Idle);
    public void Cancel() => SetPhase(AlchemyBrewPhase.Idle);
    public void ApplyVisualState(AlchemyBrewPhase phase, int ticks)
    {
        Phase = phase;
        Ticks = Math.Clamp(ticks, 0, DurationTicks);
    }
    private void SetPhase(AlchemyBrewPhase phase) { Phase = phase; Ticks = 0; }
}
