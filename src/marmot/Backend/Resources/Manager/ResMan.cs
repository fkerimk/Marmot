using Newtonsoft.Json;
using static System.IO.Path;

using Marmot.Backend.Resources.Types;

namespace Marmot.Backend.Resources;

internal static partial class ResMan {

    internal static readonly string ResPath = PathM.SearchPath(AppContext.BaseDirectory, "res", 6)
                                              ?? throw new DirectoryNotFoundException("Resources folder not found");

    internal static Dictionary<string, string> PathMap = [];
    internal static Dictionary<string, Resource> ResMap = [];

    internal static async Task LoadPathMap() {

        var path = Join(ResPath, "map.json");
        PathMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(await File.ReadAllTextAsync(path)) ?? throw Log.InvalidJsonException(path);
    }

    internal static string FindResourcePath(string relativePath, bool safe = false) {

        var filePath = Join(ResPath, PathMap.GetValueOrDefault(relativePath, relativePath));

        if (File.Exists(filePath)) return filePath;

        return safe ? null! : throw new FileNotFoundException($"Resource not found: {relativePath}");
    }

    internal static Resource GetResource<T>(string relativePath) where T : Resource, new() {

        if (ResMap.TryGetValue(relativePath, out var res)) return res;

        Log.Info($"Loading {relativePath}");

        var newRes = new T();
        var importPath = newRes.RawImportPath ? relativePath : FindResourcePath(relativePath);
        newRes.Import(importPath);

        ResMap[relativePath] = newRes;

        return newRes;
    }

    internal static void UnloadResources() {

        foreach (var res in ResMap) {

            Log.Info($"Unloading {res.Key}");
            res.Value.Unload();
        }
    }
}