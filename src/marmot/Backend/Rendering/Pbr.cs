using System.Numerics;
using Marmot.Backend.Resources;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Marmot.Backend.Rendering;

internal static class Pbr {

    private const int MaxLights = 4;

    private static Shader[] _shaders = [];

    // A uniform with the same name may have a different location index in two different shaders
    private static int[] _matNormalLoc = [];
    private static int[] _viewPosLoc = [];
    private static int[] _ambientColorLoc = [];
    private static int[] _ambientIntensityLoc = [];
    private static int[] _lightsCountLoc = [];

    private static int[,] _enabledLoc   = new int[0, 0];
    private static int[,] _typeLoc      = new int[0, 0];
    private static int[,] _posLoc       = new int[0, 0];
    private static int[,] _dirLoc       = new int[0, 0];
    private static int[,] _colorLoc     = new int[0, 0];
    private static int[,] _intensityLoc = new int[0, 0];
    private static int[,] _rangeLoc     = new int[0, 0];
    private static int[,] _innerLoc     = new int[0, 0];
    private static int[,] _outerLoc     = new int[0, 0];

    // Material uniform locations cached to avoid calling GetShaderLocation for every frame
    private static int[] _metallicLoc = [];
    private static int[] _roughnessLoc = [];
    private static int[] _aoLoc = [];
    private static int[] _emissiveColorLoc = [];
    private static int[] _emissiveIntensityLoc = [];
    private static int[] _useTexAlbedoLoc = [];
    private static int[] _useTexMetalnessLoc = [];
    private static int[] _useTexNormalLoc = [];
    private static int[] _useTexRoughnessLoc = [];
    private static int[] _useTexOcclusionLoc = [];
    private static int[] _useTexEmissiveLoc = [];

