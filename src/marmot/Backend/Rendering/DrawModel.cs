using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Color;

namespace Marmot.Backend.Rendering;

internal static partial class Rl {

    internal static void DrawModel(Raylib_cs.Model model) {

        if (Compatibility.IsCpuSkinned) {

            var boneMatrices = model.BoneMatricesAsSpan();

            for (var i = 0; i < model.Skeleton.BoneCount; i++) {

                Matrix4x4.Decompose(boneMatrices[i], out var matrixScale, out _, out var translation);
                boneMatrices[i] = Matrix4x4.CreateScale(matrixScale) * Matrix4x4.CreateTranslation(translation);
            }
        }

        Raylib.DrawModel(model, Vector3.Zero, 1.0f, White);
    }
}