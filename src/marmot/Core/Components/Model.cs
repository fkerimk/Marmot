namespace Marmot;

[Component]
public struct Model(string path) {

    public float Scale = 1;

    public readonly ModelRes Resource = Res.GetModel(path);
    public Raylib_cs.Model RlModel => Resource.RlModel;

    public readonly float FramesPerSecond = 25;
    public readonly float FrameDuration => 1f / FramesPerSecond;
}