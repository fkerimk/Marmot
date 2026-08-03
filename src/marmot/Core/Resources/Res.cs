using Marmot.Backend.Resources;
using Marmot.Backend.Resources.Types;

namespace Marmot;

public static class Res {

    private static T Get<T>(string relativePath) where T : Resource, new()
        => (T)ResMan.GetResource<T>(relativePath);

    public static Model GetModel(string path) => Get<Model>(path);
    public static Shader GetShader(string path) => Get<Shader>(path);
}