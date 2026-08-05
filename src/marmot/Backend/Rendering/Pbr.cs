using System.Numerics;
using Marmot.Backend.Resources;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Marmot.Backend.Rendering;

internal static class Pbr {

    private const int MaxLights = 4;

    internal static Shader MainShader;
    internal static Shader SkinnedMainShader;

    private static Shader[] _shaders = [];

    // A uniform with the same name may have a different location index in two different shaders
    private static int[] _matNormalLoc        = [];
    private static int[] _viewPosLoc          = [];
    private static int[] _ambientColorLoc     = [];
    private static int[] _ambientIntensityLoc = [];
    private static int[] _lightsCountLoc      = [];

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
    private static int[] _albedoBlendLoc         = [];
    private static int[] _emissiveBlendLoc       = [];
    private static int[] _albedoMultiplierLoc    = [];
    private static int[] _metalnessMultiplierLoc = [];
    private static int[] _normalMultiplierLoc    = [];
    private static int[] _roughnessMultiplierLoc = [];
    private static int[] _occlusionMultiplierLoc = [];
    private static int[] _emissiveIntensityLoc   = [];
    private static int[] _albedoOverrideLoc      = [];
    private static int[] _metalnessOverrideLoc   = [];
    private static int[] _normalOverrideLoc      = [];
    private static int[] _roughnessOverrideLoc   = [];
    private static int[] _occlusionOverrideLoc   = [];
    private static int[] _emissiveOverrideLoc    = [];
    private static int[] _useTexAlbedoLoc        = [];
    private static int[] _useTexMetalnessLoc     = [];
    private static int[] _useTexNormalLoc        = [];
    private static int[] _useTexRoughnessLoc     = [];
    private static int[] _useTexOcclusionLoc     = [];
    private static int[] _useTexEmissiveLoc      = [];

    private const bool UseTexAlbedo    = true;
    private const bool UseTexMetalness = true;
    private const bool UseTexNormal    = true;
    private const bool UseTexRoughness = true;
    private const bool UseTexOcclusion = true;
    private const bool UseTexEmissive  = true;

