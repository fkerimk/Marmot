using System.Reflection;

using Marmot.Backend.Projects;
using static Marmot.Backend.Projects.ProjectManager;

namespace Marmot;

internal static class Cli {

    private static string FixedArg(string arg) => arg.TrimStart('"').TrimEnd('"').Trim();
    private static Project GetProject(string name) => FindProject(FixedArg(name));

    private static PropertyInfo? GetProp<T>(string name)
        => typeof(T).GetProperty(name, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

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

                case ["project", var name, "run"]: { await Run(GetProject(name)); break; }

                case ["project", var name, "build"]: { await ProjectBuilder.Build(GetProject(name)); break; }

                case ["project", var name, "get", var field]: {

                    var prop = typeof(Project).GetProperty(field, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    Console.WriteLine(prop?.GetValue(GetProject(name)) ?? throw new InvalidOperationException($"There is no {field} in {name}"));

                    break;
                }
            }

        } catch (Exception e) {

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(e.Message);

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(e.StackTrace);

            Console.ResetColor();

            Environment.Exit(1);
        }
    }
}