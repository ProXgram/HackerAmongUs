using UnityEngine;
using System;

namespace HAU;

public class MovementTab : ITab
{
    public string name => "Movement";
    private int _page;
    private readonly FunTab _stunts = new();

    public void Draw()
    {
        MovementAutomation.Pause();
        SprintHandler.Reset();
        _page = UIHelpers.Subtabs(_page, "Sprint & speed", "Stunts", "Teleport");
        if (_page == 1) { _stunts.Draw(); return; }
        if (_page == 2) { DrawTeleport(); return; }

        UIHelpers.BeginSection("Hold to sprint",
            "The boost applies while your sprint key is held. Close the menu and chat before moving.");
        CheatToggles.sprint = GUILayout.Toggle(CheatToggles.sprint, " Enable sprint");
        GUILayout.Label("Boost: " + HAUPlugin.sprintMultiplier.Value.ToString("0.0") + "x");
        var multiplier = Mathf.Round(GUILayout.HorizontalSlider(HAUPlugin.sprintMultiplier.Value,
            1f, SprintHandler.MaximumMultiplier) * 10f) / 10f;
        if (!Mathf.Approximately(multiplier, HAUPlugin.sprintMultiplier.Value))
            HAUPlugin.sprintMultiplier.Value = multiplier;
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("2x")) HAUPlugin.sprintMultiplier.Value = 2f;
        if (GUILayout.Button("4x")) HAUPlugin.sprintMultiplier.Value = 4f;
        if (GUILayout.Button("10x boost")) HAUPlugin.sprintMultiplier.Value = 10f;
        GUILayout.EndHorizontal();
        if (GUILayout.Button("Sprint key: " + HAUPlugin.sprintKeybind.Value + " / click to change"))
        {
            var keys = new[] { "LeftShift", "RightShift", "LeftControl", "RightControl" };
            var index = Array.IndexOf(keys, HAUPlugin.sprintKeybind.Value);
            HAUPlugin.sprintKeybind.Value = keys[(index + 1) % keys.Length];
        }
        UIHelpers.Hint("Sprint pauses during automatic movement. Your walking speed returns when you release the key.");
        UIHelpers.EndSection();

        UIHelpers.BeginSection("Walking speed", "This is your normal speed before the sprint boost.");
        var local = PlayerControl.LocalPlayer;
        if (local && local.MyPhysics && local.Data)
        {
            bool ghost = local.Data.IsDead;
            var physics = local.MyPhysics;
            float speed = Mathf.Abs(ghost ? physics.GhostSpeed : physics.Speed);
            float defaultSpeed = ghost ? Utils.DefaultGhostSpeed : Utils.DefaultSpeed;
            GUILayout.Label((ghost ? "Ghost speed: " : "Walking speed: ") + speed.ToString("0.00") +
                (Mathf.Approximately(speed, defaultSpeed) ? " / default" : ""));
            float changed = GUILayout.HorizontalSlider(speed, 0f, 20f);
            if (Mathf.Abs(changed - defaultSpeed) < 0.05f) changed = defaultSpeed;
            // The slider edits magnitude. Inversion is a direction, so opening
            // this page must never clamp a negative physics speed to zero.
            float direction = CheatToggles.invertControls ? -1f : 1f;
            if (ghost) physics.GhostSpeed = changed * direction;
            else physics.Speed = changed * direction;
            if (GUILayout.Button("Restore default walking speed"))
            {
                if (ghost) physics.GhostSpeed = defaultSpeed * direction;
                else physics.Speed = defaultSpeed * direction;
            }
        }
        else UIHelpers.Hint("Join a lobby or open Freeplay to adjust your current walking speed.");
        CheatToggles.noClip = GUILayout.Toggle(CheatToggles.noClip, " Walk through walls (noclip)");
        CheatToggles.invertControls = GUILayout.Toggle(CheatToggles.invertControls, " Reverse movement keys");
        UIHelpers.EndSection();
    }

    private static void DrawTeleport()
    {
        UIHelpers.BeginSection("Move to a player", "Choose a player from the selection screen for one teleport.");
        UIHelpers.ChoosePlayer("Choose player and teleport", ref CheatToggles.teleportPlayer, Utils.isPlayer);
        UIHelpers.EndSection();
        UIHelpers.BeginSection("Move to the cursor", "Enable this, close the menu, then right-click a point in the world.");
        CheatToggles.teleportCursor = GUILayout.Toggle(CheatToggles.teleportCursor, " Right-click teleport");
        UIHelpers.EndSection();
        UIHelpers.BeginSection("Saved spots", "Save two positions and jump between them. The mode stops after 12 jumps.");
        DrawSavedSpots();
        UIHelpers.EndSection();
    }

    internal static void DrawSavedSpots()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(MovementAutomation.HasPointA ? "Replace spot A" : "Save spot A"))
            MovementAutomation.SavePointA();
        if (GUILayout.Button(MovementAutomation.HasPointB ? "Replace spot B" : "Save spot B"))
            MovementAutomation.SavePointB();
        GUILayout.EndHorizontal();
        FunTab.DrawMode("Teleport Yo-Yo", MovementMode.TeleportYoYo);
        float interval = Mathf.Round(GUILayout.HorizontalSlider(HAUPlugin.yoyoInterval.Value, 1f, 5f) * 10f) / 10f;
        if (!Mathf.Approximately(interval, HAUPlugin.yoyoInterval.Value))
            HAUPlugin.yoyoInterval.Value = interval;
        GUILayout.Label("Jump interval: " + interval.ToString("0.0") + " seconds");
        UIHelpers.Hint("Close the menu to start. F8 stops the mode.");
    }
}
