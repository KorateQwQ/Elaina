using System;
using System.Collections.Generic;
using System.Reflection;
using SilkyUIFramework;
using SilkyUIFramework.Attributes;
using Terraria.UI;

namespace 伊蕾娜.System.UI;

/// <summary>Resolve SUI registration metadata against the actual interface layer order.</summary>
internal sealed class SUIInputLayerOrder
{
    private readonly Dictionary<string, int> _layersByName = [];
    private readonly Dictionary<string, List<int>> _suiLayers = [];
    private readonly Dictionary<Type, (bool Global, string Name, string Anchor)> _registrations = [];
    private IReadOnlyList<GameInterfaceLayer> _layers = Array.Empty<GameInterfaceLayer>();
    private bool _dirty;

    public void Update(IReadOnlyList<GameInterfaceLayer> layers)
    {
        // Keep the shared list until Draw starts, so later mods' layer edits are included.
        _layers = layers;
        _dirty = true;
    }

    private void Refresh()
    {
        if (!_dirty) return;
        _dirty = false;
        _layersByName.Clear();
        _suiLayers.Clear();
        for (int i = 0; i < _layers.Count; i++)
        {
            var layer = _layers[i];
            if (!layer.Active) continue;
            _layersByName.TryAdd(layer.Name, i);
            if (layer is not SilkyUILayer) continue;
            if (!_suiLayers.TryGetValue(layer.Name, out var positions))
                _suiLayers.Add(layer.Name, positions = []);
            positions.Add(i);
        }
    }

    public bool IsAbove(Type rootType, string vanillaLayer)
    {
        Refresh();
        if (rootType == null || !_layersByName.TryGetValue(vanillaLayer, out int below)) return false;
        if (!_registrations.TryGetValue(rootType, out var registration))
        {
            var local = rootType.GetCustomAttribute<RegisterUIAttribute>(true);
            registration = (rootType.IsDefined(typeof(RegisterGlobalUIAttribute), true),
                local?.Name, local?.LayerNode);
            _registrations.Add(rootType, registration);
        }
        // Global SUI is drawn at the cursor, above the vanilla layers shielded here.
        if (registration.Global) return true;
        if (registration.Name == null || !_suiLayers.TryGetValue(registration.Name, out var positions)) return false;
        if (positions.Count == 1) return positions[0] > below;
        // Default SUI names can repeat across mods. SUI inserts each group directly
        // after its declared anchor, so disambiguate by that anchor rather than mod name.
        if (!_layersByName.TryGetValue(registration.Anchor, out int anchor)) return false;
        foreach (int above in positions)
            if (above > anchor) return above > below;
        return false;
    }

    public void Clear()
    {
        _layers = Array.Empty<GameInterfaceLayer>();
        _dirty = false;
        _layersByName.Clear();
        _suiLayers.Clear();
        _registrations.Clear();
    }
}
