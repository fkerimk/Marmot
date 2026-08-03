using Raylib_cs;

namespace Marmot;

[Component]
public struct CameraComponent(int id, float fov) {

    public float Fov = fov;

    public Camera3D RlCamera { get {

        var transform = id.GetTransformOrDefault();

        return new() {

            Up = transform.RlUp,
            Projection = CameraProjection.Perspective,
            Position = transform.RlPosition,
            Target = transform.RlPosition + transform.RlForward,
            FovY = Fov
        };
    }}
}