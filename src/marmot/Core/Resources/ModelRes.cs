using Raylib_cs;
using static Raylib_cs.Raylib;

using Marmot.Backend.Resources.Types;

namespace Marmot;

public unsafe class ModelRes : Resource {

    // Model
    public Raylib_cs.Model RlModel;
    public BoundingBox Bounds;

    // Animations
    public ModelAnimation* RlAnims;
    private int _animCount;

    internal override void Import(string path) {

        RlModel = LoadModel(path);
        RlAnims = LoadModelAnimations(path, ref _animCount);

        // Mikktspace tangent4 (xyz = tangent, w = bitangent sign)
        for (var m = 0; m < RlModel.MeshCount; m++)
            GenMeshTangents(ref RlModel.Meshes[m]);

        for (var i = 0; i < RlModel.MaterialCount; i++) {

            SetTextureFilter(RlModel.Materials[i].Maps[(int)MaterialMapIndex.Albedo].Texture, TextureFilter.Bilinear);
            RlModel.Materials[i].Shader = _animCount > 0 ? Res.SkinnedMainShader.RlShader!.Value : Res.MainShader.RlShader!.Value;
        }

        MaterialUtils.EnsureModelMaterialDefaults(ref RlModel);

        Bounds = GetModelBoundingBox(RlModel);
    }

    public override void Unload() {

        UnloadModelAnimations(RlAnims, _animCount);
        UnloadModel(RlModel);
    }
}