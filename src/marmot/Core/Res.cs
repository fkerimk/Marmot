using Marmot.Backend.Resources;
using Marmot.Backend.Resources.Types;

namespace Marmot;

public static class Res {

    public static T Get<T>(string relativePath) where T : Resource, new() {

        return (T)ResourceManager.GetResource<T>(relativePath);
    }

    public static Model GetModel(string path) => Get<Model>(path);
    public static Shader GetShader(string path) => Get<Shader>(path);
}