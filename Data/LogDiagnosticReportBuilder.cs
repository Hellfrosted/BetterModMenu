namespace BetterModMenu.Data;

internal static class LogDiagnosticReportBuilder
{
    public static string Build(
        string modVersion,
        string logPath,
        string operatingSystem,
        string runtime,
        string logContent)
    {
        return string.Join(
            Environment.NewLine,
            "Better Mod Menu diagnostic report",
            $"Better Mod Menu version: {ValueOrUnknown(modVersion)}",
            $"Operating system: {ValueOrUnknown(operatingSystem)}",
            $"Runtime: {ValueOrUnknown(runtime)}",
            $"Log file: {ValueOrUnknown(logPath)}",
            $"Created (UTC): {DateTime.UtcNow:O}",
            string.Empty,
            "Log tail:",
            logContent ?? string.Empty);
    }

    private static string ValueOrUnknown(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "unknown" : value;
    }
}
