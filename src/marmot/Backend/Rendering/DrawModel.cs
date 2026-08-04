using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Rlgl;

namespace Marmot.Backend.Rendering;

internal static unsafe partial class Rl {

    internal static void DrawModel(Raylib_cs.Model model) {

        if (Compatibility.IsCpuSkinned) {

            var boneMatrices = model.BoneMatricesAsSpan();

            for (var i = 0; i < model.Skeleton.BoneCount; i++) {

                Matrix4x4.Decompose(boneMatrices[i], out var matrixScale, out _, out var translation);
                boneMatrices[i] = Matrix4x4.CreateScale(matrixScale) * Matrix4x4.CreateTranslation(translation);
            }
        }

        var meshes = model.MeshesAsSpan();

        for (var i = 0; i < model.MeshCount; i++) {

            var materialIndex = model.MeshMaterial[i];
            var material = materialIndex >= 0 && materialIndex < model.MaterialCount
                ? model.Materials[materialIndex]
                : model.Materials[0];

            var boneTransformLoc = material.Shader.Locs[(int)ShaderLocationIndex.MatrixBoneTransforms];
            if (boneTransformLoc != -1 && model.BoneMatrices != null) {

                EnableShader(material.Shader.Id);
                SetUniformMatrices(boneTransformLoc, model.BoneMatrices, model.Skeleton.BoneCount);
            }

            Raylib.DrawMesh(meshes[i], material, model.Transform);
        }
    }
}
