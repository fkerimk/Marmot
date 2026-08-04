using System.Drawing;
using System.Numerics;
using RlColor = Raylib_cs.Color;

public static class Colors {

    public static readonly Vector4 Red    = "#fe5a1d".ToColor();
    public static readonly Vector4 Orange = "#ff8c00".ToColor();
    public static readonly Vector4 Green  = "#0bda51".ToColor();
    public static readonly Vector4 Blue   = "#468FEA".ToColor();
    public static readonly Vector4 White  = "#F2F3F4".ToColor();
    public static readonly Vector4 Gray   = "#555555".ToColor();
    public static readonly Vector4 Black  = "#010B13".ToColor();

    public static readonly Vector4 Background = Gray;
    public static readonly Vector4 Primary    = Orange;
    public static readonly Vector4 Wireframe  = Red;
}

public static partial class Extensions {

    /// <summary>https://coolors.co/palette/f94144-f3722c-f8961e-f9844a-f9c74f-90be6d-43aa8b-4d908e-577590-277da1</summary>
    public static readonly Vector4[] VibrantTones = new [] {"#f94144","#f3722c","#f8961e","#f9844a","#f9c74f","#90be6d","#43aa8b","#4d908e","#577590","#277da1"}.ToColors();

    /// <summary>https://coolors.co/palette/fbf8cc-fde4cf-ffcfd2-f1c0e8-cfbaf0-a3c4f3-90dbf4-8eecf5-98f5e1-b9fbc0</summary>
    public static readonly Vector4[] SoftRainbow = new [] {"#fbf8cc","#fde4cf","#ffcfd2","#f1c0e8","#cfbaf0","#a3c4f3","#90dbf4","#8eecf5","#98f5e1","#b9fbc0"}.ToColors();

    public static Vector4 ToColor(this string hex) {

        hex = '#' + hex.TrimStart('#').ToLowerInvariant();

        var color = ColorTranslator.FromHtml(hex);
        var vector = new Vector4(color.R, color.G, color.B, color.A);

        return vector;
    }

    public static Vector4[] ToColors(this string[] hexes) {

        var colors = new Vector4[hexes.Length];
        Parallel.For(0, hexes.Length, i => colors[i] = hexes[i].ToColor());

        return colors;
    }

    extension(Vector4 color) {

        public RlColor ToRlColor() => new((byte)color.X, (byte)color.Y, (byte)color.Z, (byte)color.W);
        public Vector4 ToImColor() => new(color.X / 255f, color.Y / 255f, color.Z / 255f, color.W / 255f);
    }

    extension(Vector4[] colors) {

        public RlColor[] ToRl() {

            var rayColors = new RlColor[colors.Length];
            Parallel.For(0, colors.Length, i => rayColors[i] = colors[i].ToRlColor());

            return rayColors;
        }

        public Vector4[] ToImColor() {

            var imColors = new Vector4[colors.Length];
            Parallel.For(0, colors.Length, i => imColors[i] = colors[i].ToImColor());

            return imColors;
        }
    }
}