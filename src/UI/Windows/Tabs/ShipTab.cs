using UnityEngine;

namespace HAU;

public class ShipTab : ITab, IMenuPage
{
    public string name => "Sabotage";
    private int _page;

    public void OpenPage(int page) => _page = System.Math.Clamp(page, 0, 1);

    public void Draw()
    {
        _page = UIHelpers.Subtabs(_page, "Sabotage & doors", "Meetings & reports");
        if (_page == 1) DrawMeetings();
        else DrawSabotage();
    }

    private static void DrawMeetings()
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
        UIHelpers.Hint("Shared meeting controls and Ghost Voting are in Host Controls.");
    }

    private static void DrawSabotage()
    {
        SabotageTools.SyncForMenu();
        UIHelpers.BeginSection("Sabotage & repair", "Choose an action. The status shows the game state; online requests still need host acceptance.");
        DrawSystem("Reactor / seismic", SabotageKind.Reactor);
        DrawSystem("Oxygen", SabotageKind.Oxygen);
        DrawSystem("Lights", SabotageKind.Lights);
        DrawSystem("Communications", SabotageKind.Communications);
        UIHelpers.Hint(SabotageTools.StatusText);
        UIHelpers.EndSection();
        UIHelpers.BeginSection("Map & doors");
        bool mapReady = OwnedActionContext.GameplayReady && !Utils.isMeeting && Utils.isNormalGame;
        UIHelpers.Action("Open sabotage map", ref CheatToggles.sabotageMap, mapReady);
        if (CheatToggles.sabotageMap && mapReady) MenuUI.CloseMenuFromPointer();
        CheatToggles.showDoorsMenu = GUILayout.Toggle(CheatToggles.showDoorsMenu, " Open door controls window");
        UIHelpers.EndSection();
        UIHelpers.BeginSection("Fungle effects");
        UIHelpers.Action("Trigger mushroom mixup / Fungle", ref CheatToggles.mushSab, Utils.isShip);
        UIHelpers.Action("Trigger spores / Fungle", ref CheatToggles.mushSpore, Utils.isShip);
        UIHelpers.EndSection();
    }

    private static void DrawSystem(string label, SabotageKind kind)
    {
        bool available = SabotageTools.IsAvailable(kind);
        bool active = available && SabotageTools.IsActive(kind);
        bool ready = available && OwnedActionContext.GameplayReady && !Utils.isMeeting && Utils.isNormalGame;
        bool horizontal = MenuUI.ContentWidth >= 350f;
        if (horizontal) GUILayout.BeginHorizontal();
        GUILayout.Label(label + ": " + (!available ? "unavailable" : active ? "active" : "clear"));
        bool previous = GUI.enabled;
        try
        {
            GUI.enabled = previous && ready && !active;
            if (GUILayout.Button("Activate", GUILayout.Width(horizontal ? 82f : MenuUI.ContentWidth - 22f)))
                SabotageTools.Request(kind, true);
            GUI.enabled = previous && ready && active;
            if (GUILayout.Button("Repair", GUILayout.Width(horizontal ? 72f : MenuUI.ContentWidth - 22f)))
                SabotageTools.Request(kind, false);
        }
        finally { GUI.enabled = previous; }
        if (horizontal) GUILayout.EndHorizontal();
    }
}
