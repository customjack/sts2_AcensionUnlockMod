using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Timeline;
using MegaCrit.Sts2.Core.Timeline.Epochs;

namespace AscensionUnlockMod.Features.Settings;

/// <summary>
/// View model used by settings UI for one character's ascension controls.
/// </summary>
internal sealed class CharacterAscensionSetting
{
    public required ModelId CharacterId { get; init; }
    public required string CharacterName { get; init; }
    public required int CurrentMaxAscension { get; init; }
    public required bool IsAscensionEpochRevealed { get; init; }
}

/// <summary>
/// Applies ascension-unlock updates directly to progress save state.
/// </summary>
internal static class AscensionUnlockService
{
    public const int MinAscension = 0;
    public const int MaxAscension = 10;

    /// <summary>
    /// Returns playable characters (excluding random-select pseudo character).
    /// </summary>
    public static IReadOnlyList<CharacterModel> GetEditableCharacters()
    {
        var randomCharacterId = ModelDb.GetId<RandomCharacter>();
        return ModelDb.AllCharacters
            .Where(character => character.Id != randomCharacterId)
            .ToList();
    }

    /// <summary>
    /// Returns one editable ascension row per playable character.
    /// </summary>
    public static IReadOnlyList<CharacterAscensionSetting> GetCharacterSettings()
    {
        var saveManager = SaveManager.Instance;
        var progress = saveManager.Progress;

        return GetEditableCharacters()
            .Select(character =>
            {
                var current = progress.GetStatsForCharacter(character.Id)?.MaxAscension ?? 0;
                var isEpochRevealed = !TryGetAscensionEpochId(character.Id, out var epochId) || saveManager.IsEpochRevealed(epochId);

                return new CharacterAscensionSetting
                {
                    CharacterId = character.Id,
                    CharacterName = GetCharacterName(character),
                    CurrentMaxAscension = ClampLevel(current),
                    IsAscensionEpochRevealed = isEpochRevealed
                };
            })
            .ToList();
    }

    /// <summary>
    /// Returns short summary text for UI body.
    /// </summary>
    public static string GetCurrentSummaryText()
    {
        try
        {
            var settings = GetCharacterSettings();
            if (settings.Count == 0)
            {
                return "No playable characters were found.";
            }

            var minCharacter = settings.Min(setting => setting.CurrentMaxAscension);
            var maxCharacter = settings.Max(setting => setting.CurrentMaxAscension);
            var lockedEpochs = settings.Count(setting => !setting.IsAscensionEpochRevealed);

            if (minCharacter == maxCharacter)
            {
                return $"Current character unlock level: A{maxCharacter}. Epochs not revealed: {lockedEpochs}.";
            }

            return $"Current character unlock range: A{minCharacter} - A{maxCharacter}. Epochs not revealed: {lockedEpochs}.";
        }
        catch (Exception ex)
        {
            Log.Warn($"[AscensionUnlockMod] Could not build summary text: {ex.Message}");
            return "Current ascension state unavailable. You can still apply values.";
        }
    }

    /// <summary>
    /// Returns one character's current ascension level as represented in progress save.
    /// </summary>
    public static int GetCurrentCharacterAscensionLevel(ModelId characterId)
    {
        try
        {
            var progress = SaveManager.Instance.Progress;
            return ClampLevel(progress.GetStatsForCharacter(characterId)?.MaxAscension ?? MinAscension);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("InitProfileId", StringComparison.Ordinal))
        {
            Log.Warn($"[AscensionUnlockMod] Character ascension level requested before profile init for '{characterId}'.");
            return MaxAscension;
        }
    }

    /// <summary>
    /// Returns the currently selected multiplayer ascension level.
    /// </summary>
    public static int GetCurrentMultiplayerAscensionLevel()
    {
        try
        {
            var progress = SaveManager.Instance.Progress;
            var selected = progress.PreferredMultiplayerAscension;
            if (selected <= MinAscension && progress.MaxMultiplayerAscension > MinAscension)
            {
                selected = progress.MaxMultiplayerAscension;
            }

            return ClampLevel(selected);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("InitProfileId", StringComparison.Ordinal))
        {
            Log.Warn("[AscensionUnlockMod] Multiplayer ascension level requested before profile init.");
            return MaxAscension;
        }
    }

