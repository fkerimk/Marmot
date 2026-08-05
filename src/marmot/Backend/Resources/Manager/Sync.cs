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

        var sidekicks = new List<string>();

        await ImportResources(project.ResPath);
        await ImportResources(Join(AppContext.BaseDirectory, "res"));

        // Clean target
        var targetFiles = Directory.GetFiles(project.ResGenPath, "*", SearchOption.AllDirectories);

        foreach (var filePath in targetFiles) {

            if (filePath == project.ResMapPath) continue;
            if (resHash.ContainsValue(GetFileNameWithoutExtension(filePath))) continue;

            var relativePath = GetRelativePath(project.ResGenPath, filePath);
            if (sidekicks.Contains(relativePath)) continue;

            File.Delete(filePath);

            Log.Info($"Removed import of {relativePath}");
        }

        // Save maps
        var resMapJson = JsonSerializer.Serialize(resMap, JsonContext.Default.DictionaryStringString);
        var resHashJson = JsonSerializer.Serialize(resHash, JsonContext.Default.DictionaryStringString);

        await File.WriteAllTextAsync(project.ResMapPath, resMapJson, Encoding.UTF8);
        await File.WriteAllTextAsync(project.ResHashPath, resHashJson, Encoding.UTF8);

        return;

        async Task ImportResources(string path) {

            Log.Info($"Importing resources in {path}");

            var sourceFiles = Directory.GetFiles(path, "*", SearchOption.AllDirectories);

            foreach (var importer in importers) {

                var importerName = importer.GetType().Name;

                var importSources = new List<ImportSource>();
                var markedImportSources = new List<ImportSource>();

                foreach (var sourcePath in sourceFiles) {

                    var relativePath = GetRelativePath(path, sourcePath);
                    var ext = GetExtension(sourcePath);
                    var hash = await FileM.GetHash(sourcePath);

                    if (!importer.SupportedExtensions().Contains(ext)) continue;

                    var targetFile = hash + importer.GetTargetExtension(ext);
                    var targetPath = Combine(project.ResGenPath, targetFile);

                    var importSource = new ImportSource {

                        SourcePath = sourcePath,
                        TargetPath = targetPath,

                        SourceRelativePath = relativePath,
                        TargetRelativePath = targetFile
                    };

                    importSources.Add(importSource);

                    resMap[relativePath] = targetFile;
                    resHash[relativePath] = hash;

                    if (loadedResHash.TryGetValue(relativePath, out var storedHash) && storedHash == hash) continue;

                    markedImportSources.Add(importSource);

                    Log.Info($"Marked {relativePath} for {importerName}");
                }

                foreach (var importSource in importSources)
                    sidekicks.AddRange(importer.GetSideKicks(project, importSource));

                if (markedImportSources.Count == 0) {

                    Log.Info($"Skipping {importerName}");
                    continue;
                }

                Log.Info($"Running {importerName}");

                await importer.ImportOperation(project, markedImportSources.ToArray());

                Log.Info($"Finished {importerName}");
            }
        }
    }
}
