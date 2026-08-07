using Newtonsoft.Json;
using static System.IO.Path;

using Marmot.Backend.Projects;
using Marmot.Backend.Resources.Importers;

namespace Marmot.Backend.Resources;

internal static partial class ResMan {

    private static readonly Importer[] Importers = [

        new BlenderImporter(),
        new DirectImporter()
    ];

    internal static async Task Sync(Project project) {

        // Ensure directories
        Directory.CreateDirectory(project.ResPath);
        Directory.CreateDirectory(project.ResGenPath);

        var existResData = File.Exists(project.ResDataPath)
            ? JsonConvert.DeserializeObject<Dictionary<string, ResData>>(await File.ReadAllTextAsync(project.ResDataPath)) ?? throw Log.InvalidJsonException(project.ResDataPath)
            : new Dictionary<string, ResData>();

        var resMap = new Dictionary<string, string>();
        var resData = new Dictionary<string, ResData>();
        var keepFiles = new List<string> { project.ResMapPath, project.ResDataPath };

        await ImportResources(project.ResPath);
        await ImportResources(Join(AppContext.BaseDirectory, "res"));

        // Clean target
        var targetFiles = Directory.GetFiles(project.ResGenPath, "*", SearchOption.AllDirectories);

        foreach (var filePath in targetFiles) {

            var relativePath = GetRelativePath(project.ResGenPath, filePath);
            if (keepFiles.Contains(filePath)) continue;
            File.Delete(filePath);
            Log.Info($"Removed import of {relativePath}");
        }

        // Save maps
        var resMapJson = JsonConvert.SerializeObject(resMap, Formatting.Indented);
        var resDataJson = JsonConvert.SerializeObject(resData, Formatting.Indented);

        await File.WriteAllTextAsync(project.ResMapPath, resMapJson);
        await File.WriteAllTextAsync(project.ResDataPath, resDataJson);

        return;

        async Task ImportResources(string path) {

            Log.Info($"Importing resources in {path}");

            var sourceFiles = Directory.GetFiles(path, "*", SearchOption.AllDirectories);

            foreach (var importer in Importers) {

                var importerName = importer.GetType().Name;
                var markedSources = new List<ImportSource>();

                foreach (var sourcePath in sourceFiles) {

                    var relativePath = GetRelativePath(path, sourcePath);
                    var ext = GetExtension(sourcePath);
                    var hash = await FileM.GetHash(sourcePath);

                    if (!importer.SupportedExtensions().Contains(ext)) continue;

                    var targetFile = hash + importer.GetTargetExtension(ext);
                    var targetPath = Combine(project.ResGenPath, targetFile);

                    var importSource = new ImportSource {

                        SrcPath = sourcePath,
                        SrcRelPath = relativePath,
                        SrcResPath = project.ResPath,

                        TargetPath = targetPath,
                        TargetRelPath = targetFile,
                        TargetResPath = project.ResGenPath
                    };

                    var sidekicks = (await Task.WhenAll(
                        importer.GetImportSideKicks(project, importSource)
                            .Select(async relPath => KeyValuePair.Create(
                                relPath,
                                await FileM.GetHash(Combine(importSource.SrcResPath, relPath))
                            ))
                    )).ToDictionary(x => x.Key, x => x.Value);


                    resMap[relativePath] = targetFile;

                    resData[relativePath] = new ResData {

                        Hash = hash,
                        Sidekicks = sidekicks
                    };

                    keepFiles.Add(targetPath);
                    keepFiles.AddRange(importer.GetExportSideKicks(project, importSource));

                    if (existResData.TryGetValue(relativePath, out var storedData)
                        && storedData.Hash == hash
                        && sidekicks.Count == storedData.Sidekicks.Count
                        && !sidekicks.Except(storedData.Sidekicks).Any())
                        continue;

                    markedSources.Add(importSource);

                    Log.Info($"Marked {relativePath} for {importerName}");
                }

                if (markedSources.Count == 0) {

                    Log.Info($"Skipping {importerName}");
                    continue;
                }

                Log.Info($"Running {importerName}");
                await importer.ImportOperation(project, markedSources.ToArray());
                Log.Info($"Finished {importerName}");
            }
        }
    }
}
