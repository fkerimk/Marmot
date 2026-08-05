using Marmot.Backend.Rendering;

namespace Marmot;

public static class ModelRenderSystem {

    public static void Draw() {

        foreach (var (id, model) in Scene.GetComponents<Model>()) {

            var transform = id.GetTransformOrDefault();

            var scaledTransform = transform with { Scale = transform.Scale * model.Scale };
            var rlModel = model.RlModel with { Transform = scaledTransform.RlMatrix };

            Pbr.SetNormalMatrix(scaledTransform.RlMatrix);
            Pbr.ApplyUniforms(model);

            Rl.DrawModel(rlModel);
        }
    }
}
