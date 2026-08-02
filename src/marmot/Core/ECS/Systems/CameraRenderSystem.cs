using Marmot.Backend.Rendering;

namespace Marmot;

public static class CameraRenderSystem {

    public static void Start() {

        foreach (var (id, camera) in Scene.GetComponents<CameraComponent>()) {

            Rl.BeginCamera(CameraComponent.GetRlCamera(id));
        }
    }

    public static void End() {

        foreach (var (id, model) in Scene.GetComponents<CameraComponent>()) {

            Rl.EndCamera();
        }
    }
}