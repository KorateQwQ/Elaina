using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace 伊蕾娜.ElainaModSkills.ElainaSkillUI.ConstellationSkillPanel;

// Coordinates belong to the map, before camera pan, zoom or UI scaling.
internal sealed class ConstellationLayout
{
    public int Version { get; set; } = 1;
    public Dictionary<string, ConstellationPosition> Positions { get; set; } = [];
    internal const string AssetPath = "ElainaModSkills/ElainaSkillUI/ConstellationSkillPanel/Layout.json";
    internal const float MinCoordinate = 50, MaxCoordinate = 20000;
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    internal static ConstellationLayout Parse(string json)
    {
        var layout = JsonSerializer.Deserialize<ConstellationLayout>(json.TrimStart('\uFEFF'), Options);
        if (layout == null || layout.Version != 1 || layout.Positions == null)
            throw new InvalidDataException("不支持的布局格式或版本。");
        foreach (var (id, point) in layout.Positions)
            if (string.IsNullOrWhiteSpace(id) || point == null || !Valid(point.X) || !Valid(point.Y))
                throw new InvalidDataException("技能坐标必须是 50 到 20000 之间的有限数值。");
        return layout;
    }

    private static bool Valid(float n) => float.IsFinite(n) && n >= MinCoordinate && n <= MaxCoordinate;
    internal static ConstellationLayout Read(string path) => File.Exists(path) ? Parse(File.ReadAllText(path)) : new();
    internal static bool IsFileError(Exception e) => e is IOException or UnauthorizedAccessException or JsonException or InvalidDataException;

    internal void Save(string path)
    {
        // Serialize and validate before touching an existing layout. Rename only once the write succeeds.
        string json = JsonSerializer.Serialize(this, Options);
        Parse(json);
        string fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        string temporary = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, json + Environment.NewLine);
            File.Move(temporary, fullPath, true);
        }
        finally
        {
            try { if (File.Exists(temporary)) File.Delete(temporary); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    internal static ConstellationLayout Capture(ConstellationState state) => new()
    {
        Positions = state.Skills.ToDictionary(s => s.id, s => new ConstellationPosition { X = s.x, Y = s.y })
    };

    internal void Apply(ConstellationState state)
    {
        foreach (var node in state.Skills)
            if (Positions.TryGetValue(node.id, out var point)) { node.x = point.X; node.y = point.Y; }
        state.UpdateWorldSize();
    }
}

internal sealed record ConstellationPosition
{
    public float X { get; set; }
    public float Y { get; set; }
}

internal sealed class ConstellationLayoutEditor
{
    private readonly ConstellationState _state;
    private readonly ConstellationLayout _defaults;
    private readonly ConstellationLayout _saved;
    private ConstellationLayout _before;
    internal bool IsEditing => _before != null;
    internal bool HasChanges => IsEditing && _state.Skills.Any(s =>
        !_before.Positions.TryGetValue(s.id, out var p) || p.X != s.x || p.Y != s.y);

    internal ConstellationLayoutEditor(ConstellationState state, ConstellationLayout bundled, ConstellationLayout saved)
    {
        _state = state;
        bundled.Apply(state);
        _defaults = ConstellationLayout.Capture(state);
        _saved = saved;
        saved.Apply(state);
    }
    internal void Begin() { if (!IsEditing) _before = ConstellationLayout.Capture(_state); }
    internal void Cancel()
    {
        _before?.Apply(_state);
        _before = null;
    }
    internal void RestoreDefaults() { if (IsEditing) _defaults.Apply(_state); }

    internal void Move(string id, Vector2 start, Vector2 designDelta, float zoom, bool snap)
    {
        if (!IsEditing || _state.Find(id) is not { } node || !float.IsFinite(zoom) || zoom <= 0) return;
        Vector2 position = start + designDelta / zoom;
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y)) return;
        if (snap) position = new Vector2(MathF.Round(position.X / 10) * 10, MathF.Round(position.Y / 10) * 10);
        position = Vector2.Clamp(position, new Vector2(ConstellationLayout.MinCoordinate), new Vector2(ConstellationLayout.MaxCoordinate));
        node.x = MathF.Round(position.X, 2);
        node.y = MathF.Round(position.Y, 2);
        _state.UpdateWorldSize();
    }

    internal bool Save(string path, out string error)
    {
        error = null;
        if (!IsEditing) return false;
        // Keep entries for temporarily absent skills so saving a partial catalog does not erase them.
        var layout = new ConstellationLayout { Positions = new(_saved.Positions) };
        foreach (var (id, point) in ConstellationLayout.Capture(_state).Positions) layout.Positions[id] = point;
        try { layout.Save(path); }
        catch (Exception e) when (ConstellationLayout.IsFileError(e)) { error = e.Message; return false; }
        _saved.Positions = layout.Positions;
        _before = null;
        return true;
    }
}