    internal static void LoadMainShaders() {

        var shader = new ShaderRes();
        var skinShader = new ShaderRes();

        var vsPath = ResMan.FindResourcePath("shaders/main.vs");
        var vsSkinnedPath = ResMan.FindResourcePath("shaders/main-skinned.vs");
        var fsPath = ResMan.FindResourcePath("shaders/main.fs");

        shader.RlShader = LoadShader(vsPath, fsPath);
        skinShader.RlShader = LoadShader(vsSkinnedPath, fsPath);

        ResMan.ResMap["shaders/main"] = shader;

        Res.MainShader = shader;
        Res.SkinnedMainShader = skinShader;

        _shaders = [Res.MainShader.RlShader!.Value, Res.SkinnedMainShader.RlShader!.Value];

        var count = _shaders.Length;

        _matNormalLoc = new int[count];
        _viewPosLoc = new int[count];
        _ambientColorLoc = new int[count];
        _ambientIntensityLoc = new int[count];
        _lightsCountLoc = new int[count];

        _enabledLoc   = new int[count, MaxLights];
        _typeLoc      = new int[count, MaxLights];
        _posLoc       = new int[count, MaxLights];
        _dirLoc       = new int[count, MaxLights];
        _colorLoc     = new int[count, MaxLights];
        _intensityLoc = new int[count, MaxLights];
        _rangeLoc     = new int[count, MaxLights];
        _innerLoc     = new int[count, MaxLights];
        _outerLoc     = new int[count, MaxLights];

        for (var s = 0; s < count; s++) {

            var rlShader = _shaders[s];

            _matNormalLoc[s] = GetShaderLocation(rlShader, "matNormal");
            _viewPosLoc[s] = GetShaderLocation(rlShader, "viewPos");
            _ambientColorLoc[s] = GetShaderLocation(rlShader, "ambientColor");
            _ambientIntensityLoc[s] = GetShaderLocation(rlShader, "ambientIntensity");
            _lightsCountLoc[s] = GetShaderLocation(rlShader, "lightsCount");

            for (var i = 0; i < MaxLights; i++) {

                _enabledLoc[s, i]   = GetShaderLocation(rlShader, $"lights[{i}].enabled");
                _typeLoc[s, i]      = GetShaderLocation(rlShader, $"lights[{i}].type");
                _posLoc[s, i]       = GetShaderLocation(rlShader, $"lights[{i}].position");
                _dirLoc[s, i]       = GetShaderLocation(rlShader, $"lights[{i}].direction");
                _colorLoc[s, i]     = GetShaderLocation(rlShader, $"lights[{i}].color");
                _intensityLoc[s, i] = GetShaderLocation(rlShader, $"lights[{i}].intensity");
                _rangeLoc[s, i]     = GetShaderLocation(rlShader, $"lights[{i}].range");
                _innerLoc[s, i]     = GetShaderLocation(rlShader, $"lights[{i}].innerCutoff");
                _outerLoc[s, i]     = GetShaderLocation(rlShader, $"lights[{i}].outerCutoff");
            }
        }

        // Cache the uniform locations of the materials
        _metallicLoc          = new int[count];
        _roughnessLoc         = new int[count];
        _aoLoc                = new int[count];
        _emissiveColorLoc     = new int[count];
        _emissiveIntensityLoc = new int[count];
        _useTexAlbedoLoc      = new int[count];
        _useTexMetalnessLoc   = new int[count];
        _useTexNormalLoc      = new int[count];
        _useTexRoughnessLoc   = new int[count];
        _useTexOcclusionLoc   = new int[count];
        _useTexEmissiveLoc    = new int[count];

        for (var s = 0; s < count; s++) {

            var rl = _shaders[s];

            _metallicLoc[s]          = GetShaderLocation(rl, "metallicValue");
            _roughnessLoc[s]         = GetShaderLocation(rl, "roughnessValue");
            _aoLoc[s]                = GetShaderLocation(rl, "aoValue");
            _emissiveColorLoc[s]     = GetShaderLocation(rl, "emissiveColor");
            _emissiveIntensityLoc[s] = GetShaderLocation(rl, "emissiveIntensity");
            _useTexAlbedoLoc[s]      = GetShaderLocation(rl, "useTexAlbedo");
            _useTexMetalnessLoc[s]   = GetShaderLocation(rl, "useTexMetalness");
            _useTexNormalLoc[s]      = GetShaderLocation(rl, "useTexNormal");
            _useTexRoughnessLoc[s]   = GetShaderLocation(rl, "useTexRoughness");
            _useTexOcclusionLoc[s]   = GetShaderLocation(rl, "useTexOcclusion");
            _useTexEmissiveLoc[s]    = GetShaderLocation(rl, "useTexEmissive");
        }

        Console.WriteLine($"lightsCountLoc[0]={_lightsCountLoc[0]} lightsCountLoc[1]={_lightsCountLoc[1]}");
        Console.WriteLine($"posLoc[0,0]={_posLoc[0,0]} posLoc[1,0]={_posLoc[1,0]}");
        Console.WriteLine($"matNormalLoc[0]={_matNormalLoc[0]} matNormalLoc[1]={_matNormalLoc[1]}");
    }

    private static void SetAmbient(Vector3 color, float intensity) {

        for (var s = 0; s < _shaders.Length; s++) {

            SetShaderValue(_shaders[s], _ambientColorLoc[s], color, ShaderUniformDataType.Vec3);
            SetShaderValue(_shaders[s], _ambientIntensityLoc[s], intensity, ShaderUniformDataType.Float);
        }
    }

    private static void SetViewPosition(Vector3 camPos) {

        for (var s = 0; s < _shaders.Length; s++)
            SetShaderValue(_shaders[s], _viewPosLoc[s], camPos, ShaderUniformDataType.Vec3);
    }

    internal static void SetNormalMatrix(Matrix4x4 modelMatrix) {

        Matrix4x4.Invert(modelMatrix, out var inv);
        var normalMatrix = Matrix4x4.Transpose(inv);

        for (var s = 0; s < _shaders.Length; s++)
            SetShaderValueMatrix(_shaders[s], _matNormalLoc[s], normalMatrix);
    }

