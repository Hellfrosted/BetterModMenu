using System.Text;

namespace BetterModMenu.Data;

internal static class LogViewerService
{
    public const int DefaultMaxLines = 5000;
    public const int DefaultMaxChars = 500000;
    private static readonly string[] KnownLogFileNames =
    [
        "TTSMM.log",
        "ttsmm.log",
        "BetterModMenu.log",
        "bettermodmenu.log",
        "godot.log",
        "Godot.log",
        "player.log",
        "Player.log",
        "latest.log",
        "log.txt"
    ];

    private static readonly string[] KnownLogSubdirectories =
    [
        "",
        "logs",
        "Logs",
        "log",
        "Log"
    ];

    public static IReadOnlyList<string> BuildCandidatePaths(IEnumerable<string?> baseDirectories)
    {
        var paths = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string? baseDirectory in baseDirectories)
        {
            if (string.IsNullOrWhiteSpace(baseDirectory))
                continue;

            foreach (string directoryBase in EnumerateBaseAndAncestors(baseDirectory, maxAncestorCount: 6))
                AddKnownLogPaths(directoryBase, paths, seen);
        }

        return paths;
    }

    private static IEnumerable<string> EnumerateBaseAndAncestors(string baseDirectory, int maxAncestorCount)
    {
        var directory = new DirectoryInfo(Path.GetFullPath(baseDirectory));
        for (int i = 0; i <= maxAncestorCount && directory != null; i++)
        {
            yield return directory.FullName;
            directory = directory.Parent;
        }
    }

    private static void AddKnownLogPaths(string baseDirectory, List<string> paths, HashSet<string> seen)
    {
        foreach (string subdirectory in KnownLogSubdirectories)
        {
            string directory = string.IsNullOrEmpty(subdirectory)
                ? baseDirectory
                : Path.Combine(baseDirectory, subdirectory);

            foreach (string fileName in KnownLogFileNames)
            {
                string candidate = Path.Combine(directory, fileName);
                if (seen.Add(candidate))
                    paths.Add(candidate);
            }
        }
    }

    public static bool TryReadLatestLog(
        IEnumerable<string> candidatePaths,
        int maxLines,
        int maxChars,
        out string title,
        out string content,
        out string logPath,
        out string? error)
    {
        title = "Logs";
        content = string.Empty;
        logPath = string.Empty;
        error = null;

        string? path = candidatePaths.FirstOrDefault(File.Exists);
        if (string.IsNullOrWhiteSpace(path))
        {
            error = "No log file was found in the known BetterModMenu/TTSMM locations.";
            return false;
        }

        title = Path.GetFileName(path);
        logPath = path;
        return TryReadTail(path, maxLines, maxChars, out content, out error);
    }

    public static bool TryReadLatestLog(
        IEnumerable<string> candidatePaths,
        int maxLines,
        int maxChars,
        out string title,
        out string content,
        out string? error)
    {
        return TryReadLatestLog(candidatePaths, maxLines, maxChars, out title, out content, out _, out error);
    }

    public static bool TryReadTail(string path, int maxLines, int maxChars, out string content, out string? error)
    {
        content = string.Empty;
        error = null;

        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                error = "Log file does not exist.";
                return false;
            }

            string[] lines = ReadAllLinesShared(path);
            int lineCount = Math.Max(1, maxLines);
            var selectedLines = lines.Skip(Math.Max(0, lines.Length - lineCount));
            string text = string.Join(Environment.NewLine, selectedLines);

            int charCount = Math.Max(1, maxChars);
            if (text.Length > charCount)
                text = text[^charCount..];

            content = string.IsNullOrEmpty(text) ? "(Log file is empty.)" : text;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static string[] ReadAllLinesShared(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var lines = new List<string>();
        while (reader.ReadLine() is { } line)
            lines.Add(line);

        return lines.ToArray();
    }
}
