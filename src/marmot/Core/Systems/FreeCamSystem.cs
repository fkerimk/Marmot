using System.Numerics;

namespace Marmot;

public static class FreeCamSystem {

    public static void Update() {

        foreach (var (id, freeCam) in Scene.GetComponents<FreeCam>()) {

            var transform = id.EnsureTransform();
            transform = transform.Movement();
            transform = transform.Rotation();
            id.SetTransform(transform);
        }
    }

    extension(Transform transform) {

        private Transform Movement() {

            var input = Vector3.Zero;
            if (Input.IsButtonDown(Button.KeyBoardW)) input.Z++;
            if (Input.IsButtonDown(Button.KeyBoardS)) input.Z--;
            if (Input.IsButtonDown(Button.KeyBoardD)) input.X++;
            if (Input.IsButtonDown(Button.KeyBoardA)) input.X--;
            if (Input.IsButtonDown(Button.KeyBoardE)) input.Y++;
            if (Input.IsButtonDown(Button.KeyBoardQ)) input.Y--;

            var movement = transform.Up * input.Y + transform.Right * input.X + transform.Forward * input.Z;
            transform.Position += movement * Time.Delta;

            return transform;
        }

        private Transform Rotation() {

            var doRotate = Input.IsButtonDown(Button.MouseRight);
            Input.CursorLock = doRotate;

            if (!doRotate) return transform;

            var input = new Vector3(-Input.MouseDelta.Y, Input.MouseDelta.X, 0) * 0.35f;
            transform.Rotation += input;
            transform.Rotation.X = float.Clamp(transform.Rotation.X, -90, 90);

            return transform;
        }
    }
}