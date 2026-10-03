using System;
using Microsoft.Xna.Framework;

namespace 伊蕾娜.ElainaModAlchemy.UI;

// Only selection changes animate. Scrolling, panel movement and UI scaling are
// applied through the selected card's current draw-time bounds without easing.
internal sealed class AlchemySelectionQuillMotion
{
    private string _selectedId;
    private Vector2 _target, _offset;
    internal bool Moving => _offset != Vector2.Zero;

    internal Vector2 ResolvePosition(string selectedId, Vector2 cardPosition,
        Vector2 contentOrigin, Vector2 scrollOffset, float scale)
    {
        Vector2 target = (cardPosition - contentOrigin - scrollOffset) / scale;
        if (_selectedId != null && _selectedId != selectedId)
            _offset += _target - target;
        _selectedId = selectedId;
        _target = target;
        return cardPosition + _offset * scale;
    }

    internal void Advance(float seconds)
    {
        _offset *= MathF.Exp(-Math.Max(0, seconds) * 12);
        if (_offset.LengthSquared() < .05f * .05f) _offset = Vector2.Zero;
    }

    internal void Reset()
    {
        _selectedId = null;
        _target = _offset = Vector2.Zero;
    }
}
