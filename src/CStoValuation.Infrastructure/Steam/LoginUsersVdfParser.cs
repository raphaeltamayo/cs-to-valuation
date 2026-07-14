using System.Text;
using CStoValuation.Core.Models;

namespace CStoValuation.Infrastructure.Steam;

internal static class LoginUsersVdfParser
{
    public static IReadOnlyList<LocalSteamAccount> Parse(string content)
    {
        var tokens = Tokenize(content);
        var pos = 0;
        var root = ParseObject(tokens, ref pos);

        if (!root.TryGetValue("users", out var usersValue) || usersValue is not Dictionary<string, object> users)
        {
            return [];
        }

        var accounts = new List<LocalSteamAccount>(users.Count);
        foreach (var (steamId64, value) in users)
        {
            if (value is not Dictionary<string, object> fields)
            {
                continue;
            }

            accounts.Add(new LocalSteamAccount(
                SteamId64: steamId64,
                PersonaName: GetString(fields, "PersonaName"),
                IsActive: false,
                IsMostRecent: GetString(fields, "MostRecent") == "1",
                Timestamp: long.TryParse(GetString(fields, "Timestamp"), out var timestamp) ? timestamp : 0));
        }

        return accounts;
    }

    private static string? GetString(Dictionary<string, object> fields, string key) =>
        fields.TryGetValue(key, out var value) && value is string text ? text : null;

    private static Dictionary<string, object> ParseObject(IReadOnlyList<string> tokens, ref int pos)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        while (pos < tokens.Count && tokens[pos] != "}")
        {
            var key = tokens[pos++];
            if (pos >= tokens.Count)
            {
                break;
            }

            if (tokens[pos] == "{")
            {
                pos++;
                var child = ParseObject(tokens, ref pos);
                if (pos < tokens.Count && tokens[pos] == "}")
                {
                    pos++;
                }

                result[key] = child;
            }
            else
            {
                result[key] = tokens[pos++];
            }
        }

        return result;
    }

    private static List<string> Tokenize(string content)
    {
        var tokens = new List<string>();
        var i = 0;
        while (i < content.Length)
        {
            var c = content[i];

            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if (c == '/' && i + 1 < content.Length && content[i + 1] == '/')
            {
                while (i < content.Length && content[i] != '\n')
                {
                    i++;
                }

                continue;
            }

            if (c is '{' or '}')
            {
                tokens.Add(c.ToString());
                i++;
                continue;
            }

            if (c == '"')
            {
                i++;
                var builder = new StringBuilder();
                while (i < content.Length && content[i] != '"')
                {
                    if (content[i] == '\\' && i + 1 < content.Length)
                    {
                        i++;
                    }

                    builder.Append(content[i]);
                    i++;
                }

                i++;
                tokens.Add(builder.ToString());
                continue;
            }

            i++;
        }

        return tokens;
    }
}
