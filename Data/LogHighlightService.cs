using System.Text;
using System.Text.RegularExpressions;

namespace BetterModMenu.Data;

internal static partial class LogHighlightService
{
    public static string BuildHighlightedBbCode(string content)
    {
        if (string.IsNullOrEmpty(content))
            return string.Empty;

        var builder = new StringBuilder(content.Length + 256);
        string normalized = content.Replace("\r\n", "\n").Replace('\r', '\n');
        string[] lines = normalized.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0)
                builder.Append('\n');

            AppendHighlightedLine(builder, lines[i]);
        }

        return builder.ToString();
    }

    private static void AppendHighlightedLine(StringBuilder builder, string line)
    {
        Match manifestMatch = ManifestMigrationWarningRegex().Match(line);
        if (manifestMatch.Success)
        {
            AppendWithHighlightedGroup(builder, line, manifestMatch, "mod", "ff5a4e");
            return;
        }

        Match dllMatch = DllInitializationWarningRegex().Match(line);
        if (dllMatch.Success)
        {
            AppendWithHighlightedGroup(builder, line, dllMatch, "mod", "ff4d4d");
            return;
        }

        if (line.Contains("WITH ERRORS", StringComparison.OrdinalIgnoreCase))
        {
            builder.Append("[color=ff4040][b]");
            builder.Append(EscapeBbCode(line));
            builder.Append("[/b][/color]");
            return;
        }

        builder.Append(EscapeBbCode(line));
    }

    private static void AppendWithHighlightedGroup(StringBuilder builder, string line, Match match, string groupName, string color)
    {
        Group group = match.Groups[groupName];
        builder.Append(EscapeBbCode(line[..group.Index]));
        builder.Append("[color=");
        builder.Append(color);
        builder.Append("][b]");
        builder.Append(EscapeBbCode(group.Value));
        builder.Append("[/b][/color]");
        builder.Append(EscapeBbCode(line[(group.Index + group.Length)..]));
    }

    private static string EscapeBbCode(string text)
    {
        return text.Replace("[", "[lb]").Replace("]", "[rb]");
    }

    [GeneratedRegex(@"Mod\s+(?<mod>\S+)\s+has a mod manifest that should be migrated!", RegexOptions.CultureInvariant)]
    private static partial Regex ManifestMigrationWarningRegex();

    [GeneratedRegex(@"Assembly DLL for mod\s+(?<mod>\S+)\s+failed to initialize!", RegexOptions.CultureInvariant)]
    private static partial Regex DllInitializationWarningRegex();
}
