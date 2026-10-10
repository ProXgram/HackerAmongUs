using UnityEngine;

namespace HAU;

public class RolesTab : ITab
{
    public string name => "Abilities";
    private int _page;

    public void Draw()
    {
        _page = UIHelpers.Subtabs(_page, "Multi Role", "Role settings", "Previews");
        if (_page == 0) DrawBundle();
        else if (_page == 1) DrawRoleSettings();
        else DrawPreviews();
    }

    private static void DrawBundle()
    {
        UIHelpers.BeginSection("Multi Role panel", "Vent access, player tracking, Scientist Vitals and Detective Notes in one panel.");
        bool enabled = GUILayout.Toggle(CheatToggles.multiRole, " Enable Multi Role");
        if (enabled != CheatToggles.multiRole) MultiRoleHandler.SetEnabled(enabled);
        UIHelpers.Hint("Close this menu during a living player's round or Freeplay to use the draggable ability panel.");
        if (CheatToggles.multiRole) GUILayout.Label(MultiRoleHandler.StatusText);
        UIHelpers.EndSection();
        UIHelpers.BeginSection("What each tool does");
        GUILayout.Label("Vitals / Opens the native status display.");
        GUILayout.Label("Track / Select a player, then open their tracking map.");
        GUILayout.Label("Detective Notes / Records deaths observed while enabled, including their recorded rooms.");
        GUILayout.Label("Interrogate / Shows recorded location information for a nearby tracked player.");
        GUILayout.Label("Vent / Offers vent access; the host still validates the request.");
        UIHelpers.Hint("Your assigned role stays the same. Escape and Close Detective Notes close the notebook.");
        UIHelpers.EndSection();
    }

    private static void DrawRoleSettings()
    {
        UIHelpers.Hint("Each setting adjusts an ability available on your client. It does not assign a real online role.");
        UIHelpers.BeginSection("Vents");
        CheatToggles.unlockVents = GUILayout.Toggle(CheatToggles.unlockVents, " Enable vent access");
        CheatToggles.walkInVents = GUILayout.Toggle(CheatToggles.walkInVents, " Move while vented");
        CheatToggles.endlessVentTime = GUILayout.Toggle(CheatToggles.endlessVentTime, " Remove local Engineer vent timer");
        CheatToggles.noVentCooldown = GUILayout.Toggle(CheatToggles.noVentCooldown, " Remove local Engineer vent cooldown");
        UIHelpers.Hint("Engineer timer settings apply to the Engineer ability. Host validation still applies to vent requests.");
        UIHelpers.EndSection();

        UIHelpers.BeginSection("Scientist Vitals");
        CheatToggles.endlessBattery = GUILayout.Toggle(CheatToggles.endlessBattery, " Keep Vitals battery full");
        CheatToggles.noVitalsCooldown = GUILayout.Toggle(CheatToggles.noVitalsCooldown, " Remove local Vitals cooldown");
        UIHelpers.EndSection();

        UIHelpers.BeginSection("Tracker");
        CheatToggles.endlessTracking = GUILayout.Toggle(CheatToggles.endlessTracking, " Keep tracking active");
        CheatToggles.noTrackingDelay = GUILayout.Toggle(CheatToggles.noTrackingDelay, " Remove tracking delay");
        CheatToggles.noTrackingCooldown = GUILayout.Toggle(CheatToggles.noTrackingCooldown, " Remove tracking cooldown");
        CheatToggles.trackReach = GUILayout.Toggle(CheatToggles.trackReach, " Extend tracking selection range");
        UIHelpers.EndSection();

        UIHelpers.BeginSection("Detective");
        CheatToggles.interrogateReach = GUILayout.Toggle(CheatToggles.interrogateReach, " Extend native Interrogate range");
        UIHelpers.Hint("Applies to the native Detective ability. Multi Role uses its own nearby tracked-player checks.");
        UIHelpers.EndSection();

        UIHelpers.BeginSection("Impostor abilities");
        CheatToggles.killReach = GUILayout.Toggle(CheatToggles.killReach, " Extend local kill targeting range");
        CheatToggles.noShapeshiftAnim = GUILayout.Toggle(CheatToggles.noShapeshiftAnim, " Skip shapeshift animation");
        CheatToggles.endlessSsDuration = GUILayout.Toggle(CheatToggles.endlessSsDuration, " Keep shapeshift active locally");
        UIHelpers.Hint("Requires an available ability. The host determines whether online actions are accepted.");
        UIHelpers.EndSection();
    }

    private static void DrawPreviews()
    {
        UIHelpers.BeginSection("Local role preview", "Changes the role interface on your own screen. Your host-assigned role remains authoritative.");
        UIHelpers.ChoosePlayer("Choose a role preview", ref CheatToggles.setFakeRole, Utils.isPlayer);
        if (!Utils.isHost && !Utils.isFreePlay)
        {
            RolePreviewControls.DrawAlwaysImpostor(" Impostor preview on my screen");
            UIHelpers.Hint("This preview does not grant counted kills.");
        }
        else UIHelpers.Hint("Hosted Impostor assignment is in Host Controls. Freeplay experiments are in Practice.");
        UIHelpers.EndSection();
    }
}
