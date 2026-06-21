namespace BetterModMenu.Data;

internal static class ModNameStyleCatalog
{
    private static readonly Dictionary<string, string> DefaultTagFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        ["<none selected>"] = "[color=#8e99a6]{name}[/color]",
        ["Acts"] = "[color=#ffb257]{name}[/color]",
        ["Ancients"] = "[color=#dfd0a8]{name}[/color]",
        ["Audio"] = "[color=#9edcff]{name}[/color]",
        ["Cards"] = "[color=#32d4ff]{name}[/color]",
        ["Characters"] = "[color=#ff5ec7]{name}[/color]",
        ["Cosmetics"] = "[color=#f1a6ff]{name}[/color]",
        ["Events"] = "[color=#ff7a35]{name}[/color]",
        ["Expansion"] = "[color=#ff7894]{name}[/color]",
        ["Extensions"] = "[color=#5fd6a1]{name}[/color]",
        ["Humor"] = "[color=#ffe066]{name}[/color]",
        ["Modifiers"] = "[color=#a47cff]{name}[/color]",
        ["Monsters"] = "[color=#ff4d3d]{name}[/color]",
        ["Potions"] = "[color=#32e1ca]{name}[/color]",
        ["QoL"] = "[color=#b3ed5e]{name}[/color]",
        ["Relics"] = "[color=#c99638]{name}[/color]",
        ["Rooms"] = "[color=#82bd5c]{name}[/color]",
        ["Tools & APIs"] = "[color=#74a6ff]{name}[/color]",
        ["Utility"] = "[color=#eec46d]{name}[/color]",
        ["Misc"] = "[color=#b8bec6]{name}[/color]"
    };

    private static readonly string[] DefaultTagPriority =
    {
        "Tools & APIs",
        "Extensions",
        "Utility",
        "QoL",
        "Expansion",
        "Acts",
        "Characters",
        "Cards",
        "Relics",
        "Potions",
        "Monsters",
        "Events",
        "Rooms",
        "Modifiers",
        "Ancients",
        "Audio",
        "Cosmetics",
        "Humor",
        "Misc",
        "<none selected>"
    };

    private static readonly HashSet<string> SupportedWorkshopTags = new(DefaultTagFormats.Keys, StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, string> WorkshopTagAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["none selected"] = "<none selected>",
        ["<none>"] = "<none selected>",
        ["none"] = "<none selected>",
        ["no tag"] = "<none selected>",
        ["no tags"] = "<none selected>",
        ["Act"] = "Acts",
        ["Ancient"] = "Ancients",
        ["Card"] = "Cards",
        ["Character"] = "Characters",
        ["Cosmetic"] = "Cosmetics",
        ["Event"] = "Events",
        ["Expansions"] = "Expansion",
        ["Extension"] = "Extensions",
        ["Humour"] = "Humor",
        ["Modifier"] = "Modifiers",
        ["Monster"] = "Monsters",
        ["Potion"] = "Potions",
        ["Quality of Life"] = "QoL",
        ["Quality-of-Life"] = "QoL",
        ["Quality Of Life"] = "QoL",
        ["Q.O.L."] = "QoL",
        ["Relic"] = "Relics",
        ["Room"] = "Rooms",
        ["Tool"] = "Tools & APIs",
        ["Tools"] = "Tools & APIs",
        ["API"] = "Tools & APIs",
        ["APIs"] = "Tools & APIs",
        ["Tools & API"] = "Tools & APIs",
        ["Tool & API"] = "Tools & APIs",
        ["Tool & APIs"] = "Tools & APIs",
        ["Tools and APIs"] = "Tools & APIs",
        ["Tools and API"] = "Tools & APIs",
        ["Tool and API"] = "Tools & APIs",
        ["Tool and APIs"] = "Tools & APIs",
        ["Utilities"] = "Utility",
        ["Miscellaneous"] = "Misc"
    };

    public static IReadOnlyDictionary<string, string> GetDefaultTagFormats()
    {
        return DefaultTagFormats;
    }

    public static Dictionary<string, string> CreateDefaultTagFormatLookup()
    {
        return new Dictionary<string, string>(DefaultTagFormats, StringComparer.OrdinalIgnoreCase);
    }

    public static string[] BuildTagPriority(IEnumerable<string>? customPriority)
    {
        var priority = new List<string>();
        foreach (string tag in customPriority ?? Enumerable.Empty<string>())
        {
            if (TryGetSupportedTagName(tag, out string supportedTag) &&
                !priority.Contains(supportedTag, StringComparer.OrdinalIgnoreCase))
            {
                priority.Add(supportedTag);
            }
        }

        foreach (string tag in DefaultTagPriority)
        {
            if (!priority.Contains(tag, StringComparer.OrdinalIgnoreCase))
                priority.Add(tag);
        }

        return priority.ToArray();
    }

    public static HashSet<string> BuildSupportedSet(IEnumerable<string>? tags)
    {
        var supportedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string tag in tags ?? Enumerable.Empty<string>())
        {
            if (TryGetSupportedTagName(tag, out string supportedTag))
                supportedTags.Add(supportedTag);
        }

        return supportedTags;
    }

    public static bool TryGetSupportedTagName(string tag, out string supportedTag)
    {
        supportedTag = string.Empty;
        string normalizedTag = tag.Trim();
        if (SupportedWorkshopTags.TryGetValue(normalizedTag, out string? directMatch) && directMatch != null)
        {
            supportedTag = directMatch;
            return true;
        }

        if (WorkshopTagAliases.TryGetValue(normalizedTag, out string? alias) &&
            SupportedWorkshopTags.TryGetValue(alias, out string? canonicalMatch) &&
            canonicalMatch != null)
        {
            supportedTag = canonicalMatch;
            return true;
        }

        return false;
    }
}
