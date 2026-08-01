namespace Marmot;

public static class PathM {

    public static string? SearchPath(string basePath, string search, int depth) {

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