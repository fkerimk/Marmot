namespace Marmot;

internal static class PathM {

    internal static string BasePath => AppContext.BaseDirectory;
    internal static string GetBasePath(string relativePath) => Path.Join(BasePath, relativePath);

    internal static string LibPath => GetBasePath("lib");
    internal static string GetLibPath(string relativePath) => Path.Join(LibPath, relativePath);

    internal static string PyPath => GetLibPath("py");
    internal static string GetPyPath(string relativePath) => Path.Join(PyPath, relativePath + ".py");

    internal static string? SearchPath(string basePath, string search, int depth) {

        var current = basePath;

        for (var i = 0; i < depth; i++) {

            if (string.IsNullOrEmpty(current)) break;

            var path = Path.Join(current, search);

            if (File.Exists(path) || Directory.Exists(path))
                return path;

            current = Directory.GetParent(current)?.FullName;
        }

        return null;
    }
}