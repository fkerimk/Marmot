namespace Marmot.Backend.Resources.Importers;

internal class DirectImporter : Importer {

    public override string[] SupportedExtensions()  => [ ".vs", ".fs" ];
    public override string GetTargetExtension(string sourceExtension) => sourceExtension;

    public override async Task ImportOperation(string sourcePath, string targetPath) {

        await using var sourceStream = File.OpenRead(sourcePath);
        await using var targetStream = File.Create(targetPath);
        await sourceStream.CopyToAsync(targetStream);
    }
}