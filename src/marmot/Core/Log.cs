namespace Marmot;

public static class Log {

    private static void PrintColor(string message, ConsoleColor? color = null) {

        if (color.HasValue) Console.ForegroundColor = color.Value;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public static void Info(string message) => PrintColor($"Info: {message}", ConsoleColor.Blue);
    public static void Warn(string message) => PrintColor($"Warn: {message}", ConsoleColor.Yellow);
    public static void Pass(string message) => PrintColor($"Pass: {message}", ConsoleColor.Green);
    public static void Fail(string message) => PrintColor($"Fail: {message}", ConsoleColor.Red);

    public static Exception FailException (string message) {

        Fail(message);
        return new Exception();
    }

    public static Exception ComponentException<T>(int id) => throw FailException($"{typeof(T).Name} with id {id} does not exist");
    public static Exception InvalidJsonException(string path) => throw FailException($"Invalid json file: {path}");
    public static Exception FileException(string path) => throw FailException($"File does not exist or invalid: {path}");
    public static Exception MaterialException() => throw FailException("Material doesn't exist");
}