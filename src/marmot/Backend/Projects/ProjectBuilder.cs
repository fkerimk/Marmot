using Marmot.Backend.Resources;
using Marmot.Backend.Scripting;

namespace Marmot.Backend.Projects;

public static class ProjectBuilder {

    public static async Task Build(Project project) {

        await ResMan.Sync(project);
        await ScriptingManager.Build(project);

        // Copy resources
        Directory.CreateDirectory(project.SrcBuildResPath);

        foreach (var file in Directory.GetFiles(project.ResGenPath))
            File.Copy(file, Path.Combine(project.SrcBuildResPath, Path.GetFileName(file)), true);

        // Copy raylib
        // File.Copy(Path.Join(AppContext.BaseDirectory, "lib/libraylib.so"), Path.Combine(project.SrcBuildPath, Path.GetFileName("libraylib.so")), true);
    }
}