using System.Collections.Generic;
using Godot;

namespace BetterModMenu.Patches;

internal enum ModdingScreenIcon
{
    FilePlus,
    FilePenLine,
    FileX,
    ListChecks,
    ListX,
    PencilLine,
    Trash,
    ListChevronsDownUp,
    ListChevronsUpDown,
    ChevronUp,
    ChevronDown
}

internal static class ModdingScreenIcons
{
    private const string IconColor = "#EBDCBF";

    private static readonly Dictionary<ModdingScreenIcon, Texture2D> Cache = new();

    public static Texture2D? Get(ModdingScreenIcon icon)
    {
        if (Cache.TryGetValue(icon, out var texture))
            return texture;

        var image = new Image();
        string svg = GetSvg(icon).Replace("currentColor", IconColor);
        Error error = image.LoadSvgFromString(svg);
        if (error != Error.Ok)
            return null;

        texture = ImageTexture.CreateFromImage(image);
        Cache[icon] = texture;
        return texture;
    }

    private static string GetSvg(ModdingScreenIcon icon)
    {
        return icon switch
        {
            ModdingScreenIcon.FilePlus => """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14 2v5a1 1 0 0 0 1 1h5M9 15h6m-3 3v-6M6 22a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h8a2.4 2.4 0 0 1 1.704.706l3.588 3.588A2.4 2.4 0 0 1 20 8v12a2 2 0 0 1-2 2z"/></svg>""",
            ModdingScreenIcon.FilePenLine => """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14.364 13.634a2 2 0 0 0-.506.854l-.837 2.87a.5.5 0 0 0 .62.62l2.87-.837a2 2 0 0 0 .854-.506l4.013-4.009a1 1 0 0 0-3.004-3.004zm.123-5.776A1 1 0 0 1 14 7V2M20 19.645V20a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h8a2.4 2.4 0 0 1 1.704.706l2.516 2.516M8 18h1"/></svg>""",
            ModdingScreenIcon.FileX => """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14 2v5a1 1 0 0 0 1 1h5m-5.5 4.5l-5 5m0-5l5 5M6 22a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h8a2.4 2.4 0 0 1 1.704.706l3.588 3.588A2.4 2.4 0 0 1 20 8v12a2 2 0 0 1-2 2z"/></svg>""",
            ModdingScreenIcon.ListChecks => """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 5h8m-8 7h8m-8 7h8M3 17l2 2l4-4M3 7l2 2l4-4"/></svg>""",
            ModdingScreenIcon.ListX => """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 5H3m8 7H3m13 7H3m12.5-9.5l5 5m0-5l-5 5"/></svg>""",
            ModdingScreenIcon.PencilLine => """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 21h8M15 5l4 4m2.174-2.188a1 1 0 0 0-3.986-3.987L3.842 16.174a2 2 0 0 0-.5.83l-1.321 4.352a.5.5 0 0 0 .623.622l4.353-1.32a2 2 0 0 0 .83-.497z"/></svg>""",
            ModdingScreenIcon.Trash => """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6M3 6h18M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/></svg>""",
            ModdingScreenIcon.ListChevronsDownUp => """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 5h8m-8 7h8m-8 7h8m4-14l3 3l3-3m-6 14l3-3l3 3"/></svg>""",
            ModdingScreenIcon.ListChevronsUpDown => """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 5h8m-8 7h8m-8 7h8m4-11l3-3l3 3m-6 8l3 3l3-3"/></svg>""",
            ModdingScreenIcon.ChevronUp => """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="m18 15l-6-6l-6 6"/></svg>""",
            ModdingScreenIcon.ChevronDown => """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="m6 9l6 6l6-6"/></svg>""",
            _ => string.Empty
        };
    }
}
