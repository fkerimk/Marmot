using System.Numerics;

namespace Marmot;

[Component]
public struct PositionComponent(Vector3 value) {

    public Vector3 Value = value;
}
