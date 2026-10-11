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
        RolePreviewControls.DrawAlwaysImpostor(" Always Impostor / host or Freeplay");
        UIHelpers.Hint("When hosting, applies at the next round start. Freeplay uses its local practice role.");
        UIHelpers.EndSection();
        UIHelpers.BeginSection("Round rules");
        CheatToggles.noGameEnd = GUILayout.Toggle(CheatToggles.noGameEnd, " No Game End");
        CheatToggles.unfixableLights = GUILayout.Toggle(CheatToggles.unfixableLights, " Unfixable Lights");
        UIHelpers.Hint("No Game End suspends normal win checks. Turn Unfixable Lights off to release that lights override.");
        UIHelpers.Action("Force Start", ref CheatToggles.forceStartGame, Utils.isLobby);
        UIHelpers.EndSection();
        UIHelpers.BeginSection("Kill rules");
        CheatToggles.noKillCd = GUILayout.Toggle(CheatToggles.noKillCd, " No Kill Cooldown");
        CheatToggles.killAnyone = GUILayout.Toggle(CheatToggles.killAnyone, " Kill Anyone");
        CheatToggles.killVanished = GUILayout.Toggle(CheatToggles.killVanished, " Kill While Vanished");
        UIHelpers.Hint("These change your hosted kill controls. Kill Anyone expands eligible targets, including teammates; Kill While Vanished affects Phantom targeting.");
        CheatToggles.showProtectMenu = GUILayout.Toggle(CheatToggles.showProtectMenu, " Open player protection window");
        UIHelpers.EndSection();
    }

    private static void DrawMeetings()
    {
        UIHelpers.BeginSection("Meeting actions");
        UIHelpers.Action("Force Meeting", ref CheatToggles.callMeeting, OwnedActionContext.StartMeeting);
        UIHelpers.Action("Skip Meeting / everyone", ref CheatToggles.skipMeeting, OwnedActionContext.HostMeetingAction);
        UIHelpers.ChoosePlayer("Eject Player...", ref CheatToggles.ejectPlayer, OwnedActionContext.HostMeetingAction);
        CheatToggles.voteImmune = GUILayout.Toggle(CheatToggles.voteImmune, " Vote Immune / me");
        UIHelpers.Hint("Eject and End meeting are available during a meeting.");
        UIHelpers.EndSection();
    }

    private static void DrawPlayerActions()
    {
        UIHelpers.BeginSection("Choose a player", "Opens the player selection screen. Kill actions are unavailable in the lobby.");
        UIHelpers.ChoosePlayer("Kill Player...", ref CheatToggles.killPlayer, OwnedActionContext.HostRoundAction);
        UIHelpers.ChoosePlayer("Telekill Player...", ref CheatToggles.telekillPlayer, OwnedActionContext.HostRoundAction);
        UIHelpers.Hint("Telekill returns you to your previous position after the selected kill action.");
        UIHelpers.EndSection();
        UIHelpers.BeginSection("Group actions");
        UIHelpers.Action("Kill all crewmates", ref CheatToggles.killAllCrew, OwnedActionContext.HostRoundAction);
        UIHelpers.Action("Kill all impostors", ref CheatToggles.killAllImps, OwnedActionContext.HostRoundAction);
        UIHelpers.Action("Kill everyone", ref CheatToggles.killAll, OwnedActionContext.HostRoundAction);
        UIHelpers.Action("Kick From Vents / once", ref CheatToggles.kickVents, OwnedActionContext.HostRoundAction);
        UIHelpers.Hint("Kick From Vents processes all current vents once. It does not keep running.");
        UIHelpers.EndSection();
    }
}
