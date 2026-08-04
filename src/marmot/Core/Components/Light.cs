using System.Numerics;

namespace Marmot;

[Component]
public struct Light(Vector4 color, LightType type = LightType.Point, float intensity = 1f, float range = 10f) {

    public Vector4 Color     = color;
    public LightType Type    = type;
    public float Intensity   = intensity;

    // Max range for point and spot-lights only
    public float Range = range;

    /* Inner & outer cone angles (in degrees) only for spot-light.
    Full intensity within inner angle. Outside outer angle, it is completely dark. */
    public float InnerAngle = 12.5f;
    public float OuterAngle = 20.0f;

    public Light() : this(Vector4.One) { }
}

public enum LightType {

    Directional,
    Point,
    Spot,
}