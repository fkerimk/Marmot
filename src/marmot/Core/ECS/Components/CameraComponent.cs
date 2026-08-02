using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Marmot;

[Component]
public struct CameraComponent(float fov) {

    public float Fov = fov;

    public static Vector3 GetForward(int id) {

        if (!id.HasRotation()) return Vector3.Zero;

        var rot = id.GetRotation();

        var pitch = rot.Value.X * DEG2RAD;
        var yaw = rot.Value.Y * DEG2RAD;

        return Vector3.Normalize(new Vector3(

            MathF.Cos(pitch) * MathF.Sin(yaw),
            MathF.Sin(pitch),
            MathF.Cos(pitch) * MathF.Cos(yaw)
        ));
    }

    public static void LookAt(int id, Vector3 targetPos) {

        if (!id.HasPosition()) return;

        var pos = id.GetPosition();
        var rot = id.EnsureRotation();

        var direction = Vector3.Normalize(targetPos - pos.Value);

        rot.Value.X = MathF.Asin(direction.Y) * RAD2DEG;
        rot.Value.Y = MathF.Atan2(direction.X, direction.Z) * RAD2DEG;
        rot.Value.Z = 0f;

        id.SetRotation(rot);
    }

    public static Camera3D GetRlCamera(int id) {

        var cam = id.GetCameraOrDefault();
        var pos = id.GetPositionOrDefault();

        return new() {

            Up = Vector3.UnitY,
            Projection = CameraProjection.Perspective,
            Position = pos.Value,
            Target = pos.Value + GetForward(id),
            FovY = cam.Fov
        };
    }
}