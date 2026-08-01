using System.Reflection;

namespace Marmot;

public static class Marmot {

    public static readonly string Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "Unknown Version";
}
