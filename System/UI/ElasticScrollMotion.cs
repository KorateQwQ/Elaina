using System;

namespace 伊蕾娜.System.UI;

// Positions and pointer coordinates are in page design units, independent of UI scale.
internal sealed class ElasticScrollMotion
{
    internal const float ElasticLimit = 32;
    internal float Position { get; private set; }
    internal float Target { get; private set; }
    internal float Maximum { get; private set; }
    internal bool Dragging { get; private set; }
    internal bool DidDrag { get; private set; }
    private bool _pressed;
    private float _pointerStart, _positionStart, _velocity, _wheelHold;

    internal void SetRange(float maximum)
    {
        maximum = Math.Max(0, maximum);
        if (maximum == Maximum) return;
        Maximum = maximum;
        Reset(Position);
    }

    internal void Reset(float position = 0)
    {
        Position = Target = Math.Clamp(position, 0, Maximum);
        _velocity = _wheelHold = 0;
        _pressed = Dragging = false;
    }

    internal void Reveal(float position)
    {
        EndDrag();
        Target = Math.Clamp(position, 0, Maximum);
        _wheelHold = 0;
    }

    internal void Wheel(float distance)
    {
        if (_pressed || Maximum <= 0) return;
        // Wheel feedback is gentler than a deliberate pull, leaving room to grab the page.
        float wheelLimit = ElasticLimit * .75f;
        Target = Math.Clamp(Target + distance, -wheelLimit, Maximum + wheelLimit);
        _wheelHold = .12f;
    }

    internal void BeginDrag(float pointer, bool allowed)
    {
        DidDrag = Dragging = false;
        _pressed = allowed && Maximum > 0;
        if (!_pressed) return;
        _pointerStart = pointer;
        // Invert the rubber band so grabbing an already displaced page cannot jump.
        float edge = Math.Clamp(Position, 0, Maximum), extra = Position - edge;
        _positionStart = edge + extra / Math.Max(.000001f, 1 - Math.Abs(extra) / ElasticLimit) / .45f;
        Target = Position;
        _velocity = _wheelHold = 0;
    }

    internal void DragTo(float pointer)
    {
        if (!_pressed) return;
        float delta = _pointerStart - pointer;
        if (!Dragging && Math.Abs(delta) < 4) return;
        DidDrag = Dragging = true;
        float raw = _positionStart + delta, edge = Math.Clamp(raw, 0, Maximum);
        float extra = (raw - edge) * .45f;
        Position = Target = edge + extra / (1 + Math.Abs(extra) / ElasticLimit);
    }

    internal void EndDrag()
    {
        if (!_pressed) return;
        _pressed = Dragging = false;
        Target = Math.Clamp(Position, 0, Maximum);
        _velocity = 0;
    }

    internal void Advance(float seconds)
    {
        if (_pressed || seconds <= 0) return;
        // Small steps keep the wheel's brief edge hold consistent at different frame rates.
        float remaining = Math.Min(seconds, .1f);
        while (remaining > 0)
        {
            float dt = Math.Min(remaining, 1f / 240);
            remaining -= dt;
            _wheelHold = Math.Max(0, _wheelHold - dt);
            if (_wheelHold == 0) Target = Math.Clamp(Target, 0, Maximum);
            const float spring = 20;
            float offset = Position - Target, impulse = _velocity + spring * offset;
            float decay = MathF.Exp(-spring * dt);
            Position = Target + (offset + impulse * dt) * decay;
            _velocity = (_velocity - spring * impulse * dt) * decay;
            // Repeated wheel input cannot build up an unbounded edge displacement.
            Position = Math.Clamp(Position, -ElasticLimit, Maximum + ElasticLimit);
        }
        if (_wheelHold == 0 && Math.Abs(Position - Target) < .01f && Math.Abs(_velocity) < .1f)
        {
            Position = Target;
            _velocity = 0;
        }
    }
}
