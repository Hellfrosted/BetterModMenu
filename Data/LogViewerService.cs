using System.Text;

namespace BetterModMenu.Data;

internal static class LogViewerService
{
    public const int DefaultMaxLines = 5000;
    public const int DefaultMaxChars = 500000;
    public const string EmptyLogContent = "(Log file is empty.)";
    public const string ErrorNoLogFileFound = "No log file was found in the known Better Mod Menu log locations.";
    public const string ErrorLogFileDoesNotExist = "Log file does not exist.";
    private static readonly string[] KnownLogFileNames =
    [
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
            error = ErrorNoLogFileFound;
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
                error = ErrorLogFileDoesNotExist;
                return false;
            }

            int lineCount = Math.Max(1, maxLines);
            int charCount = Math.Max(1, maxChars);
            string text = ReadTailShared(path, lineCount, charCount);
            if (text.Length > charCount)
                text = text[^charCount..];

            content = string.IsNullOrEmpty(text) ? EmptyLogContent : text;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static string ReadTailShared(string path, int maxLines, int maxChars)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        if (stream.Length == 0)
            return string.Empty;

        int readLimit = Math.Max(4096, maxChars * 4);
        int targetNewlines = Math.Max(0, maxLines - 1);
        var chunks = new List<byte[]>();
        var buffer = new byte[4096];
        long position = stream.Length;
        int bytesRead = 0;
        int newlineCount = 0;

        while (position > 0 && bytesRead < readLimit && (targetNewlines == 0 || newlineCount < targetNewlines))
        {
            int readSize = (int)Math.Min(Math.Min(buffer.Length, position), readLimit - bytesRead);
            position -= readSize;
            stream.Position = position;
            int count = stream.Read(buffer, 0, readSize);
            var chunk = new byte[count];
            Array.Copy(buffer, chunk, count);
            chunks.Insert(0, chunk);
            bytesRead += count;

            for (int i = 0; i < count; i++)
            {
                if (chunk[i] == (byte)'\n')
                    newlineCount++;
            }
        }

        var selectedBytes = new byte[bytesRead];
        int offset = 0;
        foreach (byte[] chunk in chunks)
        {
            Buffer.BlockCopy(chunk, 0, selectedBytes, offset, chunk.Length);
            offset += chunk.Length;
        }

        string decoded = Encoding.UTF8.GetString(selectedBytes);
        string normalized = decoded.Replace("\r\n", "\n").Replace('\r', '\n');
        if (normalized.EndsWith('\n'))
            normalized = normalized[..^1];

        string[] lines = normalized.Split('\n');
        var selectedLines = lines.Skip(Math.Max(0, lines.Length - maxLines));

        return string.Join(Environment.NewLine, selectedLines);
    }
}
