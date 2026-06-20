using System.Text;
using System.Text.RegularExpressions;

namespace BetterModMenu.Data;

[Flags]
internal enum LogLevelFilter
{
    None = 0,
    Debug = 1 << 0,
    Info = 1 << 1,
    Warning = 1 << 2,
    Error = 1 << 3,
    Other = 1 << 4,
    All = Debug | Info | Warning | Error | Other
}

internal static partial class LogLevelFilterService
{
    public static string Filter(string content, LogLevelFilter includedLevels)
    {
        if (string.IsNullOrEmpty(content) || includedLevels == LogLevelFilter.All)
            return content;

        var builder = new StringBuilder(content.Length);
        string normalized = content.Replace("\r\n", "\n").Replace('\r', '\n');
        string[] lines = normalized.Split('\n');
        bool appendedLine = false;

        foreach (string line in lines)
        {
            LogLevelFilter level = Classify(line);
            if ((includedLevels & level) == 0)
                continue;

            if (appendedLine)
                builder.Append('\n');
            builder.Append(line);
            appendedLine = true;
        }

        return builder.ToString();
    }

    public static LogLevelFilter Classify(string line)
    {
        string normalized = line
            .Replace("[lb]", "[", StringComparison.OrdinalIgnoreCase)
            .Replace("[rb]", "]", StringComparison.OrdinalIgnoreCase);

        if (ContainsLevelToken(normalized, ErrorTokenRegex()))
            return LogLevelFilter.Error;
        if (ContainsLevelToken(normalized, WarningTokenRegex()))
            return LogLevelFilter.Warning;
        if (ContainsLevelToken(normalized, DebugTokenRegex()))
            return LogLevelFilter.Debug;
        if (ContainsLevelToken(normalized, InfoTokenRegex()))
            return LogLevelFilter.Info;
        if (IsErrorLine(normalized))
            return LogLevelFilter.Error;
        if (IsWarningLine(normalized))
            return LogLevelFilter.Warning;

        return LogLevelFilter.Other;
    }

    private static bool IsErrorLine(string line)
    {
        return line.Contains("WITH ERRORS", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("ERROR", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("EXCEPTION", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("FAILED", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("[ERR", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsWarningLine(string line)
    {
        return ContainsLevelToken(line, WarningTokenRegex());
    }

    private static bool ContainsLevelToken(string line, Regex regex)
    {
        return regex.IsMatch(line);
    }

    [GeneratedRegex(@"(^|[\s\[/\\])DEBUG(\]|\)|:|\s|/|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DebugTokenRegex();

    [GeneratedRegex(@"(^|[\s\[/\\])INFO(\]|\)|:|\s|/|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex InfoTokenRegex();

    [GeneratedRegex(@"(^|[\s\[/\\])WARN(?:ING)?(\]|\)|:|\s|/|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex WarningTokenRegex();

    [GeneratedRegex(@"(^|[\s\[/\\])ERR(?:OR)?(\]|\)|:|\s|/|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ErrorTokenRegex();
}
