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

            Input.Update();

            Rl.BeginDrawing();

            // Logic
            game.Loop();
            Scene.Current?.Loop();
            Time.Update(game);
            FreeCamSystem.Update();

            // Render
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