    internal static void Load() {

        var shader = new ShaderRes();
        var skinShader = new ShaderRes();

        var vsPath = ResMan.FindResourcePath("shaders/main.vs");
        var vsSkinnedPath = ResMan.FindResourcePath("shaders/main-skinned.vs");
        var fsPath = ResMan.FindResourcePath("shaders/main.fs");

        shader.RlShader = LoadShader(vsPath, fsPath);
        skinShader.RlShader = LoadShader(vsSkinnedPath, fsPath);

        ResMan.ResMap["shaders/main"] = shader;

        MainShader = shader.RlShader.Value;
        SkinnedMainShader = skinShader.RlShader.Value;

        //SetupMaterialMapLocations(ref MainShader);
        //SetupMaterialMapLocations(ref SkinnedMainShader);

        _shaders = [ MainShader, SkinnedMainShader ];

        var count = _shaders.Length;

        _matNormalLoc        = new int[count];
        _viewPosLoc          = new int[count];
        _ambientColorLoc     = new int[count];
        _ambientIntensityLoc = new int[count];
        _lightsCountLoc      = new int[count];

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
        _albedoBlendLoc         = new int[count];
        _emissiveBlendLoc       = new int[count];
        _albedoMultiplierLoc    = new int[count];
        _metalnessMultiplierLoc = new int[count];
        _normalMultiplierLoc    = new int[count];
        _roughnessMultiplierLoc = new int[count];
        _occlusionMultiplierLoc = new int[count];
        _emissiveIntensityLoc   = new int[count];
        _albedoOverrideLoc      = new int[count];
        _metalnessOverrideLoc   = new int[count];
        _normalOverrideLoc      = new int[count];
        _roughnessOverrideLoc   = new int[count];
        _occlusionOverrideLoc   = new int[count];
        _emissiveOverrideLoc    = new int[count];
        _useTexAlbedoLoc        = new int[count];
        _useTexMetalnessLoc     = new int[count];
        _useTexNormalLoc        = new int[count];
        _useTexRoughnessLoc     = new int[count];
        _useTexOcclusionLoc     = new int[count];
        _useTexEmissiveLoc      = new int[count];

        for (var s = 0; s < count; s++) {

            var rl = _shaders[s];

            _albedoBlendLoc[s]         = GetShaderLocation(rl, "albedoBlend");
            _emissiveBlendLoc[s]       = GetShaderLocation(rl, "emissiveBlend");
            _albedoMultiplierLoc[s]    = GetShaderLocation(rl, "albedoMultiplier");
            _metalnessMultiplierLoc[s] = GetShaderLocation(rl, "metalnessMultiplier");
            _normalMultiplierLoc[s]    = GetShaderLocation(rl, "normalMultiplier");
            _roughnessMultiplierLoc[s] = GetShaderLocation(rl, "roughnessMultiplier");
            _occlusionMultiplierLoc[s] = GetShaderLocation(rl, "occlusionMultiplier");
            _emissiveIntensityLoc[s]   = GetShaderLocation(rl, "emissiveIntensity");
            _albedoOverrideLoc[s]      = GetShaderLocation(rl, "albedoOverride");
            _metalnessOverrideLoc[s]   = GetShaderLocation(rl, "metalnessOverride");
            _normalOverrideLoc[s]      = GetShaderLocation(rl, "normalOverride");
            _roughnessOverrideLoc[s]   = GetShaderLocation(rl, "roughnessOverride");
            _occlusionOverrideLoc[s]   = GetShaderLocation(rl, "occlusionOverride");
            _emissiveOverrideLoc[s]    = GetShaderLocation(rl, "emissiveOverride");
            _useTexAlbedoLoc[s]        = GetShaderLocation(rl, "useTexAlbedo");
            _useTexMetalnessLoc[s]     = GetShaderLocation(rl, "useTexMetalness");
            _useTexNormalLoc[s]        = GetShaderLocation(rl, "useTexNormal");
            _useTexRoughnessLoc[s]     = GetShaderLocation(rl, "useTexRoughness");
            _useTexOcclusionLoc[s]     = GetShaderLocation(rl, "useTexOcclusion");
            _useTexEmissiveLoc[s]      = GetShaderLocation(rl, "useTexEmissive");
        }
    }

