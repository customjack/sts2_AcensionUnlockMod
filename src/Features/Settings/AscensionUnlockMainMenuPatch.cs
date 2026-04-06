using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace AscensionUnlockMod.Features.Settings;

[HarmonyPatch(typeof(NMainMenu), nameof(NMainMenu._Ready))]
internal static class AscensionUnlockMainMenuPatch
{
    public static void Postfix()
    {
        try
        {
            AscensionUnlockSettingsRegistration.Register();
        }
        catch (Exception ex)
        {
            Log.Error($"[AscensionUnlockMod] Failed to register ModManagerSettings on main menu load. {ex}");
        }
    }
}
