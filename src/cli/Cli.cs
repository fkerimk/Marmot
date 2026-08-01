using Marmot.Backend.Projects;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using static Marmot.Backend.Projects.ProjectManager;
using static Marmot.Backend.Resources.ResourceManager;

namespace Marmot;

internal static class Cli {

    private static string FixedArg(string arg) => arg.TrimStart('"').TrimEnd('"').Trim();

    private static PropertyInfo? GetProp<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] T>(string name)
        => typeof(T).GetProperty(name, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);

    public static async Task Main(string[] args) {

        try {

            switch (args) {

                case ["about", ..]: Help.About(); break;

                case ["project", "list", var field, ..]: {

                    var prop = GetProp<Project>(field);

                    foreach (var project in GetProjects())
                        Console.WriteLine(prop?.GetValue(project) ?? throw new InvalidOperationException($"There is no {field} in {project.Folder}"));

                    break;
                }

                case ["project", "list", ..]:

                    var projects = GetProjects();

                    for (var i = 0; i < projects.Length; i++)
                        Console.WriteLine($"{i}: {projects[i].Name} ({projects[i].Folder})");

                    break;

                case ["project", "create", var name]: { await Create(name); break; }

                case ["project", var name, "run"]: { await Run(FindProject(FixedArg(name))); break; }

                case ["project", var name, "build"]: { await ProjectBuilder.Build(FindProject(FixedArg(name))); break; }

                case ["project", var name, "get", var field]: {

                    var prop = typeof(Project).GetProperty(field, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    Console.WriteLine(prop?.GetValue(FindProject(FixedArg(name))) ?? throw new InvalidOperationException($"There is no {field} in {name}"));

                    break;
                }

                case ["project", var name, "sync"]: { await Sync(FindProject(FixedArg(name))); break; }
            }

        } catch (Exception e) {

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(e.Message);
            Console.ResetColor();

            Environment.Exit(1);
        }
    }
}