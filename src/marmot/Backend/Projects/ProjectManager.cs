using System.Data;
using Newtonsoft.Json;
using static System.Text.RegularExpressions.Regex;

using Marmot.Backend.Resources;
using Marmot.Backend.Scripting;

namespace Marmot.Backend.Projects;

public static class ProjectManager {

    // Private
    private static string FindProjectsPath() =>
        PathM.SearchPath(AppContext.BaseDirectory, "projects", 4)
        ?? throw new DirectoryNotFoundException("Projects folder not found");

    private static Project GetProject(string path) {

        path = Path.GetFullPath(path);

        var jsonPath = Path.Join(path, "project.json");

        if (!File.Exists(jsonPath)) throw Log.FileException(jsonPath);

        var json = File.ReadAllText(jsonPath);
        var project = JsonConvert.DeserializeObject<Project>(json);
        project.Path = path;

        return project;
    }

    private static string GetProjectPath(string path) => Path.Join(FindProjectsPath(), path);

    // Public
    public static Project[] GetProjects() =>
        Directory.GetDirectories(FindProjectsPath(), "*", SearchOption.TopDirectoryOnly).Select(GetProject).ToArray();

    public static Project FindProject(string project) =>
        project.All(char.IsDigit) ? GetProjects()[int.Parse(project)] : GetProject(GetProjectPath(project));

    public static async Task Create(string name) {

        var safeName = Replace(name.ToLowerInvariant().Replace(' ', '-'), @"[^a-zA-Z0-9\-_]", "");
        var path = Path.GetFullPath(Path.Join(FindProjectsPath(), safeName));

        Console.WriteLine(path);

        if (Directory.Exists(path)) throw new DuplicateNameException("Project already exists");

        // Project
        Directory.CreateDirectory(path);
        var project = new Project {

            Name = name,
            Path = path
        };

        // Project json
        var json = JsonConvert.SerializeObject(project, Formatting.Indented);
        await File.WriteAllTextAsync(Path.Join(path, "project.json"), json);

        // Generate project files
        await ResMan.Sync(project);
        await ScriptingManager.GenerateSource(project);
    }

    public static async Task Run(Project project) {

        await ResMan.Sync(project);
        await ScriptingManager.Build(project, "run", false);
    }
}