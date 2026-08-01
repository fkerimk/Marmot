namespace Marmot;

public static class Help {

    private static void Base() {

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("Marmot");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($" v{Marmot.Version}\n");
        Console.ResetColor();
    }

    public static Exception About() {

        Base();

        return new Exception();
    }

    public static Exception Usage() {

        Base();
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("\n  usage: ");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("marmot");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("<file>");
        Console.Write("\n");
        Console.ResetColor();

        return new Exception();
    }
}