using System.Numerics;
using Raylib_cs;

namespace Marmot;

public static partial class Extensions {

    extension(BoundingBox bounds) {

        public BoundingBox Transform(Transform transform) {

            var m = transform.RlMatrix;

            var min = new Vector3(float.MaxValue);
            var max = new Vector3(float.MinValue);

            for (var i = 0; i < 8; i++) {

                var p = Raymath.Vector3Transform(new(
                    (i & 1) == 0 ? bounds.Min.X : bounds.Max.X,
                    (i & 2) == 0 ? bounds.Min.Y : bounds.Max.Y,
                    (i & 4) == 0 ? bounds.Min.Z : bounds.Max.Z
                ), m);

                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }

            return new BoundingBox(min, max);
        }

        public BoundingBox Extend(float extend) =>
            new(bounds.Min - new Vector3(extend), bounds.Max + new Vector3(extend));

        public BoundingBox ScaleExtend(float scale) {

            var center = (bounds.Max + bounds.Min) / 2f;
            var extents = (bounds.Max - bounds.Min) * (scale / 2f);
            return new BoundingBox(center - extents, center + extents);
        }

        public Vector3 GetCenter() =>
            (bounds.Max + bounds.Min) / 2f;

        public Vector3 GetSize() =>
            bounds.Max - bounds.Min;
    }
}