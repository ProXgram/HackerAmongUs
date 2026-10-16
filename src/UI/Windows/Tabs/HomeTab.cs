using UnityEngine;

namespace HAU;

public class HomeTab : ITab
{
    public string name => "Home";

    public void Draw()
    {
        UIHelpers.BeginSection("Quick access");
        ShortcutRow("Sprint & speed", "sprint", "Outfits & name", "outfits");
        ShortcutRow("Player ESP", "player-esp", "Automatic Tasks", "automatic-tasks");
        ShortcutRow("Multi Role", "multi-role", "Sabotage & repair", "sabotage");
        ShortcutRow("Follow & orbit", "follow", "Check for updates", "updates");
        UIHelpers.Hint("Looking for something else? Click Find a feature above and type its name.");
        UIHelpers.EndSection();

        UIHelpers.BeginSection("Current activity");
        GUILayout.Label("Sprint: " + (CheatToggles.sprint ? "enabled" : "off") +
            " / " + HAUPlugin.sprintMultiplier.Value.ToString("0.0") + "x / " + HAUPlugin.sprintKeybind.Value);
        GUILayout.Label(MovementAutomation.StatusText);
        GUILayout.Label("Automatic Tasks: " + (CheatToggles.automaticTasks ? "on for this round" : "off"));
        GUILayout.Label("Multi Role: " + (CheatToggles.multiRole ? "on" : "off"));
        UIHelpers.Hint("Automatic Tasks starts only when you enable it yourself. F8 stops movement and color cycling.");
        UIHelpers.EndSection();
        UIHelpers.Hint(Utils.isFreePlay ? "Freeplay is ready. Role experiments are in Practice."
            : Utils.isHost ? "You are hosting. Shared rules are in Host Controls."
            : Utils.isLobby || Utils.isInGame ? "You are a guest. Shared rules still need host support."
            : "Join a lobby or open Freeplay to use player controls.");
    }

    private static void ShortcutRow(string firstLabel, string firstId, string secondLabel, string secondId)
    {
        bool horizontal = MenuUI.ContentWidth >= 330f;
        if (horizontal) GUILayout.BeginHorizontal();
        if (GUILayout.Button(firstLabel, GUILayout.Height(34f))) MenuUI.OpenFeature(firstId);
        if (GUILayout.Button(secondLabel, GUILayout.Height(34f))) MenuUI.OpenFeature(secondId);
        if (horizontal) GUILayout.EndHorizontal();
    }
}
