using Raylib_cs;

namespace Marmot;

[Component]
public struct Camera(float fov) {

    public float Fov = fov;

    public static Camera3D GetRlCamera(int id) {

        var camera = id.RequireCamera();
        var transform = id.GetTransformOrDefault();

        return new() {

            Up = transform.RlUp,
            Projection = CameraProjection.Perspective,
            Position = transform.RlPosition,
            Target = transform.RlPosition + transform.RlForward,
            FovY = camera.Fov
        };
    }
}