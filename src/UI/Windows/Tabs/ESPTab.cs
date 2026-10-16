using UnityEngine;

namespace HAU;

public class ESPTab : ITab, IMenuPage
{
    public string name => "ESP";
    private int _page;

    public void OpenPage(int page) => _page = System.Math.Clamp(page, 0, 2);

    public void Draw()
    {
        _page = UIHelpers.Subtabs(_page, "Player ESP", "Camera", "Map & tracers");
        UIHelpers.Hint("ESP displays client data on your own screen.");
        if (_page == 0)
        {
            UIHelpers.BeginSection("Player labels", "Add details above characters on your screen.");
            CheatToggles.seePlayerInfo = GUILayout.Toggle(CheatToggles.seePlayerInfo, " Player Info ESP");
            CheatToggles.seeRoles = GUILayout.Toggle(CheatToggles.seeRoles, " Role ESP");
            CheatToggles.seeGhosts = GUILayout.Toggle(CheatToggles.seeGhosts, " Ghost ESP");
            UIHelpers.Hint("Player Info adds host, level and platform. Ghost ESP also shows received ghost chat and protection effects.");
            UIHelpers.EndSection();
            UIHelpers.BeginSection("Round and lobby");
            CheatToggles.noShadows = GUILayout.Toggle(CheatToggles.noShadows, " Fullbright / No Shadows");
            CheatToggles.revealVotes = GUILayout.Toggle(CheatToggles.revealVotes, " Reveal Votes");
            CheatToggles.seeLobbyInfo = GUILayout.Toggle(CheatToggles.seeLobbyInfo, " Lobby Browser Info");
            UIHelpers.Hint("Lobby Browser Info adds the host name, code, platform and lobby age to refreshed public lobby listings.");
            UIHelpers.EndSection();
        }
        else if (_page == 1)
        {
            UIHelpers.BeginSection("Camera view");
            CheatToggles.zoomOut = GUILayout.Toggle(CheatToggles.zoomOut, " Mouse-Wheel Zoom");
            UIHelpers.Hint("Enable zoom, close the menu, then scroll down to zoom out or up to zoom in. Turning it off restores the normal view.");
            CheatToggles.freecam = GUILayout.Toggle(CheatToggles.freecam, " Freecam");
            if (CheatToggles.freecam) CheatToggles.spectate = false;
            UIHelpers.Hint("Freecam moves the camera with your movement keys and pauses your player.");
            if (!CheatToggles.spectate)
            {
                bool previous = GUI.enabled;
                GUI.enabled = previous && !CheatToggles.freecam;
                UIHelpers.ChoosePlayer("Spectate Player...", ref CheatToggles.spectate, Utils.isPlayer);
                GUI.enabled = previous;
            }
            else if (GUILayout.Button("Stop Spectating")) CheatToggles.spectate = false;
            UIHelpers.Hint("Spectate follows one selected player. Stop it before choosing another player; turn Freecam off before selecting.");
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
            UIHelpers.BeginSection("Tracers", "Draw lines from your player toward the selected groups.");
            CheatToggles.tracersCrew = GUILayout.Toggle(CheatToggles.tracersCrew, " Lines to crewmates");
            CheatToggles.tracersImps = GUILayout.Toggle(CheatToggles.tracersImps, " Lines to impostors");
            CheatToggles.tracersGhosts = GUILayout.Toggle(CheatToggles.tracersGhosts, " Lines to ghosts");
            CheatToggles.tracersBodies = GUILayout.Toggle(CheatToggles.tracersBodies, " Lines to bodies");
            CheatToggles.colorBasedTracers = GUILayout.Toggle(CheatToggles.colorBasedTracers, " Player-Colored Tracers");
            CheatToggles.distanceBasedTracers = GUILayout.Toggle(CheatToggles.distanceBasedTracers, " Distance-Colored Tracers");
            UIHelpers.Hint("Distance colors take priority: red nearby, yellow midway, green far away. With neither option, colors follow teams.");
            UIHelpers.EndSection();
        }
    }
}
