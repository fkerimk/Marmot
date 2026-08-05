using System.Diagnostics;
using Marmot.Backend.Projects;
using Newtonsoft.Json;

namespace Marmot.Backend.Resources.Importers;

internal class BlenderImporter : Importer {

    public override string[] SupportedExtensions() => [ ".blend" ];
    public override string GetTargetExtension(string sourceExtension) =>  ".m3d" ;

    public override async Task ImportOperation(Project project, ImportSource[] sources) {

        var map = new Dictionary<string, string>();

        foreach (var source in sources) {

            map.Add(source.SourcePath, source.TargetPath);
        }

        var json = JsonConvert.SerializeObject(map, Formatting.Indented);

        await File.WriteAllTextAsync(project.ResTargetsPath, json);

        var startInfo = new ProcessStartInfo {

            FileName = "blender",
            Arguments = $"-b --factory-startup -noaudio -P \"{PathM.LibPath("batch_exporter.py")}\" -- \"{project.ResTargetsPath}\"",
            WorkingDirectory = AppContext.BaseDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);

        if (process != null) {

            //process.OutputDataReceived += (s, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
            //process.ErrorDataReceived += (s, e) => { if (e.Data != null) Console.Error.WriteLine(e.Data); };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            var exitCode = process.ExitCode;
        }
    }

    public override string[] GetSideKicks(Project project, ImportSource source) => [ source.TargetRelativePath + ".json" ];
}