    private static unsafe void SetupMaterialMapLocations(ref Shader shader) {

        shader.Locs[(int)ShaderLocationIndex.MapAlbedo]    = GetShaderLocation(shader, "texture0");
        shader.Locs[(int)ShaderLocationIndex.MapMetalness] = GetShaderLocation(shader, "texture1");
        shader.Locs[(int)ShaderLocationIndex.MapNormal]    = GetShaderLocation(shader, "texture2");
        shader.Locs[(int)ShaderLocationIndex.MapRoughness] = GetShaderLocation(shader, "texture3");
        shader.Locs[(int)ShaderLocationIndex.MapOcclusion] = GetShaderLocation(shader, "texture4");
        shader.Locs[(int)ShaderLocationIndex.MapEmission]  = GetShaderLocation(shader, "texture5");
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

                SetShaderValue(_shaders[s], _enabledLoc[s, index]   , 1               , ShaderUniformDataType.Int);
                SetShaderValue(_shaders[s], _typeLoc[s, index]      , (int)light.Type , ShaderUniformDataType.Int);
                SetShaderValue(_shaders[s], _posLoc[s, index]       , position        , ShaderUniformDataType.Vec3);
                SetShaderValue(_shaders[s], _dirLoc[s, index]       , direction       , ShaderUniformDataType.Vec3);
                SetShaderValue(_shaders[s], _colorLoc[s, index]     , light.Color     , ShaderUniformDataType.Vec4);
                SetShaderValue(_shaders[s], _intensityLoc[s, index] , light.Intensity , ShaderUniformDataType.Float);
                SetShaderValue(_shaders[s], _rangeLoc[s, index]     , light.Range     , ShaderUniformDataType.Float);
                SetShaderValue(_shaders[s], _innerLoc[s, index]     , innerCutoff     , ShaderUniformDataType.Float);
                SetShaderValue(_shaders[s], _outerLoc[s, index]     , outerCutoff     , ShaderUniformDataType.Float);
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

    internal static void ApplyUniforms(Model model) {

        for (var s = 0; s < _shaders.Length; s++) {

            SetShaderValue(_shaders[s], _albedoBlendLoc[s]        , model.AlbedoBlend        , ShaderUniformDataType.Vec3);
            SetShaderValue(_shaders[s], _emissiveBlendLoc[s]      , model.EmissiveBlend      , ShaderUniformDataType.Vec3);
            SetShaderValue(_shaders[s], _albedoMultiplierLoc[s]   , model.AlbedoMultiplier   , ShaderUniformDataType.Float);
            SetShaderValue(_shaders[s], _metalnessMultiplierLoc[s], model.MetalnessMultiplier, ShaderUniformDataType.Float);
            SetShaderValue(_shaders[s], _normalMultiplierLoc[s]   , model.NormalMultiplier   , ShaderUniformDataType.Float);
            SetShaderValue(_shaders[s], _roughnessMultiplierLoc[s], model.RoughnessMultiplier, ShaderUniformDataType.Float);
            SetShaderValue(_shaders[s], _occlusionMultiplierLoc[s], model.OcclusionMultiplier, ShaderUniformDataType.Float);
            SetShaderValue(_shaders[s], _emissiveIntensityLoc[s]  , model.EmissiveIntensity  , ShaderUniformDataType.Float);
            SetShaderValue(_shaders[s], _albedoOverrideLoc[s]     , model.AlbedoOverride     , ShaderUniformDataType.Float);
            SetShaderValue(_shaders[s], _metalnessOverrideLoc[s]  , model.MetalnessOverride  , ShaderUniformDataType.Float);
            SetShaderValue(_shaders[s], _normalOverrideLoc[s]     , model.NormalOverride     , ShaderUniformDataType.Float);
            SetShaderValue(_shaders[s], _roughnessOverrideLoc[s]  , model.RoughnessOverride  , ShaderUniformDataType.Float);
            SetShaderValue(_shaders[s], _occlusionOverrideLoc[s]  , model.OcclusionOverride  , ShaderUniformDataType.Float);
            SetShaderValue(_shaders[s], _emissiveOverrideLoc[s]   , model.EmissiveOverride   , ShaderUniformDataType.Float);

            SetShaderValue(_shaders[s], _useTexAlbedoLoc[s]   , UseTexAlbedo    ? 1 : 0, ShaderUniformDataType.Int);
            SetShaderValue(_shaders[s], _useTexMetalnessLoc[s], UseTexMetalness ? 1 : 0, ShaderUniformDataType.Int);
            SetShaderValue(_shaders[s], _useTexNormalLoc[s]   , UseTexNormal    ? 1 : 0, ShaderUniformDataType.Int);
            SetShaderValue(_shaders[s], _useTexRoughnessLoc[s], UseTexRoughness ? 1 : 0, ShaderUniformDataType.Int);
            SetShaderValue(_shaders[s], _useTexOcclusionLoc[s], UseTexOcclusion ? 1 : 0, ShaderUniformDataType.Int);
            SetShaderValue(_shaders[s], _useTexEmissiveLoc[s] , UseTexEmissive  ? 1 : 0, ShaderUniformDataType.Int);
        }
    }

    internal static void Update() {

        var cam = Scene.GetComponents<Camera>().First().Key;
        var camTransform = cam.EnsureTransform();

        SetViewPosition(camTransform.RlPosition);
        SetAmbient(new Vector3(0.03f, 0.03f, 0.03f), 1.0f);
        UpdateLights();
    }

    internal static void Unload() {

        foreach (var shader in _shaders)
            UnloadShader(shader);
    }
}
