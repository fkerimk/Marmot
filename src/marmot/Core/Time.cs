using static Raylib_cs.Raylib;

namespace Marmot;

public static class Time {

    public static float Current => (float)GetTime();
    public static float Delta => GetFrameTime();

    public static float FixedTime;
    public const float FixedDelta = 1f / 60f;

    private static float _accumulator = 0f;
    public static void Update(Game game) {

        _accumulator += MathF.Min(Delta, 0.25f);

        while (_accumulator >= FixedDelta) {

            FixedTime += FixedDelta;

            game.FixedLoop();
            Scene.Current?.FixedLoop();

            _accumulator -= FixedDelta;
        }
    }
}