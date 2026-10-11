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
        CheatToggles.unlockVents = GUILayout.Toggle(CheatToggles.unlockVents, " Unlock Vents");
        CheatToggles.walkInVents = GUILayout.Toggle(CheatToggles.walkInVents, " Walk in Vents");
        CheatToggles.endlessVentTime = GUILayout.Toggle(CheatToggles.endlessVentTime, " Endless Vent Time");
        CheatToggles.noVentCooldown = GUILayout.Toggle(CheatToggles.noVentCooldown, " No Vent Cooldown");
        UIHelpers.Hint("Unlock Vents offers the vent button; the host checks entry. Walk in Vents releases your local vent movement lock. Timer and cooldown changes require the native Engineer ability.");
        UIHelpers.EndSection();

        UIHelpers.BeginSection("Scientist Vitals");
        CheatToggles.endlessBattery = GUILayout.Toggle(CheatToggles.endlessBattery, " Endless Vitals Battery");
        CheatToggles.noVitalsCooldown = GUILayout.Toggle(CheatToggles.noVitalsCooldown, " No Vitals Cooldown");
        UIHelpers.Hint("These adjust the native Scientist role. Multi Role opens a separate Vitals panel.");
        UIHelpers.EndSection();

        UIHelpers.BeginSection("Tracker");
        CheatToggles.endlessTracking = GUILayout.Toggle(CheatToggles.endlessTracking, " Endless Tracking");
        CheatToggles.noTrackingDelay = GUILayout.Toggle(CheatToggles.noTrackingDelay, " No Tracking Delay");
        CheatToggles.noTrackingCooldown = GUILayout.Toggle(CheatToggles.noTrackingCooldown, " No Tracking Cooldown");
        CheatToggles.trackReach = GUILayout.Toggle(CheatToggles.trackReach, " Infinite Track Reach");
        UIHelpers.Hint("These adjust the native Tracker ability. Track Reach removes the local selection-distance limit; Multi Role uses its own tracking.");
        UIHelpers.EndSection();

        UIHelpers.BeginSection("Detective");
        CheatToggles.interrogateReach = GUILayout.Toggle(CheatToggles.interrogateReach, " Infinite Interrogate Reach");
        UIHelpers.Hint("Applies to the native Detective ability. Multi Role uses its own nearby tracked-player checks.");
        UIHelpers.EndSection();

        UIHelpers.BeginSection("Impostor abilities");
        CheatToggles.killReach = GUILayout.Toggle(CheatToggles.killReach, " Infinite Kill Reach / local targeting");
        CheatToggles.noShapeshiftAnim = GUILayout.Toggle(CheatToggles.noShapeshiftAnim, " No Shapeshift Animation");
        CheatToggles.endlessSsDuration = GUILayout.Toggle(CheatToggles.endlessSsDuration, " Endless Shapeshift Duration / local timer");
        UIHelpers.Hint("Requires an available ability. The host determines whether online actions are accepted.");
        UIHelpers.EndSection();
    }

    private static void DrawPreviews()
    {
        UIHelpers.BeginSection("Local role preview", "Changes the role interface on your own screen. Your host-assigned role remains authoritative.");
        UIHelpers.Hint(PlayerActions.RolePreviewStatus);
        UIHelpers.ChoosePlayer("Fake Role / local preview...", ref CheatToggles.setFakeRole, Utils.isPlayer);
        if (!Utils.isHost && !Utils.isFreePlay)
        {
            RolePreviewControls.DrawAlwaysImpostor(" Impostor preview on my screen");
            UIHelpers.Hint("This preview does not grant counted kills.");
        }
        else UIHelpers.Hint("Hosted Impostor assignment is in Host Controls. Freeplay experiments are in Practice.");
        UIHelpers.EndSection();
    }
}
