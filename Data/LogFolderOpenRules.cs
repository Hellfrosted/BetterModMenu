namespace BetterModMenu.Data;

using System.Diagnostics;
using System.Runtime.InteropServices;

internal enum FolderOpenPlatform
{
    Windows,
    MacOS,
    Linux,
    Other
}

internal sealed class FolderOpenCommand
{
    public string Executable { get; init; } = string.Empty;
    public IReadOnlyList<string> Arguments { get; init; } = Array.Empty<string>();
}

internal static class LogFolderOpenRules
{
    public const string ErrorNoLogFilePath = "No log file path is available.";
    public const string ErrorLogFileNoLongerExists = "Log file no longer exists.";
    public const string ErrorLogFolderNoLongerExists = "Log folder no longer exists.";
    public const string ErrorNoFolderPath = "No folder path is available.";
    public const string ErrorFileManagerDidNotStart = "The operating system did not start a file manager.";

    public static bool TryGetContainingDirectory(string logPath, out string directory, out string? error)
    {
        directory = string.Empty;
        error = null;

        if (string.IsNullOrWhiteSpace(logPath))
        {
            error = ErrorNoLogFilePath;
            return false;
        }

        if (!File.Exists(logPath))
        {
            error = ErrorLogFileNoLongerExists;
            return false;
        }

        string? parentDirectory = Path.GetDirectoryName(Path.GetFullPath(logPath));
        if (string.IsNullOrWhiteSpace(parentDirectory) || !Directory.Exists(parentDirectory))
        {
            error = ErrorLogFolderNoLongerExists;
            return false;
        }

        directory = parentDirectory;
        return true;
    }

    public static IReadOnlyList<FolderOpenCommand> BuildOpenFolderCommands(string directory, FolderOpenPlatform platform)
    {
        if (string.IsNullOrWhiteSpace(directory))
            return Array.Empty<FolderOpenCommand>();

        return platform switch
        {
            FolderOpenPlatform.Windows =>
            [
                new FolderOpenCommand { Executable = "explorer.exe", Arguments = [directory] }
            ],
            FolderOpenPlatform.MacOS =>
            [
                new FolderOpenCommand { Executable = "open", Arguments = [directory] }
            ],
            _ => BuildLinuxLikeOpenFolderCommands(directory)
        };
    }

    public static bool TryOpenDirectory(string directory, out string? error)
    {
        error = null;
        IReadOnlyList<FolderOpenCommand> commands = BuildOpenFolderCommands(directory, GetCurrentPlatform());
        if (commands.Count == 0)
        {
            error = ErrorNoFolderPath;
            return false;
        }

        foreach (var command in commands)
        {
            if (TryStart(command, out error))
                return true;
        }

        return false;
    }

    private static IReadOnlyList<FolderOpenCommand> BuildLinuxLikeOpenFolderCommands(string directory)
    {
        string directoryUri = new Uri(AppendDirectorySeparator(directory)).AbsoluteUri;
        return
        [
            new FolderOpenCommand { Executable = "xdg-open", Arguments = [directoryUri] },
            new FolderOpenCommand { Executable = "gio", Arguments = ["open", directoryUri] },
            new FolderOpenCommand { Executable = "kde-open5", Arguments = [directoryUri] },
            new FolderOpenCommand { Executable = "kde-open", Arguments = [directoryUri] },
            new FolderOpenCommand { Executable = "exo-open", Arguments = [directoryUri] }
        ];
    }

    private static string AppendDirectorySeparator(string directory)
    {
        return directory.EndsWith(Path.DirectorySeparatorChar) || directory.EndsWith(Path.AltDirectorySeparatorChar)
            ? directory
            : directory + Path.DirectorySeparatorChar;
    }

    private static bool TryStart(FolderOpenCommand command, out string? error)
    {
        error = null;

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo(command.Executable)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            foreach (string argument in command.Arguments)
                process.StartInfo.ArgumentList.Add(argument);

            if (process.Start())
                return true;

            error = ErrorFileManagerDidNotStart;
            return false;
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            error = ex.Message;
            return false;
        }
    }

    private static FolderOpenPlatform GetCurrentPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return FolderOpenPlatform.Windows;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return FolderOpenPlatform.MacOS;

        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            ? FolderOpenPlatform.Linux
            : FolderOpenPlatform.Other;
    }
}
