using Newtonsoft.Json;
using static System.IO.Path;

namespace Marmot.Backend.Projects;

public struct Project {

    public string Name { get; init; }

    [JsonIgnore] public string Path { get; internal set; }

    [JsonIgnore] public string Folder => GetFileName(Path);
    [JsonIgnore] public string Pascal => Folder.ToPascalCase();
    [JsonIgnore] public string SafeName => Pascal.ToLowerInvariant();

    [JsonIgnore] public string DotPath => Join(Path, ".marmot");

    [JsonIgnore] public string ResPath => Join(Path, "res");
    [JsonIgnore] public string ResGenPath => Join(DotPath, "res");
    [JsonIgnore] public string ResMapPath => Join(ResGenPath, "map.json");
    [JsonIgnore] public string ResDataPath => Join(ResGenPath, "data.json");
    [JsonIgnore] public string ResTargetsPath => Join(DotPath, "targets.json");

    [JsonIgnore] public string SrcPath => Join(Path, "src");
    [JsonIgnore] public string SrcGenPath => Join(DotPath, "src");
    [JsonIgnore] public string SrcProjPath => Join(SrcPath, $"{Pascal}.csproj");
    [JsonIgnore] public string SrcEntryPath => Join(SrcGenPath, $"{Pascal}Entry.cs");
    [JsonIgnore] public string SrcBuildPath => Join(SrcGenPath, "build");
    [JsonIgnore] public string SrcBuildResPath => Join(SrcBuildPath, "res");
}