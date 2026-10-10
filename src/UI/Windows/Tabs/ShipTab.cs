using UnityEngine;

namespace HAU;

public class ShipTab : ITab
{
    public string name => "Round tools";

    public void Draw()
    {
        UIHelpers.BeginSection("Meetings and reports");
        if (!Utils.isHost && !Utils.isFreePlay)
            UIHelpers.Action("Request emergency meeting", ref CheatToggles.callMeeting, Utils.isShip && !Utils.isMeeting);
        UIHelpers.Action("Close meeting on my screen", ref CheatToggles.closeMeeting, Utils.isMeeting || Utils.isExiling);
        UIHelpers.Hint("Closing your meeting screen does not finish the meeting for everyone.");
        CheatToggles.autoReportBodies = GUILayout.Toggle(CheatToggles.autoReportBodies, " Report nearby bodies automatically");
        CheatToggles.autoOpenDoorsOnUse = GUILayout.Toggle(CheatToggles.autoOpenDoorsOnUse, " Open doors when I use them");
        UIHelpers.EndSection();
        UIHelpers.BeginSection("Sabotage requests", "Role, map and host checks still apply. A switch requests sabotage or repair.");
        CheatToggles.reactorSab = GUILayout.Toggle(CheatToggles.reactorSab, " Reactor / seismic");
        CheatToggles.oxygenSab = GUILayout.Toggle(CheatToggles.oxygenSab, " Oxygen");
        CheatToggles.elecSab = GUILayout.Toggle(CheatToggles.elecSab, " Lights");
        CheatToggles.commsSab = GUILayout.Toggle(CheatToggles.commsSab, " Communications");
        UIHelpers.Action("Trigger mushroom mixup / Fungle", ref CheatToggles.mushSab, Utils.isShip);
        UIHelpers.Action("Trigger spores / Fungle", ref CheatToggles.mushSpore, Utils.isShip);
        UIHelpers.Action("Open sabotage map", ref CheatToggles.sabotageMap, Utils.isShip);
        CheatToggles.showDoorsMenu = GUILayout.Toggle(CheatToggles.showDoorsMenu, " Open door controls window");
        UIHelpers.EndSection();
        UIHelpers.Hint("Vent controls are in Abilities. Ghost Voting and shared rules are in Host Controls.");
    }
}
