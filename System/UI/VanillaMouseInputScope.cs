using System;
using Terraria;
using Terraria.GameInput;

namespace 伊蕾娜.System.UI;

/// <summary>Hide mouse input only during one vanilla UI call; always restore it.</summary>
internal readonly struct VanillaMouseInputScope : IDisposable
{
    private readonly (int X, int Y, int LastX, int LastY) _mainPosition;
    private readonly (int X, int Y) _inputPosition, _originalPosition;
    private readonly (int Width, int Height) _screenSize;
    private readonly (bool Left, bool Right, bool Middle, bool X1, bool X2,
        bool LeftRelease, bool RightRelease, bool Block) _buttons;
    private readonly (int UI, int Game) _scroll;

    public VanillaMouseInputScope()
    {
        _mainPosition = (Main.mouseX, Main.mouseY, Main.lastMouseX, Main.lastMouseY);
        _screenSize = (Main.screenWidth, Main.screenHeight);
        // Read the raw cache through the public zoom API, without private-field hooks.
        PlayerInput.SetZoom_Unscaled();
        _originalPosition = (Main.mouseX, Main.mouseY);
        (Main.screenWidth, Main.screenHeight) = _screenSize;
        _inputPosition = (PlayerInput.MouseX, PlayerInput.MouseY);
        _buttons = (Main.mouseLeft, Main.mouseRight, Main.mouseMiddle,
            Main.mouseXButton1, Main.mouseXButton2, Main.mouseLeftRelease,
            Main.mouseRightRelease, Main.blockMouse);
        _scroll = (PlayerInput.ScrollWheelDeltaForUI, PlayerInput.ScrollWheelDelta);

        // mouseInterface only prevents world item use. Inventory slots also run
        // hover/auto-deposit logic without a fresh click, so hide the position too.
        Main.mouseX = Main.mouseY = Main.lastMouseX = Main.lastMouseY = -1000000;
        PlayerInput.MouseX = PlayerInput.MouseY = -1000000;
        // A vanilla layer can call SetZoom_UI again; keep its source masked as well.
        PlayerInput.CacheMousePositionForZoom();
        Main.mouseLeft = Main.mouseRight = Main.mouseMiddle = false;
        Main.mouseXButton1 = Main.mouseXButton2 = false;
        Main.mouseLeftRelease = Main.mouseRightRelease = false;
        Main.blockMouse = true;
        PlayerInput.ScrollWheelDeltaForUI = PlayerInput.ScrollWheelDelta = 0;
    }

    public void Dispose()
    {
        (Main.mouseX, Main.mouseY) = _originalPosition;
        PlayerInput.CacheMousePositionForZoom();
        (Main.mouseX, Main.mouseY, Main.lastMouseX, Main.lastMouseY) = _mainPosition;
        (Main.screenWidth, Main.screenHeight) = _screenSize;
        (PlayerInput.MouseX, PlayerInput.MouseY) = _inputPosition;
        (Main.mouseLeft, Main.mouseRight, Main.mouseMiddle, Main.mouseXButton1,
            Main.mouseXButton2, Main.mouseLeftRelease, Main.mouseRightRelease, Main.blockMouse) = _buttons;
        (PlayerInput.ScrollWheelDeltaForUI, PlayerInput.ScrollWheelDelta) = _scroll;
    }
}
