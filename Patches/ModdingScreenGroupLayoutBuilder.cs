using BetterModMenu.Data;

namespace BetterModMenu.Patches;

internal readonly record struct ModdingScreenGroupLayoutRow<T>(T Item, string ModId);

internal sealed class ModdingScreenGroupLayoutGroup<T>
{
    public string Name { get; init; } = string.Empty;
    public List<ModdingScreenGroupLayoutRow<T>> Rows { get; init; } = new();
    public bool IsCollapsed { get; init; }
}

internal sealed class ModdingScreenGroupLayout<T>
{
    public List<ModdingScreenGroupLayoutGroup<T>> Groups { get; init; } = new();
}

internal static class ModdingScreenGroupLayoutBuilder
{
    public static ModdingScreenGroupLayout<T> Build<T>(
        IEnumerable<ModdingScreenGroupLayoutRow<T>> rows,
        IReadOnlyDictionary<string, string> assignedGroups,
        IReadOnlyList<string> customGroups,
        IReadOnlySet<string> collapsedGroups,
        IReadOnlyDictionary<string, int> modOrder,
        string unassignedGroupName)
    {
        var orderedRows = rows
            .Where(row => !string.IsNullOrEmpty(row.ModId))
            .Select((row, index) => new OrderedRow<T>(row, index))
            .ToList();
        orderedRows.Sort((left, right) =>
        {
            int orderComparison = GetModIndex(left.Row.ModId, modOrder).CompareTo(GetModIndex(right.Row.ModId, modOrder));
            return orderComparison != 0 ? orderComparison : left.OriginalIndex.CompareTo(right.OriginalIndex);
        });

        var groups = new Dictionary<string, List<ModdingScreenGroupLayoutRow<T>>>(StringComparer.Ordinal)
        {
            [unassignedGroupName] = new()
        };

        foreach (string groupName in customGroups)
            groups[groupName] = new();

        foreach (var orderedRow in orderedRows)
        {
            string groupName = assignedGroups.TryGetValue(orderedRow.Row.ModId, out string? assignedGroup) && assignedGroup != null
                ? assignedGroup
                : unassignedGroupName;

            groups[groupName].Add(orderedRow.Row);
        }

        var groupCounts = groups.ToDictionary(entry => entry.Key, entry => entry.Value.Count, StringComparer.Ordinal);
        var visibleGroupOrder = ProfileStateRules.BuildVisibleGroupOrder(groupCounts, customGroups, unassignedGroupName);

        return new ModdingScreenGroupLayout<T>
        {
            Groups = visibleGroupOrder
                .Select(groupName => new ModdingScreenGroupLayoutGroup<T>
                {
                    Name = groupName,
                    Rows = groups[groupName],
                    IsCollapsed = collapsedGroups.Contains(groupName)
                })
                .ToList()
        };
    }

    private static int GetModIndex(string modId, IReadOnlyDictionary<string, int> modOrder)
    {
        return !string.IsNullOrEmpty(modId) && modOrder.TryGetValue(modId, out int index)
            ? index
            : int.MaxValue;
    }

    private readonly record struct OrderedRow<T>(ModdingScreenGroupLayoutRow<T> Row, int OriginalIndex);
}
