namespace Marmot;

[Component]
public struct FreeCamComponent(float speed) {

    public float Speed = speed;

    public FreeCamComponent() : this(1) { }
}