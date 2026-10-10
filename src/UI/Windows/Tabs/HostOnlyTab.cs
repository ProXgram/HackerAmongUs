using UnityEngine;

namespace HAU;

public class HostOnlyTab : ITab
{
    public string name => "Host Controls";
    private int _page;

    public void Draw()
    {
        _page = UIHelpers.Subtabs(_page, "Lobby rules", "Meetings", "Player actions");
        bool host = Utils.isHost || Utils.isFreePlay;
        UIHelpers.Hint(Utils.isFreePlay ? "Host controls are available in Freeplay."
            : Utils.isHost ? "You are hosting. These settings affect the shared game."
            : "Host controls unlock when you host a lobby or enter Freeplay.");

        // A ghost must opt into its local controls; only a supporting host counts votes.
        if (_page == 0 || _page == 1)
        {
            UIHelpers.BeginSection("Ghost Voting", "The host and each voting ghost need this mod enabled.");
            CheatToggles.ghostVoting = GUILayout.Toggle(CheatToggles.ghostVoting,
                host ? " Allow ghost votes in my lobby" : " Enable my ghost vote controls");
            UIHelpers.Hint(host ? "Ask each voting ghost to enable their controls. Multiplayer vote counting remains untested."
                : "Your host must allow ghost votes. Enabling this alone does not make your vote count.");
            UIHelpers.EndSection();
        }

        bool previous = GUI.enabled;
        GUI.enabled = previous && host;
        try
        {
            if (_page == 0) DrawRules();
            else if (_page == 1) DrawMeetings();
            else DrawPlayerActions();
        }
        finally { GUI.enabled = previous; }
    }

    private static void DrawRules()
    {
        UIHelpers.BeginSection("My role assignment", "Set your own role before the round starts.");
        RolePreviewControls.DrawAlwaysImpostor(" Assign me Impostor / host or Freeplay");
        UIHelpers.EndSection();
        UIHelpers.BeginSection("Round rules");
        CheatToggles.noGameEnd = GUILayout.Toggle(CheatToggles.noGameEnd, " Keep the round running after win conditions");
        CheatToggles.unfixableLights = GUILayout.Toggle(CheatToggles.unfixableLights, " Make lights unfixable until disabled");
        UIHelpers.Action("Start the lobby now", ref CheatToggles.forceStartGame, Utils.isLobby);
        UIHelpers.EndSection();
        UIHelpers.BeginSection("Kill rules");
        CheatToggles.noKillCd = GUILayout.Toggle(CheatToggles.noKillCd, " Remove my kill cooldown");
        CheatToggles.killAnyone = GUILayout.Toggle(CheatToggles.killAnyone, " Allow targeting teammates and protected players");
        CheatToggles.killVanished = GUILayout.Toggle(CheatToggles.killVanished, " Allow kills while vanished");
        CheatToggles.showProtectMenu = GUILayout.Toggle(CheatToggles.showProtectMenu, " Open player protection window");
        UIHelpers.EndSection();
    }

    private static void DrawMeetings()
    {
        UIHelpers.BeginSection("Meeting actions");
        UIHelpers.Action("Start emergency meeting", ref CheatToggles.callMeeting, Utils.isShip && !Utils.isMeeting);
        UIHelpers.Action("End meeting for everyone / skip", ref CheatToggles.skipMeeting, Utils.isMeeting);
        UIHelpers.ChoosePlayer("Choose player to eject", ref CheatToggles.ejectPlayer, Utils.isMeeting);
        CheatToggles.voteImmune = GUILayout.Toggle(CheatToggles.voteImmune, " Prevent votes from ejecting me");
        UIHelpers.Hint("Eject and End meeting are available during a meeting.");
        UIHelpers.EndSection();
    }

    private static void DrawPlayerActions()
    {
        UIHelpers.BeginSection("Choose a player", "Opens the player selection screen. Kill actions are unavailable in the lobby.");
        UIHelpers.ChoosePlayer("Choose player to kill", ref CheatToggles.killPlayer, Utils.isShip && !Utils.isLobby);
        UIHelpers.ChoosePlayer("Choose player to kill and return", ref CheatToggles.telekillPlayer, Utils.isShip && !Utils.isLobby);
        UIHelpers.EndSection();
        UIHelpers.BeginSection("Group actions");
        UIHelpers.Action("Kill all crewmates", ref CheatToggles.killAllCrew, Utils.isShip && !Utils.isLobby);
        UIHelpers.Action("Kill all impostors", ref CheatToggles.killAllImps, Utils.isShip && !Utils.isLobby);
        UIHelpers.Action("Kill everyone", ref CheatToggles.killAll, Utils.isShip && !Utils.isLobby);
        CheatToggles.kickVents = GUILayout.Toggle(CheatToggles.kickVents, " Keep players out of vents");
        UIHelpers.Hint("Vent clearing stays active until you turn it off.");
        UIHelpers.EndSection();
    }
}
