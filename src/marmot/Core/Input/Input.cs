using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Marmot;

public static unsafe class Input {

    public static bool Cursor {

        get; set {

            field = value;

            if (value)
                 EnableCursor();
            else DisableCursor();
        }
    }

    public static Vector2 MousePos    => GetMousePosition();
    public static Vector2 MouseDelta  => GetMouseDelta();
    public static float   MouseScroll => GetMouseWheelMove();

    public static bool IsButtonDown     (Button button, int gamepad = 0) => ResolveButton(button, &IsMouseButtonDown    , &IsKeyDown    , &IsGamepadButtonDown    , gamepad);
    public static bool IsButtonPressed  (Button button, int gamepad = 0) => ResolveButton(button, &IsMouseButtonPressed , &IsKeyPressed , &IsGamepadButtonPressed , gamepad);
    public static bool IsButtonReleased (Button button, int gamepad = 0) => ResolveButton(button, &IsMouseButtonReleased, &IsKeyReleased, &IsGamepadButtonReleased, gamepad);
    public static bool IsButtonUp       (Button button, int gamepad = 0) => ResolveButton(button, &IsMouseButtonUp      , &IsKeyUp      , &IsGamepadButtonUp      , gamepad);

    private static bool ResolveButton(Button b, delegate*<MouseButton, CBool> mouse, delegate*<KeyboardKey, CBool> keyboard, delegate*<int, GamepadButton, CBool> gamepad, int gamepadId = 0) {

        var val = (uint)b;
        if (val == 0) return false;

        var code = (int)(val & 0xFFFFFF);

        return (val >> 24) switch {

            1 => mouse    ((MouseButton)code),
            2 => keyboard ((KeyboardKey)code),
            3 => gamepad  (gamepadId, (GamepadButton)code),

            _ => false
        };
    }

}