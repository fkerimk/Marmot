namespace Marmot;

public static class Log {

    private static void PrintColor(string message, ConsoleColor? color = null) {

        if (color.HasValue) Console.ForegroundColor = color.Value;
        Console.WriteLine(message);
        if (color.HasValue) Console.ResetColor();
    }

    public static void Warning(string message) {

        PrintColor($"Warning: {message}", ConsoleColor.Yellow);
    }
}