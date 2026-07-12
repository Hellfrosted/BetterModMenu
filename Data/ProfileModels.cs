namespace BetterModMenu.Data;

public class ModProfile
{
    public string Name { get; set; } = "Default";
    public HashSet<string> DisabledMods { get; set; } = new();
}

public class ProfileSaveData
{
    public List<ModProfile> Profiles { get; set; } = new();
    public int CurrentProfileIndex { get; set; } = 0;

    public List<string> CustomGroups { get; set; } = new();
    public Dictionary<string, string> ModGroups { get; set; } = new();
    public HashSet<string> CollapsedGroups { get; set; } = new();
    public TutorialState Tutorial { get; set; } = new();
    public CloudBackupSettings CloudBackups { get; set; } = new();
    public ModNameStyleSettings ModNameStyles { get; set; } = new();
    public Dictionary<string, ModAnnotation> ModAnnotations { get; set; } = new();
}

public class ModAnnotation
{
    public string Alias { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class ModNameStyleSettings
{
    public bool Enabled { get; set; } = true;
    public bool UseDefaultTagFormats { get; set; } = true;
    public Dictionary<string, string> TagFormats { get; set; } = new();
    public List<string> TagPriority { get; set; } = new();
    public HashSet<string> DisabledTags { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> ModFormats { get; set; } = new();
}
