namespace Marmot;

[Component]
public struct FreeCam(float speed) {

    public float Speed = speed;

    public FreeCam() : this(1) { }
}