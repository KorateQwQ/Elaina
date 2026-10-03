using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SilkyUIFramework;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.UI;

namespace 伊蕾娜.System.UI;

/// <summary>Arbitrate mouse ownership between all registered SUI panels and the vanilla UI.</summary>
[Autoload(Side = ModSide.Client)]
public sealed class ElainaUIInputSystem : ModSystem
{
    // These vanilla layers process input while drawing. Only shield layers below the owner.
    // Keep modal settings/fancy UI and cursor/tooltip rendering outside this scope.
    private static readonly HashSet<string> InteractiveLayers =
    [
        "Vanilla: Map / Minimap", "Vanilla: Hair Window", "Vanilla: Dresser Window",
        "Vanilla: NPC / Sign Dialog", "Vanilla: Resource Bars", "Vanilla: Inventory",
        "Vanilla: Info Accessories Bar", "Vanilla: Settings Button", "Vanilla: Hotbar",
        "Vanilla: Builder Accessories Bar", "Vanilla: Radial Hotbars"
    ];

    private readonly UIPointerCapture _capture = new();
    private readonly SUIInputLayerOrder _layerOrder = new();
    private readonly HashSet<Type> _gestureOwners = [];
    private readonly HashSet<Type> _drawOwners = [];
    private bool _blockDrawInput;

    private static bool CanOwnMouse => !Main.dedServ && !Main.gameMenu && !Main.hideUI
        && Main.hasFocus && !Main.ingameOptionsWindow && !Main.inFancyUI
        && SilkyUISystem.ServiceProvider != null;

    private static Type HoveredSUIRoot()
    {
        if (!CanOwnMouse) return null;
        var root = SilkyUIInputState.Instance.HoverTarget?.SilkyUI?.RootNode;
        // SUI already resolves z-order, disabled controls and mouse-ignoring decoration.
        // Root type is used only for registration metadata, never as a mod/type allowlist.
        return root is { Enabled: true, IsInteractable: true } ? root.GetType() : null;
    }

    public override void Load()
    {
        On_UserInterface.Update += UpdateVanillaInterface;
        On_LegacyGameInterfaceLayer.DrawSelf += DrawVanillaLayer;
    }

    public override void Unload()
    {
        On_UserInterface.Update -= UpdateVanillaInterface;
        On_LegacyGameInterfaceLayer.DrawSelf -= DrawVanillaLayer;
        ResetCapture();
    }

    public override void OnWorldUnload() => ResetCapture();

    private void ResetCapture()
    {
        _capture.Reset();
        _gestureOwners.Clear();
        _drawOwners.Clear();
        _layerOrder.Clear();
        _blockDrawInput = false;
    }

    public override void UpdateUI(GameTime gameTime)
    {
        // SUI dispatches input before Main.UpdateUIStates, which calls this hook.
        // Do not change Main.mouseLeft globally: SUI still needs the real release edge.
        if (!CanOwnMouse) { ResetCapture(); return; }
        Type hovered = HoveredSUIRoot();
        _drawOwners.Clear();
        _drawOwners.UnionWith(_gestureOwners);
        if (hovered != null) _drawOwners.Add(hovered);
        _blockDrawInput = _capture.Update(true, hovered != null,
            Main.mouseLeft, Main.mouseRight, Main.mouseMiddle);

        // Keep all participating owners until every held button is released. The draw
        // snapshot still includes them on that release frame, even if the panel closed.
        if (!_capture.IsCaptured) _gestureOwners.Clear();
        else if (hovered != null) _gestureOwners.Add(hovered);
        if (!_blockDrawInput) return;
        Main.LocalPlayer.mouseInterface = true;
        PlayerInput.LockVanillaMouseScroll("SUI: Vanilla input shield");
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) => _layerOrder.Update(layers);

    private bool OwnsVanillaLayer(IEnumerable<Type> owners, string layerName)
    {
        foreach (Type owner in owners)
            if (_layerOrder.IsAbove(owner, layerName)) return true;
        return false;
    }

    private void UpdateVanillaInterface(On_UserInterface.orig_Update orig, UserInterface self, GameTime time)
    {
        // Journey powers and other vanilla UIState controls process input during Update.
        // Their update precedes ModSystem.UpdateUI; the prior capture still owns release.
        // Vanilla Journey controls live in the inventory layer; lower SUI panels
        // must not take their input just because SUI itself reported a hover.
        if (!CanOwnMouse || !(_layerOrder.IsAbove(HoveredSUIRoot(), "Vanilla: Inventory")
                || OwnsVanillaLayer(_gestureOwners, "Vanilla: Inventory"))
            || self.CurrentState?.GetType().Assembly != typeof(Main).Assembly)
        {
            orig(self, time);
            return;
        }

        using var input = new VanillaMouseInputScope();
        orig(self, time);
    }

    private bool DrawVanillaLayer(On_LegacyGameInterfaceLayer.orig_DrawSelf orig, LegacyGameInterfaceLayer self)
    {
        if (!_blockDrawInput || !CanOwnMouse || !InteractiveLayers.Contains(self.Name)
            || !OwnsVanillaLayer(_drawOwners, self.Name))
            return orig(self);

        // Draw() has already applied the layer's UI scale before entering this hook.
        using var input = new VanillaMouseInputScope();
        return orig(self);
    }
}
