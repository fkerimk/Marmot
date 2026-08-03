namespace Marmot;

[Component]
public struct ModelComponent(string path) {

    public readonly Model Value = Res.GetModel(path);

    public float Scale = 1;

    public Raylib_cs.Model RlModel => Value.RlModel;
}