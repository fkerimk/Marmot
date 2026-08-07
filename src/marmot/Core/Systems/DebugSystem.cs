using Raylib_cs;

using Marmot.Backend.Rendering;

namespace Marmot;

public static class DebugSystem {

    private const float Alpha = 0.25f;
    private static readonly Color White = new(1, 1, 1, Alpha);

    public static void Debug3D() {

        DrawModels();
        DrawLights();
    }

    private static void DrawModels() {

        foreach (var (id, model) in Scene.GetComponents<Model>()) {

            var transform = id.GetTransformOrDefault();
            transform.Scale *= model.Scale;

            Rl.DrawBounds(model.Resource.Bounds.Transform(transform), White);
        }
    }

    private static void DrawLights() {

        foreach (var (id, light) in Scene.GetComponents<Light>()) {

            var transform = id.GetTransformOrDefault();

            Rl.DrawSphere(transform.RlPosition, 0.025f, light.Color.ToRlColor());
        }
    }
}