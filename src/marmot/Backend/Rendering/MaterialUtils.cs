using Raylib_cs;
using static Raylib_cs.Rlgl;

namespace Marmot;

internal static class MaterialUtils {

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

    internal static void AssignDefault(ref Material material, MaterialMapIndex materialMap, bool skip) {

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

        Raylib.SetMaterialTexture(ref material, materialMap, texture);
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
}
