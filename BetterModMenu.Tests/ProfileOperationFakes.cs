using BetterModMenu.Data;
using MegaCrit.Sts2.Core.Saves;

namespace Godot
{
    internal class Node { public virtual void _Process(double delta) { } }
    internal class Control : Node
    {
        public IEnumerable<Node> GetChildren() => [];
    }
}

namespace MegaCrit.Sts2.Core.Nodes.CommonUi
{
    internal class NTickbox { public bool IsTicked { get; set; } }
}

namespace MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen
{
    internal class NModdingScreen : Godot.Node { }
    internal class NModMenuRow : Godot.Node
    {
        public FakeMod? Mod { get; set; }
        public T? GetNodeOrNull<T>(string path) where T : class => null;
    }
    internal class FakeMod { public FakeManifest? manifest { get; set; } }
    internal class FakeManifest { public string? id { get; set; } }
}

namespace MegaCrit.Sts2.Core.Saves
{
    internal class SaveManager
    {
        public static SaveManager? Instance { get; set; }
        public FakeSettingsSave? SettingsSave { get; set; }
        public int FailSavesRemaining { get; set; }
        public List<bool> SavedEnabledStates { get; private set; } = [];
        public void SaveSettings()
        {
            if (FailSavesRemaining-- > 0)
                throw new IOException("Settings save failed");
            SavedEnabledStates = SettingsSave?.ModSettings?.ModList.Select(mod => mod.IsEnabled).ToList() ?? [];
        }
    }
    internal class FakeSettingsSave { public FakeModSettings? ModSettings { get; set; } }
    internal class FakeModSettings { public List<FakeModSetting> ModList { get; set; } = []; }
    internal class FakeModSetting { public string Id { get; set; } = ""; public bool IsEnabled { get; set; } }
}

namespace BetterModMenu.Data
{
    internal static class ProfileManager
    {
        public static List<ModProfile> Profiles { get; set; } = [];
        public static int CurrentProfileIndex { get; set; }
        public static string? LastPersistenceError => "Profile save failed";
        public static FakeLogger ModLogger { get; } = new();
        public static int FailSavesRemaining { get; set; }
        public static List<string> SavedProfileNames { get; private set; } = [];
        public static Queue<bool> TagPublications { get; } = new();
        public static bool PublishWorkshopTags() => TagPublications.Count > 0 && TagPublications.Dequeue();
        public static ModProfile CurrentProfile { get { NormalizeProfileIndex(); return Profiles[CurrentProfileIndex]; } }
        public static void NormalizeProfileIndex()
        {
            if (Profiles.Count == 0) Profiles.Add(new ModProfile());
            CurrentProfileIndex = Math.Clamp(CurrentProfileIndex, 0, Profiles.Count - 1);
        }
        public static void SnapshotCurrentState()
        {
            var mods = SaveManager.Instance?.SettingsSave?.ModSettings?.ModList;
            if (mods != null)
                CurrentProfile.DisabledMods = mods.Where(mod => !mod.IsEnabled).Select(mod => mod.Id).ToHashSet();
        }
        public static bool SaveInMemoryState()
        {
            if (FailSavesRemaining-- > 0) return false;
            SavedProfileNames = Profiles.Select(profile => profile.Name).ToList();
            return true;
        }
    }
    internal class FakeLogger { public void Error(string message) { } }
}
