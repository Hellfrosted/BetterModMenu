using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;

namespace BetterModMenu.Patches;

internal static class ModdingScreenNodeOps
{
    public static NModdingScreen? FindOwningScreen(Node? node)
    {
        while (node != null)
        {
            if (node is NModdingScreen screen)
                return screen;

            node = node.GetParent();
        }

        return null;
    }

    public static Control? GetModRowContainer(NModdingScreen screen)
    {
        return screen.GetNodeOrNull<Control>(ModdingScreenConstants.ModsScrollContentPath);
    }
}
