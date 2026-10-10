using UnityEngine;

namespace HAU;

public class FunTab : ITab
{
    public string name => "Stunts";

    public void Draw()
    {
        MovementAutomation.Pause();
        UIHelpers.BeginSection("Follow and orbit", "Both modes walk a ground route to the selected player.");
        GUILayout.Label("Selected player: " + MovementAutomation.TargetName);
        if (GUILayout.Button("Choose next player")) MovementAutomation.CycleTarget();
        DrawMode("Follow player", MovementMode.ShadowFollow);
        DrawMode("Orbit player", MovementMode.TurboOrbit);
        var boost = Mathf.Round(GUILayout.HorizontalSlider(HAUPlugin.targetMovementMultiplier.Value, 1f, 4f) * 10f) / 10f;
        if (!Mathf.Approximately(boost, HAUPlugin.targetMovementMultiplier.Value))
            HAUPlugin.targetMovementMultiplier.Value = boost;
        GUILayout.Label("Follow / orbit boost: " + boost.ToString("0.0") + "x");
        UIHelpers.Hint("Distant players use at least 2x to catch up. Walls and closed routes can pause the mode.");
        UIHelpers.EndSection();

        UIHelpers.BeginSection("Zigzag dash", "Move in alternating directions while holding a movement key.");
        DrawMode("Zigzag dash", MovementMode.ZigzagDash);
        var dash = Mathf.Round(GUILayout.HorizontalSlider(HAUPlugin.stuntMultiplier.Value, 1f, 4f) * 10f) / 10f;
        if (!Mathf.Approximately(dash, HAUPlugin.stuntMultiplier.Value))
            HAUPlugin.stuntMultiplier.Value = dash;
        GUILayout.Label("Dash boost: " + dash.ToString("0.0") + "x");
        UIHelpers.EndSection();

        UIHelpers.BeginSection("Lag effect",
            "Mixes normal walking, short pauses, forward jumps and backward corrections. It is a movement effect.");
        DrawMode("Lag mode", MovementMode.LagWalk);
        float interval = Mathf.Round(GUILayout.HorizontalSlider(HAUPlugin.lagInterval.Value, 0.5f, 2f) * 20f) / 20f;
        if (!Mathf.Approximately(interval, HAUPlugin.lagInterval.Value)) HAUPlugin.lagInterval.Value = interval;
        GUILayout.Label("Correction interval: " + interval.ToString("0.00") + " seconds / varies");
        float distance = Mathf.Round(GUILayout.HorizontalSlider(HAUPlugin.lagJumpDistance.Value, 0.25f, 2f) * 20f) / 20f;
        if (!Mathf.Approximately(distance, HAUPlugin.lagJumpDistance.Value)) HAUPlugin.lagJumpDistance.Value = distance;
        GUILayout.Label("Maximum jump: " + distance.ToString("0.00") + " m");
        UIHelpers.Hint("Jumps shorten near walls. Hold a movement key to mix walking with corrections.");
        UIHelpers.EndSection();
        GUILayout.Label(MovementAutomation.StatusText);
        if (!string.IsNullOrEmpty(MovementAutomation.LastNavigationIssue))
            UIHelpers.Hint("Last route issue: " + MovementAutomation.LastNavigationIssue);
        UIHelpers.Hint("Close the menu and chat to move. Only one movement mode runs at a time.");
    }

    internal static void DrawMode(string label, MovementMode mode)
    {
        bool active = MovementAutomation.Mode == mode;
        if (GUILayout.Button((active ? "Stop " : "Start ") + label))
        {
            if (active) MovementAutomation.Stop();
            else MovementAutomation.Start(mode);
        }
    }
}
