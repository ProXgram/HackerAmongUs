using UnityEngine;

namespace HAU;

public class HomeTab : ITab
{
    public string name => "Home";

    public void Draw()
    {
        UIHelpers.BeginSection("Your controls, in one place",
            "Choose a section on the left. Buttons perform an action; switches stay on until you turn them off.");
        GUILayout.Label(Utils.isHost ? "You are hosting. Lobby rules and role assignment are in Host Controls."
            : Utils.isFreePlay ? "Freeplay is ready. Use Practice for role experiments."
            : Utils.isLobby || Utils.isInGame ? "You are a guest. Shared rules still need host support."
            : "Join a lobby or open Freeplay to use player controls.");
        UIHelpers.EndSection();

        UIHelpers.BeginSection("Quick access");
        Shortcut("Sprint, speed and movement modes", "Movement");
        Shortcut("Pick an outfit or change your nickname", "Appearance");
        Shortcut("Vent, track, Vitals and Detective Notes", "Abilities");
        Shortcut("Start your own tasks manually", "Tasks");
        UIHelpers.EndSection();

        UIHelpers.BeginSection("Current activity");
        GUILayout.Label("Sprint: " + (CheatToggles.sprint ? "enabled" : "off") +
            " / " + HAUPlugin.sprintMultiplier.Value.ToString("0.0") + "x / " + HAUPlugin.sprintKeybind.Value);
        GUILayout.Label(MovementAutomation.StatusText);
        GUILayout.Label("Automatic Tasks: " + (CheatToggles.automaticTasks ? "on for this round" : "off"));
        GUILayout.Label("Multi Role: " + (CheatToggles.multiRole ? "on" : "off"));
        UIHelpers.Hint("Automatic Tasks starts only when you enable it yourself. F8 stops movement and color cycling.");
        UIHelpers.EndSection();
    }

    private static void Shortcut(string label, string tab)
    {
        if (GUILayout.Button(label)) MenuUI.OpenTab(tab);
    }
}
