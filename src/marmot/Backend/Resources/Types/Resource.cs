namespace Marmot.Backend.Resources.Types;

public abstract class Resource {

    public virtual bool RawImportPath => false;

    internal abstract void Import(string path);
    public abstract void Unload();
}