using Marmot.Backend.Projects;

namespace Marmot.Backend.Resources.Importers;

internal abstract class Importer {

    public abstract string[] SupportedExtensions();
    public abstract string GetTargetExtension(string sourceExtension);

    public abstract Task ImportOperation(Project project, ImportSource[] sources);

    public virtual string[] GetSideKicks(Project project, ImportSource source) => [];
}