namespace Marmot;

[Component]
public struct ModelComponent(string path) {

    public Model Value = Res.GetModel(path);
}