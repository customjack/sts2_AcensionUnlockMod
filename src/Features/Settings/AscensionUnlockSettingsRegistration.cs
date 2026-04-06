using System;
using System.Collections.Generic;
using ModManagerSettings.Api;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace AscensionUnlockMod.Features.Settings;

internal static class AscensionUnlockSettingsRegistration
{
    private const string ModId = "AscensionUnlockMod";
    private const string CharacterSettingsPath = "Settings/Characters";
    private const string MultiplayerSettingsPath = "Settings/Multiplayer";
    private const string MultiplayerKey = "multiplayer_ascension";

    private static bool _registered;
    private static readonly Dictionary<ModelId, int> PendingCharacterLevels = [];
    private static int _pendingMultiplayerLevel = AscensionUnlockService.MaxAscension;

    public static void Register()
    {
        if (_registered)
        {
            return;
        }

        _registered = true;
        ModSettingsRegistry.Register(new ModSettingsRegistration
        {
            ModPckName = ModId,
            DisplayName = "Ascension Unlock",
            Description = "Controls per-character unlock ascension and the separate multiplayer ascension selection.",
            ShowSettingsButtonInModdingMenu = true,
            ExplorerDescription =
                "Adjust ascension levels directly from ModManagerSettings. " +
                "Character values update unlock/preferred ascension, and multiplayer is controlled separately.",
            NumberSettings = BuildNumberSettings(),
            OnApply = ApplyPendingLevels,
            OnRestoreDefaults = RestoreDefaults
        });
    }

    private static IReadOnlyList<ModSettingNumberDefinition> BuildNumberSettings()
    {
        var settings = new List<ModSettingNumberDefinition>();
        foreach (var character in AscensionUnlockService.GetEditableCharacters())
        {
            var characterId = character.Id;
            var characterName = string.IsNullOrWhiteSpace(character.Title.GetFormattedText())
                ? characterId.Entry
                : character.Title.GetFormattedText();

            settings.Add(new ModSettingNumberDefinition
            {
                Key = $"character_{characterId.Entry.ToLowerInvariant()}",
                Label = characterName,
                Description = $"Set the unlocked ascension level for {characterName}.",
                Path = CharacterSettingsPath,
                DefaultValue = AscensionUnlockService.MaxAscension,
                MinValue = AscensionUnlockService.MinAscension,
                MaxValue = AscensionUnlockService.MaxAscension,
                Step = 1d,
                GetCurrentValue = () => AscensionUnlockService.GetCurrentCharacterAscensionLevel(characterId),
                OnApply = value => PendingCharacterLevels[characterId] = ClampUiValue(value)
            });
        }

        settings.Add(new ModSettingNumberDefinition
        {
            Key = MultiplayerKey,
            Label = "Multiplayer Ascension",
            Description = "Sets the separately tracked multiplayer ascension level.",
            Path = MultiplayerSettingsPath,
            DefaultValue = AscensionUnlockService.MaxAscension,
            MinValue = AscensionUnlockService.MinAscension,
            MaxValue = AscensionUnlockService.MaxAscension,
            Step = 1d,
            GetCurrentValue = () => AscensionUnlockService.GetCurrentMultiplayerAscensionLevel(),
            OnApply = value => _pendingMultiplayerLevel = ClampUiValue(value)
        });

        return settings;
    }

    private static void ApplyPendingLevels()
    {
        try
        {
            if (!AscensionUnlockService.TryApplyLevels(PendingCharacterLevels, _pendingMultiplayerLevel, out var message))
            {
                Log.Warn($"[AscensionUnlockMod] Failed applying settings from ModManagerSettings. {message}");
                return;
            }

        }
        finally
        {
            PendingCharacterLevels.Clear();
        }
    }

    private static void RestoreDefaults()
    {
        PendingCharacterLevels.Clear();
        _pendingMultiplayerLevel = AscensionUnlockService.MaxAscension;

        if (!AscensionUnlockService.TryRestoreDefaults(out var message))
        {
            Log.Warn($"[AscensionUnlockMod] Failed restoring ascension defaults. {message}");
            return;
        }

    }

    private static int ClampUiValue(double value)
    {
        return Math.Clamp((int)Math.Round(value), AscensionUnlockService.MinAscension, AscensionUnlockService.MaxAscension);
    }
}
