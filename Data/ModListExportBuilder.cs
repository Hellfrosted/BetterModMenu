using System.Text;

namespace BetterModMenu.Data;

internal sealed class InstalledModExportInput
{
    public string ModId { get; init; } = string.Empty;
    public bool Enabled { get; init; }
    public string ManifestPath { get; init; } = string.Empty;
}

internal sealed class ModListExportRow
{
    public string ModId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Link { get; init; } = string.Empty;
    public bool Enabled { get; init; }
    public string Group { get; init; } = string.Empty;
}

internal static class ModListExportBuilder
{
    private static readonly string[] Header = ["Mod Id", "Name", "Version", "Link", "Enabled", "Group"];

    public static List<ModListExportRow> BuildRows(
        IEnumerable<InstalledModExportInput> mods,
        IReadOnlyDictionary<string, string> assignedGroups,
        string unassignedGroup)
    {
        return mods
            .Select(mod => BuildRow(mod, assignedGroups, unassignedGroup))
            .OrderBy(row => row.Group, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => string.IsNullOrWhiteSpace(row.Name) ? row.ModId : row.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string BuildCsv(IEnumerable<ModListExportRow> rows)
    {
        var builder = new StringBuilder();
        AppendCsvRow(builder, Header);

        foreach (var row in rows)
        {
            AppendCsvRow(builder,
                row.ModId,
                row.Name,
                row.Version,
                row.Link,
                row.Enabled ? "TRUE" : "FALSE",
                row.Group);
        }

        return builder.ToString();
    }

    public static bool TryWriteCsv(string directory, IEnumerable<ModListExportRow> rows, DateTimeOffset timestamp, out string exportPath, out string? error)
    {
        exportPath = string.Empty;
        error = null;

        try
        {
            if (string.IsNullOrWhiteSpace(directory))
                throw new InvalidOperationException("No export directory was provided.");

            Directory.CreateDirectory(directory);
            exportPath = GetUniqueExportPath(directory, timestamp);
            File.WriteAllText(exportPath, BuildCsv(rows), Encoding.UTF8);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            exportPath = string.Empty;
            return false;
        }
    }

    public static string BuildExportFileName(DateTimeOffset timestamp)
    {
        return $"mod_list.{timestamp.UtcDateTime:yyyyMMdd-HHmmss}.csv";
    }

    private static ModListExportRow BuildRow(
        InstalledModExportInput mod,
        IReadOnlyDictionary<string, string> assignedGroups,
        string unassignedGroup)
    {
        ModManifestInfo manifestInfo = new();
        if (!string.IsNullOrWhiteSpace(mod.ManifestPath) && File.Exists(mod.ManifestPath))
            ManifestScanner.TryReadManifestInfo(mod.ManifestPath, mod.ModId, out manifestInfo);

        string modId = string.IsNullOrWhiteSpace(manifestInfo.Id) ? mod.ModId : manifestInfo.Id;
        return new ModListExportRow
        {
            ModId = modId,
            Name = manifestInfo.Name,
            Version = manifestInfo.Version,
            Link = manifestInfo.Link,
            Enabled = mod.Enabled,
            Group = assignedGroups.TryGetValue(modId, out string? group) && !string.IsNullOrWhiteSpace(group)
                ? group
                : unassignedGroup
        };
    }

    private static void AppendCsvRow(StringBuilder builder, params string[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (i > 0)
                builder.Append(',');

            AppendCsvValue(builder, values[i]);
        }

        builder.AppendLine();
    }

    private static void AppendCsvValue(StringBuilder builder, string value)
    {
        string safeValue = NeutralizeFormulaPrefix(value);
        if (!RequiresEscaping(safeValue))
        {
            builder.Append(safeValue);
            return;
        }

        builder.Append('"');
        builder.Append(safeValue.Replace("\"", "\"\""));
        builder.Append('"');
    }

    private static string NeutralizeFormulaPrefix(string value)
    {
        return value.Length > 0 && IsFormulaPrefix(value[0]) ? "'" + value : value;
    }

    private static bool IsFormulaPrefix(char value)
    {
        return value is '=' or '+' or '-' or '@' or '\t' or '\r';
    }

    private static bool RequiresEscaping(string value)
    {
        return value.Contains(',') || value.Contains('"') || value.Contains('\r') || value.Contains('\n');
    }

    private static string GetUniqueExportPath(string directory, DateTimeOffset timestamp)
    {
        string fileName = BuildExportFileName(timestamp);
        string candidate = Path.Combine(directory, fileName);
        if (!File.Exists(candidate))
            return candidate;

        string baseName = Path.GetFileNameWithoutExtension(fileName);
        for (int i = 2; ; i++)
        {
            candidate = Path.Combine(directory, $"{baseName}-{i}.csv");
            if (!File.Exists(candidate))
                return candidate;
        }
    }
}
