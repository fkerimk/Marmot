using static Raylib_cs.Raylib;

namespace Marmot;

public static class Time {

    public static float Current => (float)GetTime();
    public static float Delta => GetFrameTime();
}