using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static Raylib_cs.Color;
using static Raylib_cs.ConfigFlags;
using static Raylib_cs.TraceLogLevel;

namespace Marmot.Backend.Rendering;

internal static partial class Rl {

    internal static bool IsAlive => !WindowShouldClose();

    internal static void Init() {

        SetTraceLogLevel(None);
        SetConfigFlags(ResizableWindow | VSyncHint);
        InitWindow(1280, 720, "Marmot");
        SetWindowMonitor(0);
        SetExitKey(0);
    }

    internal static void BeginDrawing() {

        Raylib.BeginDrawing();
        ClearBackground(Black);
    }

    internal static void EndDrawing() {

        DrawFPS(10, 10);
        Raylib.EndDrawing();
    }

    internal static void BeginCamera(Camera3D camera) => BeginMode3D(camera);

    internal static void EndCamera() => EndMode3D();


    internal static void DrawBox(Vector3 pos, Vector3 size, Color color) {

        Raylib.DrawCube(pos, size.X, size.Y, size.Z, color);
    }

    internal static void SetAnimationFrame(Raylib_cs.Model model, ModelAnimation anim, int frame)
        => UpdateModelAnimation(model, anim, frame);

    internal static void Exit() => CloseWindow();
}