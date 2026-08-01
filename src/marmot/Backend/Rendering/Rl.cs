using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static Raylib_cs.Color;
using static Raylib_cs.ConfigFlags;
using static Raylib_cs.TraceLogLevel;

namespace Marmot.Backend.Rendering;

internal static class Rl {

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

    internal static void BeginCamera(Camera3D camera) {

        BeginMode3D(camera);
    }

    internal static void EndCamera() {

        EndMode3D();
    }

    internal static void DrawModel(Raylib_cs.Model model, Vector3 pos, Vector3 rot, Vector3 scale) {

        var scaleMatrix = Raymath.MatrixScale(scale.X, scale.Y, scale.Z);
        var rotationMatrix = Raymath.MatrixRotateXYZ(rot * DEG2RAD);
        var translationMatrix = Raymath.MatrixTranslate(pos.X, pos.Y, pos.Z);

        var transform = Raymath.MatrixMultiply(Raymath.MatrixMultiply(scaleMatrix, rotationMatrix), translationMatrix);

        model.Transform = transform;

        if (Compatibility.NativeRl) {

            var boneMatrices = model.BoneMatricesAsSpan();

            for (var i = 0; i < model.Skeleton.BoneCount; i++) {

                Matrix4x4.Decompose(boneMatrices[i], out var matrixScale, out _, out var translation);
                boneMatrices[i] = Matrix4x4.CreateScale(matrixScale) * Matrix4x4.CreateTranslation(translation);
            }
        }

        Raylib.DrawModel(model, Vector3.Zero, 1.0f, White);
    }

    internal static void SetAnimationFrame(Raylib_cs.Model model, ModelAnimation anim, int frame) {

        UpdateModelAnimation(model, anim, frame);
    }

    internal static void Exit() {

        CloseWindow();
    }
}