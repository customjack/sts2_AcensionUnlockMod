using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace AscensionUnlockMod.Features.Settings;

/// <summary>
/// Owns popup lifecycle and per-character ascension input rendering.
/// </summary>
internal static class SettingsPopupController
{
    /// <summary>
    /// Opens the ascension settings popup in the game's native modal container.
    /// </summary>
    public static void Open()
    {
        Log.Info("[AscensionUnlockMod] Open settings popup requested.");
        var modalContainer = NModalContainer.Instance;
        if (modalContainer == null)
        {
            Log.Warn("[AscensionUnlockMod] NModalContainer instance unavailable; cannot open settings popup.");
            return;
        }

        if (modalContainer.OpenModal is NGenericPopup openPopup && openPopup.Name == SettingsConstants.SettingsPopupName)
        {
            Log.Info("[AscensionUnlockMod] Settings popup already open.");
            return;
        }

        if (modalContainer.FindChild(SettingsConstants.SettingsPopupName, recursive: false, owned: false) is NGenericPopup existingPopup)
        {
            Log.Info("[AscensionUnlockMod] Reusing existing settings popup node.");
            existingPopup.GrabFocus();
            return;
        }

        var popup = NGenericPopup.Create();
        if (popup == null)
        {
            Log.Error("[AscensionUnlockMod] Failed to create NGenericPopup for settings.");
            return;
        }

        popup.Name = SettingsConstants.SettingsPopupName;
        popup.Connect(Node.SignalName.Ready, Callable.From(() => ConfigurePopup(popup)));
        modalContainer.Add(popup);
        Log.Info("[AscensionUnlockMod] Settings popup added to modal container.");
    }

    /// <summary>
    /// Configures popup title/body/buttons and injects per-character ascension controls.
    /// </summary>
    private static void ConfigurePopup(NGenericPopup popup)
    {
        var verticalPopup = popup.GetNodeOrNull<NVerticalPopup>("VerticalPopup");
        if (verticalPopup == null)
        {
            Log.Error("[AscensionUnlockMod] VerticalPopup node missing in settings popup.");
            return;
        }

        var settings = AscensionUnlockService.GetCharacterSettings();
        var inputsByCharacter = new Dictionary<ModelId, SpinBox>();

        verticalPopup.SetText(
            "Ascension Unlocks",
            "Set max ascension unlock level per character (0-10). Missing ascension epochs will be revealed on apply.");

        verticalPopup.InitYesButton(new LocString("main_menu_ui", "GENERIC_POPUP.confirm"), _ =>
        {
            var requestedLevels = inputsByCharacter.ToDictionary(
                kvp => kvp.Key,
                kvp => (int)Math.Round(kvp.Value.Value));

            var success = AscensionUnlockService.TryApplyLevels(requestedLevels, out var message);
            var logMessage = $"[AscensionUnlockMod] Apply success={success}. {message}";
            if (success)
            {
                Log.Info(logMessage);
            }
            else
            {
                Log.Warn(logMessage);
            }

            ShowFeedback(message);
        });
        verticalPopup.YesButton.SetText("Apply");
        verticalPopup.HideNoButton();

        InjectContent(verticalPopup, settings, inputsByCharacter);
    }

    /// <summary>
    /// Places summary + one ascension input row per character in popup body area.
    /// </summary>
    private static void InjectContent(
        NVerticalPopup verticalPopup,
        IReadOnlyList<CharacterAscensionSetting> settings,
        IDictionary<ModelId, SpinBox> inputsByCharacter)
    {
        if (verticalPopup.FindChild(SettingsConstants.InjectedContentName, recursive: false, owned: false) is Control existing)
        {
            existing.QueueFree();
        }

        var content = new VBoxContainer
        {
            Name = SettingsConstants.InjectedContentName,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        content.AddThemeConstantOverride("separation", 8);

        content.AddChild(new Label
        {
            Text = AscensionUnlockService.GetCurrentSummaryText(),
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });

        foreach (var setting in settings)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 8);

            var labelText = setting.CharacterName;
            if (!setting.IsAscensionEpochRevealed)
            {
                labelText += " (epoch locked)";
            }

            row.AddChild(new Label
            {
                Text = labelText,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                VerticalAlignment = VerticalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            });

            var input = CreateAscensionInput(setting.CurrentMaxAscension);
            inputsByCharacter[setting.CharacterId] = input;
            row.AddChild(input);

            content.AddChild(row);
        }

        if (verticalPopup.GetNodeOrNull<Control>("Description") is { } description)
        {
            description.Visible = false;
            content.Position = description.Position;
            content.Size = description.Size;
        }
        else
        {
            content.Position = new Vector2(70f, 170f);
            content.Size = new Vector2(540f, 260f);
            Log.Warn("[AscensionUnlockMod] Description node missing; using fallback content bounds.");
        }

        verticalPopup.AddChild(content);
    }

    /// <summary>
    /// Builds a SpinBox constrained to supported ascension range.
    /// </summary>
    private static SpinBox CreateAscensionInput(int value)
    {
        return new SpinBox
        {
            MinValue = AscensionUnlockService.MinAscension,
            MaxValue = AscensionUnlockService.MaxAscension,
            Step = 1,
            Value = value,
            Rounded = true,
            Editable = true,
            CustomMinimumSize = new Vector2(120f, 34f),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
        };
    }

    /// <summary>
    /// Shows short on-screen feedback after applying changes.
    /// </summary>
    private static void ShowFeedback(string text)
    {
        var vfx = NFullscreenTextVfx.Create($"[Ascension] {text}");
        if (vfx != null)
        {
            NGame.Instance?.AddChild(vfx);
        }
    }
}
