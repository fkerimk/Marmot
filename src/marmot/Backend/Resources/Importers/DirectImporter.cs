using Marmot.Backend.Projects;

namespace Marmot.Backend.Resources.Importers;

internal class DirectImporter : Importer {

    public override string[] SupportedExtensions()  => [ ".vs", ".fs" ];
    public override string GetTargetExtension(string sourceExtension) => sourceExtension;

    public override async Task ImportOperation(Project project, ImportSource[] sources) {

        foreach (var source in sources) {

            await using var sourceStream = File.OpenRead(source.SrcPath);
            await using var targetStream = File.Create(source.TargetPath);
            await sourceStream.CopyToAsync(targetStream);
        }
    }
}