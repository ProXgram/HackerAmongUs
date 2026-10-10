using System;
using UnityEngine;

namespace HAU;

public class ConfigTab : ITab
{
    public string name => "Settings";
    private bool _advanced;
    private string _feedback = "";

    public void Draw()
    {
        UIHelpers.BeginSection("Menu controls", "Choose how HAU opens and behaves. Changes are saved automatically.");
        if (GUILayout.Button("Menu key: " + HAUPlugin.menuKeybind.Value))
        {
            string[] keys = { "Delete", "Insert", "F9", "F10" };
            int index = Array.IndexOf(keys, HAUPlugin.menuKeybind.Value);
            HAUPlugin.menuKeybind.Value = keys[(index + 1) % keys.Length];
        }
        HAUPlugin.menuOpenOnMouse.Value = GUILayout.Toggle(HAUPlugin.menuOpenOnMouse.Value,
            new GUIContent("Open at the mouse pointer", "Place the menu near your mouse instead of its last position."));
        HAUPlugin.menuKeepSubwindowsOpen.Value = GUILayout.Toggle(HAUPlugin.menuKeepSubwindowsOpen.Value,
            new GUIContent("Keep tool windows open", "Keep separate tool windows visible when the main menu closes."));
        UIHelpers.Hint("Sprint controls and speed are in Movement. Press F8 to stop movement automation.");
        UIHelpers.EndSection();

        UIHelpers.BeginSection("Appearance", "Select an accent colour for your HAU menu.");
        GUILayout.BeginHorizontal();
        Accent("Cyan", "#22D3EE"); Accent("Green", "#4ADE80"); Accent("Purple", "#C084FC");
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        Accent("Orange", "#FB923C"); Accent("Pink", "#F472B6");
        if (GUILayout.Button("Default")) HAUPlugin.menuHtmlColor.Value = "";
        GUILayout.EndHorizontal();
        CheatToggles.rgbMode = GUILayout.Toggle(CheatToggles.rgbMode, "Animate the menu accent colours");
        CheatToggles.stealthMode = GUILayout.Toggle(CheatToggles.stealthMode,
            new GUIContent("Hide HAU branding in the game", "Hides the mod label and badge on your own screen."));
        UIHelpers.EndSection();

        UIHelpers.BeginSection("Saved setup", "Save your enabled tools as a profile. Automatic Tasks still requires manual activation each round.");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save tool profile")) { CheatToggles.saveProfile = true; _feedback = "Profile save requested."; }
        if (GUILayout.Button("Load tool profile")) { CheatToggles.loadProfile = true; _feedback = "Profile load requested."; }
        GUILayout.EndHorizontal();
        HAUPlugin.autoLoadProfile.Value = GUILayout.Toggle(HAUPlugin.autoLoadProfile.Value, "Load my saved profile when HAU starts");
        if (!string.IsNullOrEmpty(_feedback)) UIHelpers.Hint(_feedback);
        UIHelpers.EndSection();

        UIHelpers.BeginSection("Automatic updates", "Compatible published updates install after you close Among Us. Your settings are kept.");
        HAUPlugin.automaticUpdates.Value = GUILayout.Toggle(HAUPlugin.automaticUpdates.Value, "Check for updates on the next launch");
        UIHelpers.Hint(HAU.Updates.AutomaticUpdateHandler.Status);
        UIHelpers.Hint("A new Among Us version needs a compatible HAU build first.");
        UIHelpers.EndSection();

        if (GUILayout.Button(_advanced ? "Hide advanced settings" : "Show advanced settings")) _advanced = !_advanced;
        if (!_advanced) return;
        UIHelpers.BeginSection("Advanced", "Optional settings for local display, clipboard convenience and configuration files.");
        CheatToggles.avoidPenalties = GUILayout.Toggle(CheatToggles.avoidPenalties, "Clear the local leave-penalty timer");
        CheatToggles.unlockFeatures = GUILayout.Toggle(CheatToggles.unlockFeatures, "Show extra game controls");
        CheatToggles.copyLobbyCodeOnDisconnect = GUILayout.Toggle(CheatToggles.copyLobbyCodeOnDisconnect, "Copy the lobby code when disconnected");
        CheatToggles.spoofAprilFoolsDate = GUILayout.Toggle(CheatToggles.spoofAprilFoolsDate, "Use the April Fools display date");
        HAUPlugin.menuAllowClickThrough.Value = GUILayout.Toggle(HAUPlugin.menuAllowClickThrough.Value, "Allow clicks through the menu into the game");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Open settings file")) CheatToggles.openConfig = true;
        if (GUILayout.Button("Reload settings file")) CheatToggles.reloadConfig = true;
        GUILayout.EndHorizontal();
        if (GUILayout.Button("Reset menu appearance"))
        {
            HAUPlugin.menuHtmlColor.Value = "";
            HAUPlugin.menuOpenOnMouse.Value = false;
            CheatToggles.rgbMode = false;
            _feedback = "Menu appearance reset. Gameplay tools were preserved.";
        }
        if (GUILayout.Button("Turn off tools and close HAU")) CheatToggles.panicMode = true;
        UIHelpers.EndSection();
    }

    private static void Accent(string label, string value)
    {
        if (GUILayout.Button(label)) HAUPlugin.menuHtmlColor.Value = value;
    }
}
