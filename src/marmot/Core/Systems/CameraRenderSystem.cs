using Marmot.Backend.Rendering;

namespace Marmot;

public static class CameraRenderSystem {

    public static void Start() {

        foreach (var (id, camera) in Scene.GetComponents<Camera>())
            Rl.BeginCamera(Camera.GetRlCamera(id));
    }

    public static void End() {

        foreach (var (id, model) in Scene.GetComponents<Camera>())
            Rl.EndCamera();
    }
}