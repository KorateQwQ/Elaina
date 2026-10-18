using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace 伊蕾娜.ElainaModAlchemy.UI;

/// <summary>Current pose of one specimen card. Tilt is in degrees; Time counts seconds since it was picked up, -1 when not selected.</summary>
internal readonly record struct AlchemyCardPose(float Tilt, float Lift, float Scale, float Selected, float Time);

// Pickup springs for the specimen cards, keyed by entry id. Selecting a card lifts it,
// grows it slightly and swings it upright with a small overshoot; the previous card
// settles back onto its own messy angle. Hover is a smaller version of the same pose.
internal sealed class AlchemyCardMotion
{
    // A card that is already selected when it first appears skips the pickup and starts mid-breath.
    internal const float SettledTime = 9;
    private const float Substep = 1f / 240;

    private sealed class Card
    {
        internal float Tilt, TiltVelocity, Lift, LiftVelocity, Scale = 1, ScaleVelocity, Selected, Time = -1;
        internal int Frame;
    }

    private readonly Dictionary<string, Card> _cards = new(StringComparer.Ordinal);
    private readonly List<string> _stale = [];
    private int _frame;
    private string _pickup;

    // Stable per-entry jitter in [0, 1), matching the HTML mockup.
    internal static float Jitter(int index, int salt)
    {
        double x = Math.Sin(index * 127.1 + salt * 311.7) * 43758.5453;
        return (float)(x - Math.Floor(x));
    }

    // Direction and magnitude are drawn separately so no card lies almost square: ±1.2–3.2°.
    internal static float BaseTilt(int number) => (Jitter(number, 3) < .5f ? -1 : 1) * (1.2f + 2 * Jitter(number, 5));
    internal static Vector2 BaseOffset(int number) => new((Jitter(number, 13) * 2 - 1) * 1.5f, (Jitter(number, 17) * 2 - 1) * 1.5f);

    private static (float Tilt, float Lift, float Scale) Target(int number, bool selected, bool hover)
        => selected ? (0, 6, 1.045f) : hover ? (BaseTilt(number) * .6f, 3, 1.015f) : (BaseTilt(number), 0, 1);

    internal static AlchemyCardPose Settled(int number, bool selected, bool hover)
    {
        var (tilt, lift, scale) = Target(number, selected, hover);
        return new(tilt, lift, scale, selected ? 1 : 0, selected ? SettledTime : -1);
    }

    // An explicit selection: if its card is not on the page yet (a jump into another category),
    // it appears at rest and is then picked up, instead of appearing already held.
    internal void PickUp(string id) => _pickup = id;

    internal AlchemyCardPose Pose(string id, int number, bool selected, bool hover)
        => _cards.TryGetValue(id, out var c) ? new(c.Tilt, c.Lift, c.Scale, c.Selected, c.Time) : Settled(number, selected, hover);

    internal void Advance(float seconds, IReadOnlyList<AlchemyCatalogRecipe> visible, string selectedId, string hoveredId)
    {
        seconds = Math.Max(0, seconds);
        _frame++;
        int steps = (int)MathF.Ceiling(seconds / Substep);
        float h = steps > 0 ? seconds / steps : 0;
        foreach (var recipe in visible)
        {
            int number = AlchemyNotebookPresentation.Number(recipe);
            bool selected = recipe.Id == selectedId, hover = !selected && recipe.Id == hoveredId;
            var (tilt, lift, scale) = Target(number, selected, hover);
            if (!_cards.TryGetValue(recipe.Id, out var c))
            {
                bool held = selected && recipe.Id != _pickup;
                var start = held || !selected ? (tilt, lift, scale) : Target(number, false, false);
                _cards[recipe.Id] = c = new Card { Tilt = start.Item1, Lift = start.Item2, Scale = start.Item3,
                    Selected = held ? 1 : 0, Time = held ? SettledTime : -1 };
            }
            if (!selected) c.Time = -1;
            else if (c.Time < 0) c.Time = 0;
            else c.Time += seconds;
            c.Frame = _frame;
            for (int i = 0; i < steps; i++)
            {
                // Underdamped while being picked up: the card swings past upright and back.
                Spring(ref c.Tilt, ref c.TiltVelocity, tilt, 300, selected ? 13 : 17, h);
                Spring(ref c.Lift, ref c.LiftVelocity, lift, 260, 17, h);
                Spring(ref c.Scale, ref c.ScaleVelocity, scale, 380, 19, h);
            }
            c.Selected += ((selected ? 1 : 0) - c.Selected) * (1 - MathF.Exp(-seconds * 14));
        }
        // Cards leaving the current category start from rest when they are shown again.
        foreach (var (id, c) in _cards) if (c.Frame != _frame) _stale.Add(id);
        foreach (string id in _stale) _cards.Remove(id);
        _stale.Clear();
        _pickup = null;
    }

    private static void Spring(ref float x, ref float v, float target, float k, float damping, float h)
    {
        v += (-k * (x - target) - damping * v) * h;
        x += v * h;
    }

    internal void Reset() { _cards.Clear(); _pickup = null; }
}
