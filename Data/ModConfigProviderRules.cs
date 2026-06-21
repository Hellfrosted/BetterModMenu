namespace BetterModMenu.Data;

internal enum ModConfigProviderKind
{
    None,
    RitsuLib,
    BaseLib
}

internal static class ModConfigProviderRules
{
    public static ModConfigProviderKind SelectProvider(bool ritsuLibAvailable, bool baseLibAvailable)
    {
        if (ritsuLibAvailable)
            return ModConfigProviderKind.RitsuLib;

        return baseLibAvailable ? ModConfigProviderKind.BaseLib : ModConfigProviderKind.None;
    }
}
