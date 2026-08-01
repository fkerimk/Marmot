using System.Data;
using System.Diagnostics;
using System.Text.Json;
using Marmot.Backend.Resources;
using static System.Text.RegularExpressions.Regex;

namespace Marmot.Backend.Projects;

public static class ProjectManager {

    // Private
    private static string FindProjectsPath()
        => PathM.SearchPath(AppContext.BaseDirectory, "projects", 4)
           ?? throw new DirectoryNotFoundException("Projects folder not found");

    private static Project GetProject(string path) {

        path = Path.GetFullPath(path);

        var jsonPath = Path.Join(path, "project.json");

        if (!File.Exists(jsonPath)) throw new FileNotFoundException("Project file not found", jsonPath);

        var json = File.ReadAllText(jsonPath);
        var project = JsonSerializer.Deserialize<Project>(json, JsonContext.Default.Project) ?? throw new InvalidOperationException("Invalid project file");

        project.Populate(path);

        return project;
    }

    private static string GetProjectPath(string path)
        => Path.Join(FindProjectsPath(), path);

    // Public
    public static Project[] GetProjects()
        => Directory.GetDirectories(FindProjectsPath(), "*", SearchOption.TopDirectoryOnly).Select(GetProject).ToArray();

    public static Project FindProject(string project)
        => project.All(char.IsDigit) ? GetProjects()[int.Parse(project)] : GetProject(GetProjectPath(project));

    public static async Task Create(string name) {

        var safeName = Replace(name.ToLowerInvariant().Replace(' ', '-'), @"[^a-zA-Z0-9\-_]", "");
        var path = Path.GetFullPath(Path.Join(FindProjectsPath(), safeName));

        Console.WriteLine(path);

        if (Directory.Exists(path)) throw new DuplicateNameException("Project already exists");

        // Project
        Directory.CreateDirectory(path);
        var project = new Project(path, name);

        // Project json
        var json = JsonSerializer.Serialize(project, JsonContext.Default.Project);
        await File.WriteAllTextAsync(Path.Join(path, "project.json"), json);

        // Generate project files
        await ResourceManager.Sync(project);
        await Scripting.GenerateSource(project);
    }

    public static async Task Run(Project project) {

        await ResourceManager.Sync(project);
        await Scripting.Build(project, "run", false);
    }
}