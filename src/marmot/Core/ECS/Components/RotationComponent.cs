using System.Numerics;

namespace Marmot;

[Component]
public struct RotationComponent(Vector3 value)  {

    public Vector3 Value = value;
}