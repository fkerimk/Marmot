using Marmot.Backend.Rendering;

namespace Marmot;

public static class ModelRenderSystem {

    public static void Draw() {

        foreach (var (id, model) in Scene.GetComponents<ModelComponent>())
            Rl.DrawModel(model, id.GetTransformOrDefault());
    }
}