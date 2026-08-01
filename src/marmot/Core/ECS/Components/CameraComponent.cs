using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Marmot;

[Component]
public struct CameraComponent(float fov) {

    public float Fov = fov;

    public static Vector3 GetForward(World world, int id) {

        if (!world.HasRotation(id)) return Vector3.Zero;

        var rot = world.GetRotation(id);

        var pitch = rot.Value.X * DEG2RAD;
        var yaw = rot.Value.Y * DEG2RAD;

        return Vector3.Normalize(new Vector3(

            MathF.Cos(pitch) * MathF.Sin(yaw),
            MathF.Sin(pitch),
            MathF.Cos(pitch) * MathF.Cos(yaw)
        ));
    }

    public static void LookAt(World world, int id, Vector3 targetPos) {

        if (!world.HasPosition(id)) return;

        var pos = world.GetPosition(id);
        var rot = world.EnsureRotation(id);

        var direction = Vector3.Normalize(targetPos - pos.Value);

        rot.Value.X = MathF.Asin(direction.Y) * RAD2DEG;
        rot.Value.Y = MathF.Atan2(direction.X, direction.Z) * RAD2DEG;
        rot.Value.Z = 0f;

        world.SetRotation(id, rot);
    }

    public static Camera3D GetRlCamera(World world, int id) {

        var cam = world.GetCameraOrDefault(id);
        var pos = world.GetPositionOrDefault(id);

        return new() {

            Up = Vector3.UnitY,
            Projection = CameraProjection.Perspective,
            Position = pos.Value,
            Target = pos.Value + GetForward(world, id),
            FovY = cam.Fov
        };
    }
}