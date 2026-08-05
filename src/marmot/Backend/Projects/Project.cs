using System.Text.Json.Serialization;
using static System.IO.Path;

namespace Marmot.Backend.Projects;

public class Project() {

    public string Name { get; init; } = null!;

    [JsonIgnore] public string Path { get; private set; } = null!;

    [JsonIgnore] public string Folder { get; private set; } = null!;
    [JsonIgnore] public string Pascal { get; private set; } = null!;
    [JsonIgnore] public string SafeName { get; private set; } = null!;

    [JsonIgnore] public string DotPath { get; private set; } = null!;

    [JsonIgnore] public string ResPath { get; private set; } = null!;
    [JsonIgnore] public string ResGenPath { get; private set; } = null!;
    [JsonIgnore] public string ResMapPath { get; private set; } = null!;
    [JsonIgnore] public string ResHashPath { get; private set; } = null!;
    [JsonIgnore] public string ResTargetsPath { get; private set; } = null!;

    [JsonIgnore] public string SrcPath { get; private set; } = null!;
    [JsonIgnore] public string SrcGenPath { get; private set; } = null!;
    [JsonIgnore] public string SrcProjPath { get; private set; } = null!;
    [JsonIgnore] public string SrcEntryPath { get; private set; } = null!;
    [JsonIgnore] public string SrcBuildPath { get; private set; } = null!;
    [JsonIgnore] public string SrcBuildResPath { get; private set; } = null!;

    public Project(string path, string name) : this() {

        Name = name;
        Populate(path);
    }

    internal void Populate(string path) {

        Path = GetFullPath(path);

        Folder = GetFileName(path);
        Pascal = Folder.ToPascalCase();
        SafeName = Pascal.ToLowerInvariant();

        DotPath = Join(path, ".marmot");

        ResPath = Join(path, "res");
        ResGenPath = Join(DotPath, "res");
        ResMapPath = Join(ResGenPath, "map.json");
        ResHashPath = Join(DotPath, "hash.json");
        ResTargetsPath = Join(DotPath, "targets.json");

        SrcPath = Join(path, "src");
        SrcGenPath = Join(DotPath, "src");
        SrcProjPath = Join(SrcPath, $"{Pascal}.csproj");
        SrcEntryPath = Join(SrcGenPath, $"{Pascal}Entry.cs");
        SrcBuildPath = Join(SrcGenPath, "build");
        SrcBuildResPath = Join(SrcBuildPath, "res");
    }
}