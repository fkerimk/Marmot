namespace Marmot.Backend.Resources.Importers;

internal abstract class Importer {

    public abstract string[] SupportedExtensions();
    public abstract string GetTargetExtension(string sourceExtension);

    public abstract Task ImportOperation(string sourcePath, string targetPath);
}