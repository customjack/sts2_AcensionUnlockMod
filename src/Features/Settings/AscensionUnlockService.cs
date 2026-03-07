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
    /// Applies requested per-character levels, reveals ascension epochs if needed, and saves progress.
    /// </summary>
    public static bool TryApplyLevels(IReadOnlyDictionary<ModelId, int> requestedLevels, out string message)
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
            var maxAppliedLevel = MinAscension;

            foreach (var character in characters)
            {
                var requested = requestedLevels.TryGetValue(character.Id, out var levelFromUi)
                    ? levelFromUi
                    : progress.GetStatsForCharacter(character.Id)?.MaxAscension ?? MinAscension;
                var clamped = ClampLevel(requested);
                maxAppliedLevel = Math.Max(maxAppliedLevel, clamped);

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

            progress.MaxMultiplayerAscension = maxAppliedLevel;
            progress.PreferredMultiplayerAscension = maxAppliedLevel;
            saveManager.SaveProgressFile();

            message = $"Applied {updatedCharacters} updates across {characters.Count} characters. Revealed {revealedEpochs} ascension epochs.";
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
    /// Returns playable characters (excluding random-select pseudo character).
    /// </summary>
    private static IEnumerable<CharacterModel> GetEditableCharacters()
    {
        var randomCharacterId = ModelDb.GetId<RandomCharacter>();
        return ModelDb.AllCharacters.Where(character => character.Id != randomCharacterId);
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
