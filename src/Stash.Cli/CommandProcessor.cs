using System.Text;
using Stash.Core;

namespace Stash.Cli;

public sealed class CommandProcessor(IStore store)
{
    private const string ErrorMessage =
        "ERROR: Incorrect input submitted. Please check your value.";

    public string Execute(string? input)
    {
        try
        {
            return Run(input);
        }
        catch (ArgumentException ex)
        {
            return $"ERROR: {ex.Message}";
        }
    }

    private string Run(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return ErrorMessage;
        }

        string[] parts = input.Split(
            ' ',
            3,
            StringSplitOptions.RemoveEmptyEntries);

        return parts[0].ToLowerInvariant() switch
        {
            "get" => Get(parts),
            "put" => Put(parts),
            "del" => Delete(parts),
            "scan" => Scan(parts),
            _ => ErrorMessage
        };
    }

    private string Get(string[] parts)
    {
        if (parts.Length < 2)
        {
            return ErrorMessage;
        }

        byte[] key = Encoding.UTF8.GetBytes(parts[1]);

        return store.Get(key, out byte[] value)
            ? Encoding.UTF8.GetString(value)
            : "(nil)";
    }

    private string Put(string[] parts)
    {
        if (parts.Length < 3)
        {
            return ErrorMessage;
        }

        byte[] key = Encoding.UTF8.GetBytes(parts[1]);
        byte[] value = Encoding.UTF8.GetBytes(parts[2]);

        store.Put(key, value);

        return "OK, 1 row affected";
    }

    private string Delete(string[] parts)
    {
        if (parts.Length < 2)
        {
            return ErrorMessage;
        }

        byte[] key = Encoding.UTF8.GetBytes(parts[1]);

        bool deleted = store.Delete(key);

        return deleted
            ? "OK, 1 row affected"
            : "OK, 0 rows affected";
    }

    private string Scan(string[] parts)
    {
        if (parts.Length < 3)
        {
            return ErrorMessage;
        }

        byte[] from = Encoding.UTF8.GetBytes(parts[1]);
        byte[] to = Encoding.UTF8.GetBytes(parts[2]);

        StringBuilder result = new();
        int count = 0;

        foreach (var pair in store.Scan(from, to))
        {
            result.AppendLine(
                $"{Encoding.UTF8.GetString(pair.Key)} = " +
                $"{Encoding.UTF8.GetString(pair.Value)}");

            count++;
        }

        result.Append($"OK, {count} row{(count == 1 ? "" : "s")} returned");

        return result.ToString();
    }
}