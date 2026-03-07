using System;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace AscensionUnlockMod.Features.Settings;

/// <summary>
/// Owns settings-button injection and native main-menu button styling.
/// </summary>
internal static class SettingsButtonInstaller
{
    private static readonly StringName MainMenuFocusedMethod = new("MainMenuButtonFocused");
    private static readonly StringName MainMenuUnfocusedMethod = new("MainMenuButtonUnfocused");

    /// <summary>
    /// Adds the Ascension button to main menu button list once.
    /// </summary>
    public static void Attach(NMainMenu mainMenu, Action onPressed)
    {
        var buttonRoot = mainMenu.GetNodeOrNull<Control>("MainMenuTextButtons");
        if (buttonRoot == null)
        {
            Log.Warn("[AscensionUnlockMod] MainMenuTextButtons node not found; cannot add settings button.");
            return;
        }

        if (buttonRoot.FindChild(SettingsConstants.SettingsButtonName, recursive: false, owned: false) != null)
        {
            Log.Info("[AscensionUnlockMod] Settings button already attached.");
            return;
        }

        var template = mainMenu.GetNodeOrNull<NMainMenuTextButton>("MainMenuTextButtons/SettingsButton");
        var button = CreateButton(mainMenu, template, onPressed);

        if (buttonRoot is Container container)
        {
            container.AddChild(button);
            if (template != null)
            {
                var targetIndex = Math.Min(template.GetIndex() + 1, container.GetChildCount() - 1);
                container.MoveChild(button, targetIndex);
            }
        }
        else
        {
            buttonRoot.AddChild(button);
            if (template != null)
            {
                button.Position = template.Position + new Vector2(0f, template.Size.Y + 10f);
            }
        }

        Log.Info("[AscensionUnlockMod] Added main-menu Ascension settings button.");
    }

    /// <summary>
    /// Creates a native-like menu button by cloning the game's Settings button.
    /// </summary>
    private static Control CreateButton(NMainMenu mainMenu, NMainMenuTextButton? template, Action onPressed)
    {
        if (template?.Duplicate((int)(Node.DuplicateFlags.Groups | Node.DuplicateFlags.Scripts | Node.DuplicateFlags.UseInstantiation)) is NMainMenuTextButton cloned)
        {
            cloned.Name = SettingsConstants.SettingsButtonName;
            cloned.TooltipText = "Set ascension unlock level for all characters";

            cloned.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => onPressed()));
            cloned.Connect(NClickableControl.SignalName.Focused, Callable.From<NMainMenuTextButton>(button =>
            {
                Callable.From(() => mainMenu.Call(MainMenuFocusedMethod, button)).CallDeferred();
            }));
            cloned.Connect(NClickableControl.SignalName.Unfocused, Callable.From<NMainMenuTextButton>(button =>
            {
                mainMenu.Call(MainMenuUnfocusedMethod, button);
            }));

            cloned.Connect(Node.SignalName.Ready, Callable.From(() =>
            {
                SetButtonText(cloned, "Ascension");
                Log.Info("[AscensionUnlockMod] Main-menu settings button text applied.");
            }));

            return cloned;
        }

        Log.Warn("[AscensionUnlockMod] Failed to clone native SettingsButton; using fallback Button.");
        var fallback = new Button
        {
            Name = SettingsConstants.SettingsButtonName,
            Text = "Ascension",
            TooltipText = "Set ascension unlock level for all characters",
            CustomMinimumSize = new Vector2(220f, 40f),
            FocusMode = Control.FocusModeEnum.All,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter
        };

        fallback.Pressed += onPressed;
        return fallback;
    }

    /// <summary>
    /// Writes custom text into duplicated native menu button label.
    /// </summary>
    private static void SetButtonText(NMainMenuTextButton button, string text)
    {
        if (button.label != null)
        {
            button.label.Text = text;
            return;
        }

        if (FindFirstMegaLabel(button) is { } nestedLabel)
        {
            nestedLabel.Text = text;
        }
    }

    /// <summary>
    /// Finds the first MegaLabel in node descendants.
    /// </summary>
    private static MegaLabel? FindFirstMegaLabel(Node node)
    {
        if (node is MegaLabel label)
        {
            return label;
        }

        foreach (Node child in node.GetChildren())
        {
            if (FindFirstMegaLabel(child) is { } found)
            {
                return found;
            }
        }

        return null;
    }
}
