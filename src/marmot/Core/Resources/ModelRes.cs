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
    public M3DMaterialInfo[] MaterialInfo = [];

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
        MaterialInfo = JsonConvert.DeserializeObject<M3DMaterialInfo[]>(materialJson);;

        for (var i = 0; i < RlModel.MaterialCount; i++) {

            RlModel.Materials[i].Shader = _animCount > 0 ? Pbr.SkinnedMainShader : Pbr.MainShader;
            SetTextureFilter(RlModel.Materials[i].Maps[(int)MaterialMapIndex.Albedo].Texture   , TextureFilter.Bilinear);
            SetTextureFilter(RlModel.Materials[i].Maps[(int)MaterialMapIndex.Metalness].Texture, TextureFilter.Bilinear);
            SetTextureFilter(RlModel.Materials[i].Maps[(int)MaterialMapIndex.Normal].Texture   , TextureFilter.Bilinear);
            SetTextureFilter(RlModel.Materials[i].Maps[(int)MaterialMapIndex.Roughness].Texture, TextureFilter.Bilinear);
            SetTextureFilter(RlModel.Materials[i].Maps[(int)MaterialMapIndex.Occlusion].Texture, TextureFilter.Bilinear);
            SetTextureFilter(RlModel.Materials[i].Maps[(int)MaterialMapIndex.Emission].Texture , TextureFilter.Bilinear);

            if (i >= MaterialInfo.Length) continue;
            MaterialUtils.AssignDefault(ref RlModel.Materials[i], MaterialMapIndex.Albedo   , MaterialInfo[i].HasAlbedo   );
            MaterialUtils.AssignDefault(ref RlModel.Materials[i], MaterialMapIndex.Metalness, MaterialInfo[i].HasMetallic );
            MaterialUtils.AssignDefault(ref RlModel.Materials[i], MaterialMapIndex.Normal   , MaterialInfo[i].HasNormal   );
            MaterialUtils.AssignDefault(ref RlModel.Materials[i], MaterialMapIndex.Roughness, MaterialInfo[i].HasRoughness);
            MaterialUtils.AssignDefault(ref RlModel.Materials[i], MaterialMapIndex.Occlusion, MaterialInfo[i].HasAo       );
            MaterialUtils.AssignDefault(ref RlModel.Materials[i], MaterialMapIndex.Emission , MaterialInfo[i].HasEmission );
        }

        Bounds = GetModelBoundingBox(RlModel);
    }

    public override void Unload() {

        UnloadModelAnimations(RlAnims, _animCount);
        UnloadModel(RlModel);
    }
}
