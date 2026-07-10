# Mod config provider compatibility

BetterModMenu will expose one selected-mod `Config` action while using provider-specific compatibility behind it: direct RitsuLib navigation when available, and a small reflective BaseLib fallback when BaseLib is the only provider for a mod. This avoids crowding the mod UI with provider choices while acknowledging that RitsuLib exposes a direct settings navigator and BaseLib currently exposes registration but not an equivalent public direct-open API.
