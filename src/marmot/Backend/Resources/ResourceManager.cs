using System.Text;
using System.Text.Json;
using static System.IO.Path;

using Marmot.Backend.Projects;
using Marmot.Backend.Resources.Importers;
using Marmot.Backend.Resources.Types;

namespace Marmot.Backend.Resources;

public static class ResourceManager {

    internal static readonly string ResPath = PathM.SearchPath(AppContext.BaseDirectory, "res", 6)
                                              ?? throw new DirectoryNotFoundException("Resources folder not found");

    internal static Dictionary<string, string> PathMap = [];
    internal static Dictionary<string, Resource> ResMap = [];

    public static async Task Sync(Project project) {

        var importers = new Importer[] {

            new BlenderImporter(),
            new DirectImporter(),
        };

        // Ensure directories
        Directory.CreateDirectory(project.ResPath);
        Directory.CreateDirectory(project.DotPath);
        Directory.CreateDirectory(project.ResGenPath);

        var loadedResHash = File.Exists(project.ResHashPath)
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(await File.ReadAllTextAsync(project.ResHashPath), JsonContext.Default.DictionaryStringString) ?? new()
            : new Dictionary<string, string>();

        var resMap = new Dictionary<string, string>();
        var resHash = new Dictionary<string, string>();

        await ImportResources(project.ResPath);
        await ImportResources(Join(AppContext.BaseDirectory, "res"));

        // Clean target
        var targetFiles = Directory.GetFiles(project.ResGenPath, "*", SearchOption.AllDirectories);

        foreach (var filePath in targetFiles) {

            if (filePath == project.ResMapPath) continue;
            if (resHash.ContainsValue(GetFileNameWithoutExtension(filePath))) continue;

            File.Delete(filePath);
            Console.WriteLine($"Removed import of {GetRelativePath(project.ResGenPath, filePath)}");
        }

        // Save maps
        var resMapJson = JsonSerializer.Serialize(resMap, JsonContext.Default.DictionaryStringString);
        var resHashJson = JsonSerializer.Serialize(resHash, JsonContext.Default.DictionaryStringString);

        await File.WriteAllTextAsync(project.ResMapPath, resMapJson, Encoding.UTF8);
        await File.WriteAllTextAsync(project.ResHashPath, resHashJson, Encoding.UTF8);

        return;

        async Task ImportResources(string path) {

            var sourceFiles = Directory.GetFiles(path, "*", SearchOption.AllDirectories);

            foreach (var sourcePath in sourceFiles) {

                var relativePath = GetRelativePath(path, sourcePath);
                var ext = GetExtension(sourcePath);
                var hash = await FileM.GetHash(sourcePath);

                foreach (var importer in importers) {

                    if (!importer.SupportedExtensions().Contains(ext)) continue;

                    var targetFile = hash + importer.GetTargetExtension(ext);
                    var targetPath = Combine(project.ResGenPath, targetFile);

                    resMap[relativePath] = targetFile;
                    resHash[relativePath] = hash;

                    if (loadedResHash.TryGetValue(relativePath, out var storedHash) && storedHash == hash) continue;
                    await importer.ImportOperation(sourcePath, targetPath);

                    Console.WriteLine($"Imported {relativePath}");
                }
            }
        }
    }

    internal static async Task LoadPathMap() {

        var path = Join(ResPath, "map.json");

        PathMap = File.Exists(path)
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(await File.ReadAllTextAsync(path), JsonContext.Default.DictionaryStringString) ?? new()
            : new Dictionary<string, string>();
    }

    internal static string FindResourcePath(string relativePath, bool safe = false) {

        var filePath = Join(ResPath, PathMap.GetValueOrDefault(relativePath, relativePath));

        if (File.Exists(filePath)) return filePath;

        return safe ? null! : throw new FileNotFoundException($"Resource not found: {relativePath}");
    }

    internal static Resource GetResource<T>(string relativePath) where T : Resource, new() {

        if (ResMap.TryGetValue(relativePath, out var res)) return res;

        var newRes = new T();
        var importPath = newRes.RawImportPath ? relativePath : FindResourcePath(relativePath);
        newRes.Import(importPath);

        ResMap[relativePath] = newRes;

        return newRes;
    }

    internal static void UnloadResources() {

        foreach (var resource in ResMap.Values)
            resource.Unload();
    }
}