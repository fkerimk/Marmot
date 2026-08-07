using Marmot.Backend.Projects;

namespace Marmot.Backend.Resources.Importers;

internal abstract class Importer {

    public abstract string[] SupportedExtensions();
    public abstract string GetTargetExtension(string sourceExtension);

    public abstract Task ImportOperation(Project project, ImportSource[] sources);

    public virtual string[] GetImportSideKicks(Project project, ImportSource source) => [];
    public virtual string[] GetExportSideKicks(Project project, ImportSource source) => [];
}