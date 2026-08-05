using System.Numerics;

namespace Marmot;

[Component]
public struct Model(string path) {

    public float Scale = 1;

    public Vector3 AlbedoBlend   = Vector3.One;
    public Vector3 EmissiveBlend = Vector3.One;

    public float EmissiveIntensity = 1;

    public float AlbedoMultiplier    = 1;
    public float MetalnessMultiplier = 1;
    public float NormalMultiplier    = 1;
    public float RoughnessMultiplier = 1;
    public float OcclusionMultiplier = 1;

    public float AlbedoOverride    = -1;
    public float MetalnessOverride = -1;
    public float NormalOverride    = -1;
    public float RoughnessOverride = -1;
    public float OcclusionOverride = -1;
    public float EmissiveOverride  = -1;

    public readonly ModelRes Resource = Res.GetModel(path);
    public Raylib_cs.Model RlModel => Resource.RlModel;

    public readonly float FramesPerSecond = 25;
    public readonly float FrameDuration => 1f / FramesPerSecond;
}
