using System.Numerics;
using Raylib_cs;

namespace Marmot;

[Component]
public struct TransformComponent(Vector3 pos, Vector3 rot, Vector3 scale) {

    public Vector3 Position = pos;
    public Vector3 Rotation = rot; // degrees: X=pitch, Y=yaw, Z=roll
    public Vector3 Scale = scale;

    public TransformComponent() : this(Vector3.Zero, Vector3.Zero, Vector3.One) { }

    public void LookAt(Vector3 targetPos) {

        var dir = Vector3.Normalize(targetPos - Position);
        var yaw   = MathF.Atan2(dir.X, dir.Z);
        var pitch = MathF.Asin(dir.Y);

        Rotation = new Vector3(pitch * MathM.Rad2Deg, yaw * MathM.Rad2Deg, Rotation.Z);
    }

    /* To mirror into the right-handed space:
    Only reverses the signs of the components associated with the X-axis.
    This is purely linear algebra, completely independent of the angle formula inside the matrix.
    It remains correct no matter how BuildRotation changes.*/
    private static Matrix4x4 MirrorRotationX(Matrix4x4 r)
        => r with { M12 = -r.M12, M13 = -r.M13, M21 = -r.M21, M31 = -r.M31 };

    private Matrix4x4 GameplayRotation => BuildRotation(Rotation);
    private Matrix4x4 RenderRotation => MirrorRotationX(GameplayRotation);

    // Left-handed direction vectors
    public Vector3 Forward => Raymath.Vector3Transform(Vector3.UnitZ, GameplayRotation);
    public Vector3 Right   => Raymath.Vector3Transform(Vector3.UnitX, GameplayRotation);
    public Vector3 Up      => Raymath.Vector3Transform(Vector3.UnitY, GameplayRotation);

    // Right-handed (Raylib) vectors
    public Vector3 RlPosition => Position with { X = -Position.X };
    public Vector3 RlForward  => Raymath.Vector3Transform(Vector3.UnitZ, RenderRotation);
    public Vector3 RlRight    => Raymath.Vector3Transform(Vector3.UnitX, RenderRotation);
    public Vector3 RlUp       => Raymath.Vector3Transform(Vector3.UnitY, RenderRotation);

    public Matrix4x4 RlMatrix { get {

        var scaleMatrix = Raymath.MatrixScale(Scale.X, Scale.Y, Scale.Z);
        var rotationMatrix = RenderRotation;
        var pos = RlPosition;
        var translationMatrix = Raymath.MatrixTranslate(pos.X, pos.Y, pos.Z);

        return Raymath.MatrixMultiply(Raymath.MatrixMultiply(scaleMatrix, rotationMatrix), translationMatrix);
    }}

    // ZXY order: roll -> pitch -> yaw (yaw is on the outermost axis, around world-Y)
    private static Matrix4x4 BuildRotation(Vector3 rotDeg) {

        var pitch = rotDeg.X * MathM.Deg2Rad;
        var yaw   = rotDeg.Y * MathM.Deg2Rad;
        var roll  = rotDeg.Z * MathM.Deg2Rad;

        var rz = Raymath.MatrixRotateZ(roll);
        var rx = Raymath.MatrixRotateX(-pitch); // Must be inverted
        var ry = Raymath.MatrixRotateY(yaw);

        return Raymath.MatrixMultiply(Raymath.MatrixMultiply(rz, rx), ry);
    }
}