    private static void UpdateLights() {

        var index = 0;

        foreach (var (id, light) in Scene.GetComponents<Light>()) {

            if (index >= MaxLights) break;

            var transform = id.GetTransformOrDefault();

            var position  = transform.RlPosition;
            var direction = transform.RlForward;

            var innerCutoff = MathF.Cos(light.InnerAngle * MathF.PI / 180f);
            var outerCutoff = MathF.Cos(light.OuterAngle * MathF.PI / 180f);

            for (var s = 0; s < _shaders.Length; s++) {

                SetShaderValue(_shaders[s], _enabledLoc[s, index],   1,               ShaderUniformDataType.Int);
                SetShaderValue(_shaders[s], _typeLoc[s, index],      (int)light.Type, ShaderUniformDataType.Int);
                SetShaderValue(_shaders[s], _posLoc[s, index],       position,        ShaderUniformDataType.Vec3);
                SetShaderValue(_shaders[s], _dirLoc[s, index],       direction,       ShaderUniformDataType.Vec3);
                SetShaderValue(_shaders[s], _colorLoc[s, index],     light.Color,     ShaderUniformDataType.Vec4);
                SetShaderValue(_shaders[s], _intensityLoc[s, index], light.Intensity, ShaderUniformDataType.Float);
                SetShaderValue(_shaders[s], _rangeLoc[s, index],     light.Range,     ShaderUniformDataType.Float);
                SetShaderValue(_shaders[s], _innerLoc[s, index],     innerCutoff,     ShaderUniformDataType.Float);
                SetShaderValue(_shaders[s], _outerLoc[s, index],     outerCutoff,     ShaderUniformDataType.Float);
            }

            index++;
        }

        // Close the remaining slots
        for (var i = index; i < MaxLights; i++)
            for (var s = 0; s < _shaders.Length; s++)
                SetShaderValue(_shaders[s], _enabledLoc[s, i], 0, ShaderUniformDataType.Int);

        for (var s = 0; s < _shaders.Length; s++)
            SetShaderValue(_shaders[s], _lightsCountLoc[s], index, ShaderUniformDataType.Int);
    }

    private static void ApplyUniforms(
        float metallic,
        float roughness,
        float ao,
        Vector3 emissiveColor,
        float emissiveIntensity,
        bool hasAlbedoTex,
        bool hasMetalnessTex,
        bool hasNormalTex,
        bool hasRoughnessTex,
        bool hasOcclusionTex,
        bool hasEmissiveTex)
    {
        for (var s = 0; s < _shaders.Length; s++) {

            SetShaderValue(_shaders[s], _metallicLoc[s],          metallic,          ShaderUniformDataType.Float);
            SetShaderValue(_shaders[s], _roughnessLoc[s],         roughness,         ShaderUniformDataType.Float);
            SetShaderValue(_shaders[s], _aoLoc[s],                ao,                ShaderUniformDataType.Float);
            SetShaderValue(_shaders[s], _emissiveColorLoc[s],     emissiveColor,     ShaderUniformDataType.Vec3);
            SetShaderValue(_shaders[s], _emissiveIntensityLoc[s], emissiveIntensity, ShaderUniformDataType.Float);

            SetShaderValue(_shaders[s], _useTexAlbedoLoc[s],    hasAlbedoTex    ? 1 : 0, ShaderUniformDataType.Int);
            SetShaderValue(_shaders[s], _useTexMetalnessLoc[s], hasMetalnessTex ? 1 : 0, ShaderUniformDataType.Int);
            SetShaderValue(_shaders[s], _useTexNormalLoc[s],    hasNormalTex    ? 1 : 0, ShaderUniformDataType.Int);
            SetShaderValue(_shaders[s], _useTexRoughnessLoc[s], hasRoughnessTex ? 1 : 0, ShaderUniformDataType.Int);
            SetShaderValue(_shaders[s], _useTexOcclusionLoc[s], hasOcclusionTex ? 1 : 0, ShaderUniformDataType.Int);
            SetShaderValue(_shaders[s], _useTexEmissiveLoc[s],  hasEmissiveTex  ? 1 : 0, ShaderUniformDataType.Int);
        }
    }

    internal static void ApplyUniforms() {

        ApplyUniforms(
            metallic: 0.0f, roughness: 0.6f, ao: 1.0f,
            emissiveColor: Vector3.Zero, emissiveIntensity: 0f,
            hasAlbedoTex: true, hasMetalnessTex: true, hasNormalTex: true,
            hasRoughnessTex: true, hasOcclusionTex: true, hasEmissiveTex: true
        );
    }

    internal static void Update() {

        var cam = Scene.GetComponents<Camera>().First().Key;
        var camTransform = cam.EnsureTransform();

        SetViewPosition(camTransform.RlPosition);
        SetAmbient(new Vector3(0.03f, 0.03f, 0.03f), 1.0f);
        UpdateLights();
    }
}