using Marmot.Backend.Resources;
using Marmot.Backend.Resources.Types;

namespace Marmot;

public static class Res {

    public static ShaderRes MainShader;
    public static ShaderRes SkinnedMainShader;

    private static T Get<T>(string relativePath) where T : Resource, new()
        => (T)ResMan.GetResource<T>(relativePath);

    public static ModelRes GetModel(string path) => Get<ModelRes>(path);
    public static ShaderRes GetShader(string path) => Get<ShaderRes>(path);
}