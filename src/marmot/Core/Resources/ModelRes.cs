using Newtonsoft.Json;
using Raylib_cs;
using static Raylib_cs.Raylib;

using Marmot.Backend.Rendering;
using Marmot.Backend.Resources.Types;

namespace Marmot;

public unsafe class ModelRes : Resource {

    // Model
    internal Raylib_cs.Model RlModel;
    internal BoundingBox Bounds;
    private M3DMaterialInfo[] _materialInfo = [];

    // Animations
    internal ModelAnimation* RlAnims;
    internal int AnimCount;

    internal override void Import(string path) {

        RlModel = LoadModel(path);
        RlAnims = LoadModelAnimations(path, ref AnimCount);

        // Mikktspace tangent4 (xyz = tangent, w = bitangent sign)
        for (var m = 0; m < RlModel.MeshCount; m++)
            GenMeshTangents(ref RlModel.Meshes[m]);

        var materialJsonPath = path + ".json";
        var materialJson = File.ReadAllText(materialJsonPath);
        _materialInfo = JsonConvert.DeserializeObject<M3DMaterialInfo[]>(materialJson) ?? throw Log.InvalidJsonException(materialJsonPath);

        MaterialUtils.FixMaterials(ref RlModel, AnimCount > 0, _materialInfo);;

        Bounds = GetModelBoundingBox(RlModel);
    }

    public override void Unload() {

        UnloadModelAnimations(RlAnims, AnimCount);
        UnloadModel(RlModel);
    }
}
