using System.Diagnostics;

namespace Marmot.Backend.Resources.Importers;

internal class BlenderImporter : Importer {

    public override string[] SupportedExtensions() => [ ".blend" ];
    public override string GetTargetExtension(string sourceExtension) =>  ".m3d" ;

    public override async Task ImportOperation(string sourcePath, string targetPath) {

        var dir = Path.GetDirectoryName(sourcePath);

        var startInfo = new ProcessStartInfo {

            FileName = "blender",
            Arguments = $"-b \"{sourcePath}\" --python \"{Path.Join(AppContext.BaseDirectory, "lib/io_scene_m3d.py")}\" --python-expr \"import bpy; bpy.ops.export_scene.m3d(filepath='{targetPath}', use_inline=True)\"",
            WorkingDirectory = dir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);

        if (process != null) {

            //process.OutputDataReceived += (s, e) => {
            //    if (e.Data != null) Console.WriteLine(e.Data);
            //};
            //process.ErrorDataReceived += (s, e) => {
            //    if (e.Data != null) Console.Error.WriteLine(e.Data);
            //};

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            var exitCode = process.ExitCode;
        }
    }
}