using System.Numerics;

namespace Marmot;

[Component]
public struct Model(string path) {

    public float Scale = 1;
    public float FramesPerSecond = 60;
    public readonly int AnimationCount = Res.GetModel(path).AnimCount;

    internal readonly float FrameDuration => 1f / FramesPerSecond;

    internal readonly ModelRes Resource = Res.GetModel(path);
    internal readonly Raylib_cs.Model RlModel = Res.GetModel(path).RlModel;

    internal Vector3 AlbedoBlend   = Vector3.One;
    internal Vector3 EmissiveBlend = Vector3.One;

    internal float EmissiveIntensity = 1;

    internal float AlbedoMultiplier = 1;
    internal float MetalnessMultiplier = 1;
    internal float NormalMultiplier    = 1;
    internal float RoughnessMultiplier = 1;
    internal float OcclusionMultiplier = 1;

    internal float AlbedoOverride    = -1;
    internal float MetalnessOverride = -1;
    internal float NormalOverride    = -1;
    internal float RoughnessOverride = -1;
    internal float OcclusionOverride = -1;
    internal float EmissiveOverride  = -1;
}
