namespace 伊蕾娜.System.UI;

/// <summary>A gesture that enters a SUI stays captured through its release frame.</summary>
internal sealed class UIPointerCapture
{
    private bool _left, _right, _middle;
    public bool IsCaptured => _left || _right || _middle;

    public bool Update(bool enabled, bool hovering, bool left, bool right, bool middle)
    {
        if (!enabled) { Reset(); return false; }
        bool block = hovering || IsCaptured;
        _left = left && (_left || hovering);
        _right = right && (_right || hovering);
        _middle = middle && (_middle || hovering);
        return block;
    }

    public void Reset() => _left = _right = _middle = false;
}
