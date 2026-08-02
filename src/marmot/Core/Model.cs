using Raylib_cs;
using static Raylib_cs.Raylib;

using Marmot.Backend.Resources.Types;

namespace Marmot;

public unsafe class Model : Resource {

    // Model
    private Raylib_cs.Model? _rlModel;

    // Animations
    public ModelAnimation* RlAnims;
    private int _animCount;

    public float FrameDuration = 1.0f / 30.0f;

    public override void Import(string path) {

        _rlModel = LoadModel(path);
        RlAnims = LoadModelAnimations(path, ref _animCount);

        var shader = Res.GetShader("shaders/skinning").RlShader
                     ?? throw new FileNotFoundException("Skinning shader not found");

        for (var i = 0; i < _rlModel.Value.MaterialCount; i++) _rlModel.Value.Materials[i].Shader = shader;
    }

    public override void Unload() {

        if (_rlModel == null) return;

        UnloadModelAnimations(RlAnims, _animCount);
        UnloadModel(_rlModel.Value);
    }

    public Raylib_cs.Model GetRlModel() {

        return _rlModel ?? throw new NullReferenceException("Model is not imported");
    }
}