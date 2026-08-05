using Raylib_cs;
using static Raylib_cs.Raylib;

using Marmot.Backend.Rendering;

namespace Marmot;

internal static unsafe class MaterialUtils {

    private static Texture2D _defAlbedo;
    private static Texture2D _defNormal;
    private static Texture2D _defMetalness;
    private static Texture2D _defRoughness;
    private static Texture2D _defOcclusion;
    private static Texture2D _defEmissive;

    private static bool _isInitialized;

    internal static void InitDefaultTextures() {

        if (_isInitialized) return;

        _defAlbedo    = CreateDefaultTexture(Color.White);
        _defNormal    = CreateDefaultTexture(new Color(128, 128, 255, 255));
        _defMetalness = CreateDefaultTexture(Color.Black);
        _defRoughness = CreateDefaultTexture(Color.White);
        _defOcclusion = CreateDefaultTexture(Color.White);
        _defEmissive  = CreateDefaultTexture(Color.Black);

        _isInitialized = true;
    }

    internal static void FixMaterials(ref Raylib_cs.Model model, bool skinned, M3DMaterialInfo[] materialInfo) {

        if (materialInfo.Length == 0) throw Log.MaterialException();

        if (model.MaterialCount == 0) {

            model.MaterialCount = 1;
            model.Materials[0] = LoadMaterialDefault();

            for (var i = 0; i < model.MeshCount; i++)
                SetModelMeshMaterial(ref model, i, 0);
        }

        var cmi = 0; // cachedMaterialInfo

        for (var i = 0; i < model.MaterialCount; i++) {

            model.Materials[i].Shader = skinned ? Pbr.SkinnedMainShader : Pbr.MainShader;

            SetTextureFilter(model.Materials[i].Maps[(int)MaterialMapIndex.Albedo].Texture   , TextureFilter.Bilinear);
            SetTextureFilter(model.Materials[i].Maps[(int)MaterialMapIndex.Metalness].Texture, TextureFilter.Bilinear);
            SetTextureFilter(model.Materials[i].Maps[(int)MaterialMapIndex.Normal].Texture   , TextureFilter.Bilinear);
            SetTextureFilter(model.Materials[i].Maps[(int)MaterialMapIndex.Roughness].Texture, TextureFilter.Bilinear);
            SetTextureFilter(model.Materials[i].Maps[(int)MaterialMapIndex.Occlusion].Texture, TextureFilter.Bilinear);
            SetTextureFilter(model.Materials[i].Maps[(int)MaterialMapIndex.Emission].Texture , TextureFilter.Bilinear);

            if (cmi == 0 && i > materialInfo.Length - 1) cmi = i - 1;
            AssignDefault(ref model.Materials[i], MaterialMapIndex.Albedo   , materialInfo[cmi].HasAlbedo   );
            AssignDefault(ref model.Materials[i], MaterialMapIndex.Metalness, materialInfo[cmi].HasMetallic );
            AssignDefault(ref model.Materials[i], MaterialMapIndex.Normal   , materialInfo[cmi].HasNormal   );
            AssignDefault(ref model.Materials[i], MaterialMapIndex.Roughness, materialInfo[cmi].HasRoughness);
            AssignDefault(ref model.Materials[i], MaterialMapIndex.Occlusion, materialInfo[cmi].HasAo       );
            AssignDefault(ref model.Materials[i], MaterialMapIndex.Emission , materialInfo[cmi].HasEmission );
        }
    }

    private static void AssignDefault(ref Material material, MaterialMapIndex materialMap, bool skip) {

        if (skip) return;

        var texture = materialMap switch {

            MaterialMapIndex.Albedo     => _defAlbedo,
            MaterialMapIndex.Metalness  => _defMetalness,
            MaterialMapIndex.Normal     => _defNormal,
            MaterialMapIndex.Roughness  => _defRoughness,
            MaterialMapIndex.Occlusion  => _defOcclusion,
            MaterialMapIndex.Emission   => _defEmissive,

            _ => throw new ArgumentOutOfRangeException(nameof(materialMap), materialMap, null)
        };

        SetMaterialTexture(ref material, materialMap, texture);
    }

    internal static void UnloadDefaultTextures() {

        if (!_isInitialized) return;

        UnloadTexture(_defAlbedo);
        UnloadTexture(_defNormal);
        UnloadTexture(_defMetalness);
        UnloadTexture(_defRoughness);
        UnloadTexture(_defOcclusion);
        UnloadTexture(_defEmissive);

        _isInitialized = false;
    }

    private static Texture2D CreateDefaultTexture(Color color) {

        var img = GenImageColor(1, 1, color);
        var tex = LoadTextureFromImage(img);

        UnloadImage(img);

        return tex;
    }
}
