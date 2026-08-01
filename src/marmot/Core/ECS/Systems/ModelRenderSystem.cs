using System.Numerics;
using Marmot.Backend.Rendering;

namespace Marmot;

public static class ModelRenderSystem {

    public static void Draw(World world) {

        foreach (var (id, model) in world.ModelComponents) {

            var pos = world.GetPositionOrDefault(id);
            var rot = world.GetRotationOrDefault(id);

            Rl.DrawModel(model.Value.GetRlModel(), pos.Value, rot.Value, Vector3.Create(5));
        }
    }
}