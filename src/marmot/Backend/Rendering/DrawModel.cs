using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Color;

namespace Marmot.Backend.Rendering;

internal static partial class Rl {

    internal static void DrawModel(ModelComponent model, TransformComponent transform) {

        var scaledTransform = transform with { Scale = transform.Scale * model.Scale };
        var rlModel = model.RlModel with { Transform = scaledTransform.RlMatrix };

        if (Compatibility.IsCpuSkinned) {

            var boneMatrices = rlModel.BoneMatricesAsSpan();

            for (var i = 0; i < rlModel.Skeleton.BoneCount; i++) {

                Matrix4x4.Decompose(boneMatrices[i], out var matrixScale, out _, out var translation);
                boneMatrices[i] = Matrix4x4.CreateScale(matrixScale) * Matrix4x4.CreateTranslation(translation);
            }
        }

        Raylib.DrawModel(rlModel, Vector3.Zero, 1.0f, White);
    }
}