    /// <summary>
    /// Applies requested per-character levels, reveals ascension epochs if needed, and saves progress.
    /// </summary>
    public static bool TryApplyLevels(
        IReadOnlyDictionary<ModelId, int> requestedLevels,
        int requestedMultiplayerLevel,
        out string message)
    {
        try
        {
            var saveManager = SaveManager.Instance;
            var progress = saveManager.Progress;
            var characters = GetEditableCharacters().ToList();

            if (characters.Count == 0)
            {
                message = "No playable characters found to update.";
                return false;
            }

            var revealedEpochs = 0;
            var updatedCharacters = 0;

            foreach (var character in characters)
            {
                var requested = requestedLevels.TryGetValue(character.Id, out var levelFromUi)
                    ? levelFromUi
                    : progress.GetStatsForCharacter(character.Id)?.MaxAscension ?? MinAscension;
                var clamped = ClampLevel(requested);

                var stats = progress.GetOrCreateCharacterStats(character.Id);
                if (stats.MaxAscension != clamped || stats.PreferredAscension != clamped)
                {
                    updatedCharacters++;
                }

                stats.MaxAscension = clamped;
                stats.PreferredAscension = clamped;

                if (TryGetAscensionEpochId(character.Id, out var epochId) && !saveManager.IsEpochRevealed(epochId))
                {
                    saveManager.ObtainEpochOverride(epochId, EpochState.Revealed);
                    revealedEpochs++;
                }
            }

            var multiplayerLevel = ClampLevel(requestedMultiplayerLevel);
            var multiplayerChanged =
                progress.MaxMultiplayerAscension != multiplayerLevel ||
                progress.PreferredMultiplayerAscension != multiplayerLevel;
            if (multiplayerChanged)
            {
                updatedCharacters++;
            }

            progress.MaxMultiplayerAscension = multiplayerLevel;
            progress.PreferredMultiplayerAscension = multiplayerLevel;
            saveManager.SaveProgressFile();

            message =
                $"Applied {updatedCharacters} updates across {characters.Count} characters " +
                $"and multiplayer A{multiplayerLevel}. Revealed {revealedEpochs} ascension epochs.";
            return true;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("InitProfileId", StringComparison.Ordinal))
        {
            message = "Profile not ready yet. Open the menu after main menu fully loads.";
            Log.Warn($"[AscensionUnlockMod] Save profile not initialized: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            message = "Failed to update ascension unlock levels.";
            Log.Error($"[AscensionUnlockMod] Failed to apply ascension unlock levels: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Resets all character and multiplayer ascension controls to fully unlocked defaults.
    /// </summary>
    public static bool TryRestoreDefaults(out string message)
    {
        var characterLevels = GetEditableCharacters()
            .ToDictionary(character => character.Id, _ => MaxAscension);
        return TryApplyLevels(characterLevels, MaxAscension, out message);
    }

    /// <summary>
    /// Maps character id to the epoch id that enables ascension for that character.
    /// </summary>
    private static bool TryGetAscensionEpochId(ModelId characterId, out string epochId)
    {
        if (characterId == ModelDb.GetId<Ironclad>())
        {
            epochId = EpochModel.GetId<Ironclad4Epoch>();
            return true;
        }

        if (characterId == ModelDb.GetId<Silent>())
        {
            epochId = EpochModel.GetId<Silent4Epoch>();
            return true;
        }

        if (characterId == ModelDb.GetId<Regent>())
        {
            epochId = EpochModel.GetId<Regent4Epoch>();
            return true;
        }

        if (characterId == ModelDb.GetId<Defect>())
        {
            epochId = EpochModel.GetId<Defect4Epoch>();
            return true;
        }

        if (characterId == ModelDb.GetId<Necrobinder>())
        {
            epochId = EpochModel.GetId<Necrobinder4Epoch>();
            return true;
        }

        epochId = string.Empty;
        return false;
    }

    /// <summary>
    /// Gets a display name suitable for row labels.
    /// </summary>
    private static string GetCharacterName(CharacterModel character)
    {
        var localizedName = character.Title.GetFormattedText();
        if (!string.IsNullOrWhiteSpace(localizedName))
        {
            return localizedName;
        }

        return character.Id.Entry;
    }

    private static int ClampLevel(int value)
    {
        return Math.Clamp(value, MinAscension, MaxAscension);
    }
}
