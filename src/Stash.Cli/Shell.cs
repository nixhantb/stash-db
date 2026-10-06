namespace Stash.Cli;

using Stash.Core;

public sealed class Shell
{
    public static void Start(IStore store)
    {
        var commandProcessor = new CommandProcessor(store);

        Console.Clear();

        Console.WriteLine("""
        StashDB v0.1.0
        Light weight KV Store
        """);


        while (true)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write("stash");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("> ");
            Console.ResetColor();

            string? input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            if (input.Equals("q", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("quit", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine();
                Console.WriteLine("Goodbye!");
                break;
            }

            if (input.Equals("clear", StringComparison.OrdinalIgnoreCase))
            {
                Console.Clear();
                continue;
            }


            string result = commandProcessor.Execute(input);

            if (!string.IsNullOrWhiteSpace(result))
            {
                Console.WriteLine(result);
            }
        }
    }
}
