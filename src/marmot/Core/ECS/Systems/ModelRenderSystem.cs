using System.Numerics;
using Marmot.Backend.Rendering;

namespace Marmot;

public static class ModelRenderSystem {

    public static void Draw() {

        foreach (var (id, model) in Scene.GetComponents<ModelComponent>()) {

            var pos = id.GetPositionOrDefault();
            var rot = id.GetRotationOrDefault();

            Rl.DrawModel(model.Value.GetRlModel(), pos.Value, rot.Value, Vector3.One * model.Scale);
        }
    }
}