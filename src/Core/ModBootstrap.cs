using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;

namespace AscensionUnlockMod.Core;

/// <summary>
/// Central startup for AscensionUnlockMod.
/// </summary>
public static class ModBootstrap
{
    private const string HarmonyId = "ascensionunlockmod.harmony";

    private static bool _initialized;
    private static Harmony? _harmony;

    /// <summary>
    /// Initializes mod runtime once per process.
    /// </summary>
    public static void Initialize()
    {
        if (_initialized)
        {
            Log.Info("[AscensionUnlockMod] ModBootstrap.Initialize skipped (already initialized).");
            return;
        }

        _initialized = true;
        Log.Info("[AscensionUnlockMod] Mod bootstrap starting.");

        _harmony = new Harmony(HarmonyId);
        _harmony.PatchAll();
        Log.Info($"[AscensionUnlockMod] Harmony patches applied with id '{HarmonyId}'.");
    }
}
