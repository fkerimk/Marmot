using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Marmot;

public class Test : Game {

    private World _world = null!;

    private int _camera;
    private int _player;

    public override void Init() {

        _world = new World();

        _camera = _world.CreateEntity();
        _world.SetPosition(_camera, new PositionComponent(Vector3.Create(5)));
        _world.SetRotation(_camera, new RotationComponent());
        _world.SetCamera(_camera, new CameraComponent());
        CameraComponent.LookAt(_world, _camera, Vector3.Zero);

        _player = _world.CreateEntity();
        _world.SetModel(_player, new ModelComponent("models/bearman.blend"));
    }

    public override void Loop() {

        CameraRenderSystem.Start(_world);
        ModelRenderSystem.Draw(_world);
        CameraRenderSystem.End(_world);
    }

    public override void Exit() {
    }
}