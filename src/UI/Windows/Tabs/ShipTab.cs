using UnityEngine;

namespace HAU;

public class ShipTab : ITab
{
    public string name => "Round tools";

    public void Draw()
    {
        UIHelpers.BeginSection("Meetings and reports");
        if (!Utils.isHost && !Utils.isFreePlay)
            UIHelpers.Action("Request emergency meeting", ref CheatToggles.callMeeting, OwnedActionContext.StartMeeting);
        UIHelpers.Action(Utils.isExiling ? "Skip Ejection / my screen" : "Close Meeting / my screen",
            ref CheatToggles.closeMeeting, Utils.isMeeting || Utils.isExiling);
        UIHelpers.Hint("Closing your meeting screen does not finish the meeting for everyone.");
        CheatToggles.autoReportBodies = GUILayout.Toggle(CheatToggles.autoReportBodies, " Auto Report Bodies");
        CheatToggles.autoOpenDoorsOnUse = GUILayout.Toggle(CheatToggles.autoOpenDoorsOnUse, " Auto Open Door Minigames");
        UIHelpers.Hint("Auto Report sends report requests for detected bodies. Auto Open skips supported door minigames when you interact with them.");
        UIHelpers.EndSection();
        SabotageTools.SyncForMenu();
        UIHelpers.BeginSection("Sabotage requests", "Role, map and host checks still apply. A switch requests sabotage or repair.");
        CheatToggles.reactorSab = GUILayout.Toggle(CheatToggles.reactorSab, " Reactor / seismic");
        CheatToggles.oxygenSab = GUILayout.Toggle(CheatToggles.oxygenSab, " Oxygen");
        CheatToggles.elecSab = GUILayout.Toggle(CheatToggles.elecSab, " Lights");
        CheatToggles.commsSab = GUILayout.Toggle(CheatToggles.commsSab, " Communications");
        UIHelpers.Action("Trigger mushroom mixup / Fungle", ref CheatToggles.mushSab, Utils.isShip);
        UIHelpers.Action("Trigger spores / Fungle", ref CheatToggles.mushSpore, Utils.isShip);
        bool mapReady = OwnedActionContext.GameplayReady && !Utils.isMeeting && Utils.isNormalGame;
        UIHelpers.Action("Open sabotage map", ref CheatToggles.sabotageMap, mapReady);
        if (CheatToggles.sabotageMap && mapReady) MenuUI.CloseMenuFromPointer();
        CheatToggles.showDoorsMenu = GUILayout.Toggle(CheatToggles.showDoorsMenu, " Open door controls window");
        UIHelpers.Hint(SabotageTools.StatusText);
        UIHelpers.EndSection();
        UIHelpers.Hint("Vent controls are in Abilities. Ghost Voting and shared rules are in Host Controls.");
    }
}
