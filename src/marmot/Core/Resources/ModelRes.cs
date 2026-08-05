using Newtonsoft.Json;
using Raylib_cs;
using static Raylib_cs.Raylib;

using Marmot.Backend.Rendering;
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

        var materialJsonPath = path + ".json";
        var materialJson = File.ReadAllText(materialJsonPath);
        var materialInfo = JsonConvert.DeserializeObject<List<M3DMaterialInfo>>(materialJson);;

        //Log.Info($"{path} - {materialInfo.Count} Materials");
        //for (var i = 0; i < materialInfo.Count; i++) {
        //    Log.Info($"  {i}. {materialInfo[i].Name}");
        //    Log.Info($"\t  HasAlbedo    : {materialInfo[i].HasAlbedo}");
        //    Log.Info($"\t  HasNormal    : {materialInfo[i].HasNormal}");
        //    Log.Info($"\t  HasRoughness : {materialInfo[i].HasRoughness}");
        //    Log.Info($"\t  HasMetallic  : {materialInfo[i].HasMetallic}");
        //    Log.Info($"\t  HasEmission  : {materialInfo[i].HasEmission}");
        //    Log.Info($"\t  HasAo        : {materialInfo[i].HasAo}");
        //}

        MaterialUtils.EnsureModelMaterialDefaults(ref RlModel);

        for (var i = 0; i < RlModel.MaterialCount; i++) {

            SetTextureFilter(RlModel.Materials[i].Maps[(int)MaterialMapIndex.Albedo].Texture, TextureFilter.Bilinear);
            SetTextureFilter(RlModel.Materials[i].Maps[(int)MaterialMapIndex.Metalness].Texture, TextureFilter.Bilinear);
            SetTextureFilter(RlModel.Materials[i].Maps[(int)MaterialMapIndex.Normal].Texture, TextureFilter.Bilinear);
            SetTextureFilter(RlModel.Materials[i].Maps[(int)MaterialMapIndex.Roughness].Texture, TextureFilter.Bilinear);
            SetTextureFilter(RlModel.Materials[i].Maps[(int)MaterialMapIndex.Occlusion].Texture, TextureFilter.Bilinear);
            SetTextureFilter(RlModel.Materials[i].Maps[(int)MaterialMapIndex.Emission].Texture, TextureFilter.Bilinear);

            RlModel.Materials[i].Shader = _animCount > 0 ? Pbr.SkinnedMainShader : Pbr.MainShader;
        }

        Bounds = GetModelBoundingBox(RlModel);
    }

    public override void Unload() {

        UnloadModelAnimations(RlAnims, _animCount);
        MaterialUtils.DetachModelMaterialDefaults(ref RlModel);
        UnloadModel(RlModel);
    }
}
