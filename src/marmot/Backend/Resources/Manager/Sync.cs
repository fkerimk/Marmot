using System.Text;
using System.Text.Json;
using static System.IO.Path;

using Marmot.Backend.Projects;
using Marmot.Backend.Resources.Importers;

namespace Marmot.Backend.Resources;

internal static partial class ResMan {

    internal static async Task Sync(Project project) {

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
}
