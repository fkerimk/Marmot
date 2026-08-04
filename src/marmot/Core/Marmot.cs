using System.Reflection;
using Raylib_cs;

namespace Marmot;

public static class Marmot {

    public static readonly string Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "Unknown Version";

    public static void DrawText(string text, int x, int y)
        => Raylib.DrawText(text, x, y, 24, Color.Red);
}
