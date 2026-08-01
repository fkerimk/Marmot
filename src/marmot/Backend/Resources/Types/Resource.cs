namespace Marmot.Backend.Resources.Types;

public abstract class Resource {

    public virtual bool RawImportPath => false;

    public abstract void Import(string path);
    public abstract void Unload();
}