using UnityEngine;

namespace HAU;

public class ESPTab : ITab
{
    public string name => "Information";
    private int _page;

    public void Draw()
    {
        _page = UIHelpers.Subtabs(_page, "Player info", "Camera", "Map & lines");
        UIHelpers.Hint("These displays use information available on your client.");
        if (_page == 0)
        {
            UIHelpers.BeginSection("Player labels", "Add details above characters on your screen.");
            CheatToggles.seePlayerInfo = GUILayout.Toggle(CheatToggles.seePlayerInfo, " Player details");
            CheatToggles.seeRoles = GUILayout.Toggle(CheatToggles.seeRoles, " Role labels");
            CheatToggles.seeGhosts = GUILayout.Toggle(CheatToggles.seeGhosts, " Show ghosts");
            UIHelpers.EndSection();
            UIHelpers.BeginSection("Round and lobby");
            CheatToggles.noShadows = GUILayout.Toggle(CheatToggles.noShadows, " Remove vision shadows");
            CheatToggles.revealVotes = GUILayout.Toggle(CheatToggles.revealVotes, " Show who voted for whom");
            CheatToggles.seeLobbyInfo = GUILayout.Toggle(CheatToggles.seeLobbyInfo, " Lobby details");
            UIHelpers.EndSection();
        }
        else if (_page == 1)
        {
            UIHelpers.BeginSection("Camera view");
            CheatToggles.zoomOut = GUILayout.Toggle(CheatToggles.zoomOut, " Wider view / zoom out");
            CheatToggles.freecam = GUILayout.Toggle(CheatToggles.freecam, " Move the camera freely");
            UIHelpers.Hint("Free camera and spectating cannot run together.");
            UIHelpers.ChoosePlayer("Choose player to spectate", ref CheatToggles.spectate, Utils.isPlayer);
            if (CheatToggles.spectate && GUILayout.Button("Stop spectating")) CheatToggles.spectate = false;
            UIHelpers.Hint("Spectating follows the selected player and pauses your movement until you stop.");
            UIHelpers.EndSection();
        }
        else
        {
            UIHelpers.BeginSection("Map markers", "Show the selected groups when you open the map.");
            CheatToggles.mapCrew = GUILayout.Toggle(CheatToggles.mapCrew, " Crewmates on map");
            CheatToggles.mapImps = GUILayout.Toggle(CheatToggles.mapImps, " Impostors on map");
            CheatToggles.mapGhosts = GUILayout.Toggle(CheatToggles.mapGhosts, " Ghosts on map");
            CheatToggles.colorBasedMap = GUILayout.Toggle(CheatToggles.colorBasedMap, " Use each player's color");
            UIHelpers.EndSection();
            UIHelpers.BeginSection("Direction lines", "Draw lines from your player toward the selected groups.");
            CheatToggles.tracersCrew = GUILayout.Toggle(CheatToggles.tracersCrew, " Lines to crewmates");
            CheatToggles.tracersImps = GUILayout.Toggle(CheatToggles.tracersImps, " Lines to impostors");
            CheatToggles.tracersGhosts = GUILayout.Toggle(CheatToggles.tracersGhosts, " Lines to ghosts");
            CheatToggles.tracersBodies = GUILayout.Toggle(CheatToggles.tracersBodies, " Lines to bodies");
            CheatToggles.colorBasedTracers = GUILayout.Toggle(CheatToggles.colorBasedTracers, " Color lines by player");
            CheatToggles.distanceBasedTracers = GUILayout.Toggle(CheatToggles.distanceBasedTracers, " Adjust lines by distance");
            UIHelpers.EndSection();
        }
    }
}
