using BetterModMenu.Data;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;

namespace BetterModMenu.Patches;

[HarmonyPatch(typeof(NModInfoContainer))]
public static class NModInfoContainerPatch
{
    private static readonly System.Reflection.FieldInfo? TitleField = AccessTools.Field(typeof(NModInfoContainer), "_title");

    [HarmonyPatch(nameof(NModInfoContainer.Fill))]
    [HarmonyPostfix]
    public static void Postfix_Fill(NModInfoContainer __instance, Mod mod)
    {
        string modId = mod.manifest?.id ?? string.Empty;
        string displayName = mod.manifest?.name ?? modId;
        if (string.IsNullOrWhiteSpace(modId) || string.IsNullOrWhiteSpace(displayName))
            return;

        if (TitleField?.GetValue(__instance) is not RichTextLabel title)
            return;

        var styleTags = BuildStyleTags(modId);
        if (ModNameStyleRules.TryBuildBbCode(modId, displayName, styleTags, ProfileManager.ModNameStyles, out string bbCode))
        {
            title.BbcodeEnabled = true;
            title.ParseBbcode(bbCode);
            return;
        }

        title.BbcodeEnabled = false;
        title.Text = displayName;
    }

    private static IEnumerable<string> BuildStyleTags(string modId)
    {
        ProfileManager.BuildWorkshopTagCacheIfNeeded();
        if (!ProfileManager.ModWorkshopTagsCache.TryGetValue(modId, out var tags))
            yield break;

        foreach (string tag in tags)
            yield return tag;
    }
}
