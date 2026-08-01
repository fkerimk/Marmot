using Marmot.Backend.Rendering;

namespace Marmot;

public static class CameraRenderSystem {

    public static void Start(World world) {

        foreach (var (id, camera) in world.CameraComponents) {

            Rl.BeginCamera(CameraComponent.GetRlCamera(world, id));
        }
    }

    public static void End(World world) {

        foreach (var (id, model) in world.ModelComponents) {

            Rl.EndCamera();
        }
    }
}