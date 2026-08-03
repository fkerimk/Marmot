using System.Numerics;
using Marmot.Backend.Rendering;
using Raylib_cs;

namespace Marmot;

public static class DebugSystem {

    public static void Debug3D() {

        foreach (var (id, model) in Scene.GetComponents<ModelComponent>()) {

            var transform = id.GetTransformOrDefault();
            Rl.DrawBox(transform.Position, Vector3.One, Color.White);
        }
    }
}