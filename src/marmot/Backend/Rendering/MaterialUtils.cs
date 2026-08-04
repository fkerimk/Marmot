using Raylib_cs;
using static Raylib_cs.Rlgl;

namespace Marmot;

internal static unsafe class MaterialUtils {

    private static Texture2D _defAlbedo;
    private static Texture2D _defNormal;
    private static Texture2D _defMetalness;
    private static Texture2D _defRoughness;
    private static Texture2D _defOcclusion;
    private static Texture2D _defEmissive;

    private static bool _isInitialized;

    public static void InitDefaultTextures() {

        if (_isInitialized) return;

        _defAlbedo    = CreateDefaultTexture(Color.White);
        _defNormal    = CreateDefaultTexture(new Color(128, 128, 255, 255));
        _defMetalness = CreateDefaultTexture(Color.Black);
        _defRoughness = CreateDefaultTexture(Color.White);
        _defOcclusion = CreateDefaultTexture(Color.White);
        _defEmissive  = CreateDefaultTexture(Color.Black);

        _isInitialized = true;
    }

    public static void EnsureModelMaterialDefaults(ref Raylib_cs.Model model) {

        if (!_isInitialized) InitDefaultTextures();

        for (var i = 0; i < model.MaterialCount; i++) {

            var mat = model.Materials[i];

            CheckAndAssign(ref mat, MaterialMapIndex.Albedo, _defAlbedo);
            CheckAndAssign(ref mat, MaterialMapIndex.Normal, _defNormal);
            CheckAndAssign(ref mat, MaterialMapIndex.Metalness, _defMetalness);
            CheckAndAssign(ref mat, MaterialMapIndex.Roughness, _defRoughness);
            CheckAndAssign(ref mat, MaterialMapIndex.Occlusion, _defOcclusion);
            CheckAndAssign(ref mat, MaterialMapIndex.Emission, _defEmissive);

            model.Materials[i] = mat;
        }
    }

    public static void DetachModelMaterialDefaults(ref Raylib_cs.Model model) {

        if (!_isInitialized) return;

        for (var i = 0; i < model.MaterialCount; i++) {

            var mat = model.Materials[i];

            DetachIfDefault(ref mat, MaterialMapIndex.Albedo);
            DetachIfDefault(ref mat, MaterialMapIndex.Normal);
            DetachIfDefault(ref mat, MaterialMapIndex.Metalness);
            DetachIfDefault(ref mat, MaterialMapIndex.Roughness);
            DetachIfDefault(ref mat, MaterialMapIndex.Occlusion);
            DetachIfDefault(ref mat, MaterialMapIndex.Emission);
            mat.Shader = new Shader {
                Id = GetShaderIdDefault(),
                Locs = GetShaderLocsDefault()
            };

            model.Materials[i] = mat;
        }
    }

    public static void UnloadDefaultTextures() {

        if (!_isInitialized) return;

        Raylib.UnloadTexture(_defAlbedo);
        Raylib.UnloadTexture(_defNormal);
        Raylib.UnloadTexture(_defMetalness);
        Raylib.UnloadTexture(_defRoughness);
        Raylib.UnloadTexture(_defOcclusion);
        Raylib.UnloadTexture(_defEmissive);

        _isInitialized = false;
    }

    private static Texture2D CreateDefaultTexture(Color color) {

        var img = Raylib.GenImageColor(1, 1, color);
        var tex = Raylib.LoadTextureFromImage(img);

        Raylib.UnloadImage(img);

        return tex;
    }

    private static void CheckAndAssign(ref Material mat, MaterialMapIndex index, Texture2D defaultTex) {

        var textureId = mat.Maps[(int)index].Texture.Id;
        if (textureId > 0 && textureId != GetTextureIdDefault()) return;

        mat.Maps[(int)index].Texture = defaultTex;
        Log.Info(index + " texture assigned to " + mat.Maps[(int)index].Texture.Id);
    }

    private static void DetachIfDefault(ref Material mat, MaterialMapIndex index) {

        if (!IsMarmotDefaultTexture(mat.Maps[(int)index].Texture)) return;

        mat.Maps[(int)index].Texture = new Texture2D {
            Id = GetTextureIdDefault(),
            Width = 1,
            Height = 1,
            Mipmaps = 1,
            Format = PixelFormat.UncompressedR8G8B8A8
        };
    }

    private static bool IsMarmotDefaultTexture(Texture2D texture) =>
        texture.Id == _defAlbedo.Id ||
        texture.Id == _defNormal.Id ||
        texture.Id == _defMetalness.Id ||
        texture.Id == _defRoughness.Id ||
        texture.Id == _defOcclusion.Id ||
        texture.Id == _defEmissive.Id;
}
