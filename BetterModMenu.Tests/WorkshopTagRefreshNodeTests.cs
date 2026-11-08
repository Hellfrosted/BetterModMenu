using BetterModMenu.Data;
using BetterModMenu.Patches;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BetterModMenu.Patches
{
    internal sealed class ModdingScreenSession { public Action? WorkshopTagsChanged { get; set; } }
    internal static class ModdingScreenContext
    {
        public static NModdingScreen? Current { get; set; }
        public static ModdingScreenSession Session { get; set; } = new();
        public static bool IsCurrentScreen(NModdingScreen? screen) => screen != null && screen == Current;
        public static ModdingScreenSession GetSession(NModdingScreen screen) => Session;
    }
    internal static class NModdingScreenPatch
    {
        public static Action<NModdingScreen>? Refresh { get; set; }
        public static void RefreshModNameStyles(NModdingScreen screen) => Refresh?.Invoke(screen);
    }
}

namespace BetterModMenu.Tests
{
    [TestClass]
    public class WorkshopTagRefreshNodeTests
    {
        [TestInitialize]
        public void SetUp()
        {
            ProfileManager.TagPublications.Clear();
            ModdingScreenContext.Current = null;
            ModdingScreenContext.Session = new();
            NModdingScreenPatch.Refresh = null;
        }

        [TestMethod]
        public void CompletionRefreshesCurrentScreenAndNotifiesEditorOnce()
        {
            var screen = new NModdingScreen();
            ModdingScreenContext.Current = screen;
            var events = new List<string>();
            NModdingScreenPatch.Refresh = actual =>
            {
                Assert.AreSame(screen, actual);
                events.Add("names");
            };
            ModdingScreenContext.Session.WorkshopTagsChanged = () => events.Add("editor");
            ProfileManager.TagPublications.Enqueue(false);
            ProfileManager.TagPublications.Enqueue(true);
            var node = new WorkshopTagRefreshNode { Screen = screen };

            node._Process(0);
            Assert.AreEqual(0, events.Count);
            node._Process(0);
            node._Process(0);
            CollectionAssert.AreEqual(new[] { "names", "editor" }, events);
        }

        [TestMethod]
        public void InactiveScreenDoesNotConsumeCompletion()
        {
            var screen = new NModdingScreen();
            var node = new WorkshopTagRefreshNode { Screen = screen };
            ProfileManager.TagPublications.Enqueue(true);
            node._Process(0);
            Assert.AreEqual(1, ProfileManager.TagPublications.Count);

            ModdingScreenContext.Current = screen;
            int refreshes = 0;
            NModdingScreenPatch.Refresh = _ => refreshes++;
            node._Process(0);
            Assert.AreEqual(1, refreshes);
            Assert.AreEqual(0, ProfileManager.TagPublications.Count);
        }
    }
}
