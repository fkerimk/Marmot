using Marmot.Backend.Rendering;
using Marmot.Backend.Resources;

namespace Marmot.Backend.Player;

public static class Player {

    private static bool _isRunning;

    public static async Task Ignite(Game game, bool debugMode) {

        if (_isRunning) throw new Exception("Player is already running");

        _isRunning = true;

        Compatibility.Check(debugMode);

        await ResMan.LoadPathMap();

        Rl.Init();

        game.Init();

        while (Rl.IsAlive) {

            Rl.BeginDrawing();

            game.Loop();
            Scene.Current?.Loop();
            Time.Update(game);

            CameraRenderSystem.Start();
            AnimationSystem.Update();
            ModelRenderSystem.Draw();
            CameraRenderSystem.End();

            Rl.EndDrawing();
        }

        game.Exit();

        ResMan.UnloadResources();

        Rl.Exit();

        await Task.CompletedTask;
    }
}