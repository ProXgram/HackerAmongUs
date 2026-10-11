using Il2CppSystem.Collections.Generic;
using BepInEx.Unity.IL2CPP.Utils;
using System;
using AmongUs.GameOptions;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace HAU;
public static class PlayerActions
{
    private static bool _telekillPlayerActive;
    private static bool _killPlayerActive;
    private static bool _spectateActive;
    private static bool _teleportPlayerActive;
    private static bool _reportBodyActive;
    private static bool _ejectPlayerActive;
    private static bool _setFakeRoleActive;
    private static bool _setFakeAliveActive;
    private static bool _forceRoleActive;
    private static RoleTypes? _oldRole;
    private static OwnedActionContext.Snapshot? _previewContext;
    private static IntPtr _previewRolePointer;
    public static string RolePreviewStatus { get; private set; } = "Role previews do not grant a real online assignment.";

    public static void ResetSession()
    {
        _telekillPlayerActive = _killPlayerActive = _spectateActive = _teleportPlayerActive = false;
        _reportBodyActive = _ejectPlayerActive = _setFakeRoleActive = _setFakeAliveActive = _forceRoleActive = false;
        _oldRole = null; _previewContext = null; _previewRolePointer = IntPtr.Zero;
        RolePreviewStatus = "Role previews do not grant a real online assignment.";
        CheatToggles.freecam = false;
        CameraToolHandler.StopSpectate();
        CameraToolHandler.Reset();
    }

    private static void RefreshPreviewSession()
    {
        if (!OwnedActionContext.TryPlayer(out var local) || !_previewContext.HasValue ||
            !_previewContext.Value.IsCurrent || (HudManager.InstanceExists && HudManager.Instance.IsIntroDisplayed))
        {
            _oldRole = null; _previewContext = null; _previewRolePointer = IntPtr.Zero;
            _setFakeRoleActive = false;
            return;
        }
        if (local.Data.Role && _previewRolePointer != IntPtr.Zero && local.Data.Role.Pointer != _previewRolePointer)
        {
            if (!local.Data.IsDead && !LocalImpostorHandler.BlocksKillRequests(local)) _oldRole = local.Data.RoleType;
            _previewRolePointer = local.Data.Role.Pointer;
        }
    }

    private static bool ChoiceIsCurrent(OwnedActionContext.Snapshot context, bool hostMeeting = false,
        bool hostRound = false) => context.IsCurrent &&
        (hostMeeting ? OwnedActionContext.HostMeetingAction : hostRound ? OwnedActionContext.HostRoundAction
            : OwnedActionContext.CanPickPlayer);

    internal static bool SpecialRoleAllowed(RoleTypes? original, RoleTypes choice, bool practice, bool host)
    {
        if (practice) return true;
        if (host && (!original.HasValue || Utils.GetBehaviourByRoleType(original.Value).TeamType !=
            Utils.GetBehaviourByRoleType(choice).TeamType)) return false;
        if (choice is RoleTypes.Shapeshifter or RoleTypes.Phantom or RoleTypes.Viper or RoleTypes.Judge)
            return original == choice;
        if (choice == RoleTypes.Impostor)
            return original.HasValue && Utils.GetBehaviourByRoleType(original.Value).TeamType == RoleTeamTypes.Impostor;
        return choice is RoleTypes.Tracker or RoleTypes.Noisemaker or RoleTypes.Engineer or RoleTypes.Scientist
            or RoleTypes.Detective or RoleTypes.Crewmate;
    }


    public static void ReportBodyPPM()
    {
        if (!(OwnedActionContext.StartMeeting)) { CheatToggles.reportBody = false; _reportBodyActive = false; return; }
        if (CheatToggles.reportBody)
        {

            if (!_reportBodyActive)
            {
                // Close any player pick menus already open & their cheats
                if (PlayerPickMenu.playerpickMenu != null)
                {
                    PlayerPickMenu.playerpickMenu.Close();
                    CheatToggles.DisablePPMCheats("reportBody");
                }

                var context = new OwnedActionContext.Snapshot(PlayerControl.LocalPlayer);
                // A report still uses the normal native request and an observed dead player.
                PlayerPickMenu.OpenPlayerPickMenu(Utils.GetAllPlayerData(), (Action) (() =>
                {
                    if (!ChoiceIsCurrent(context) || !OwnedActionContext.StartMeeting) return;
                    var target = PlayerPickMenu.targetPlayerData;
                    if (target == null || target.Disconnected || !target.IsDead) return;
                    PlayerControl.LocalPlayer.CmdReportDeadBody(target);
                }));

                _reportBodyActive = true;
            }

            // Deactivate cheat if menu is closed
            if (PlayerPickMenu.playerpickMenu == null)
            {
                CheatToggles.reportBody = false;
            }

        }
        else
        {
            if (_reportBodyActive)
            {
                _reportBodyActive = false;
            }
        }
    }

    public static void EjectPlayerPPM()
    {
        if (!(OwnedActionContext.HostMeetingAction)) { CheatToggles.ejectPlayer = false; _ejectPlayerActive = false; return; }
        if (CheatToggles.ejectPlayer)
        {
            if (!_ejectPlayerActive)
            {
                // Close any player pick menus already open & their cheats
                if (PlayerPickMenu.playerpickMenu != null)
                {
                    PlayerPickMenu.playerpickMenu.Close();
                    CheatToggles.DisablePPMCheats("ejectPlayer");
                }

                if (!Utils.isMeeting)
                {
                    CheatToggles.ejectPlayer = false;
                    return;
                }

                List<NetworkedPlayerInfo> playerInfo = new List<NetworkedPlayerInfo>();
                foreach (var player in PlayerControl.AllPlayerControls)
                {
                    if (!player.Data.IsDead && !player.Data.Disconnected)
                    {
                        playerInfo.Add(player.Data);
                    }
                }

                var context = new OwnedActionContext.Snapshot(PlayerControl.LocalPlayer);
                var meetingPointer = MeetingHud.Instance.Pointer;
                // Player pick menu to choose any living player and eject them during meeting
                PlayerPickMenu.OpenPlayerPickMenu(playerInfo, (Action)(() =>
                {
                    if (!ChoiceIsCurrent(context, hostMeeting: true) || MeetingHud.Instance.Pointer != meetingPointer) return;
                    NetworkedPlayerInfo playerToEject = PlayerPickMenu.targetPlayerData;
                    if (playerToEject == null || playerToEject.Disconnected || playerToEject.IsDead) return;
                    MeetingHud.Instance.RpcVotingComplete(new Il2CppStructArray<MeetingHud.VoterState>(0L), playerToEject, false, false, ushort.MinValue);
                }));

                _ejectPlayerActive = true;
            }

            // Deactivate cheat if menu is closed
            if (PlayerPickMenu.playerpickMenu == null)
            {
                CheatToggles.ejectPlayer = false;
            }
        }
        else if (_ejectPlayerActive)
        {
            _ejectPlayerActive = false;
        }
    }

    public static void KillPlayerPPM()
    {
        if (!(OwnedActionContext.HostRoundAction)) { CheatToggles.killPlayer = false; _killPlayerActive = false; return; }
        if (CheatToggles.killPlayer)
        {
            if (!_killPlayerActive)
            {
                // Close any player pick menus already open & their cheats
                if (PlayerPickMenu.playerpickMenu != null)
                {
                    PlayerPickMenu.playerpickMenu.Close();
                    CheatToggles.DisablePPMCheats("killPlayer");
                }

                if (Utils.isLobby)
                {
                    HudManager.Instance.Notifier.AddDisconnectMessage("Killing in lobby disabled for being too buggy");
                    CheatToggles.killPlayer = false;
                    return;
                }

                var context = new OwnedActionContext.Snapshot(PlayerControl.LocalPlayer);
                // Player pick menu made for killing any player by sending a successful MurderPlayer RPC call
                PlayerPickMenu.OpenPlayerPickMenu(Utils.GetAllPlayerData(), (Action)(() =>
                {
                    if (!ChoiceIsCurrent(context, hostRound: true)) return;
                    var target = PlayerPickMenu.targetPlayerData?.Object;
                    if (!OwnedActionContext.ValidTarget(target)) return;
                    Utils.MurderPlayer(target, MurderResultFlags.Succeeded);
                }));

                _killPlayerActive = true;
            }

            // Deactivate cheat if menu is closed
            if (PlayerPickMenu.playerpickMenu == null)
            {
                CheatToggles.killPlayer = false;
            }
        }
        else if (_killPlayerActive)
        {
            _killPlayerActive = false;
        }
    }

    public static void TelekillPlayerPPM()
    {
        if (!(OwnedActionContext.HostRoundAction)) { CheatToggles.telekillPlayer = false; _telekillPlayerActive = false; return; }
        if (CheatToggles.telekillPlayer)
        {
            if (!_telekillPlayerActive)
            {
                // Close any player pick menus already open & their cheats
                if (PlayerPickMenu.playerpickMenu != null)
                {
                    PlayerPickMenu.playerpickMenu.Close();
                    CheatToggles.DisablePPMCheats("telekillPlayer");
                }

                if (Utils.isLobby)
                {
                    HudManager.Instance.Notifier.AddDisconnectMessage("Killing in lobby disabled for being too buggy");
                    CheatToggles.telekillPlayer = false;
                    return;
                }

                var context = new OwnedActionContext.Snapshot(PlayerControl.LocalPlayer);
                // Player pick menu made for killing any player by sending a successful MurderPlayer RPC call
                // and immediatly teleporting back to original position
                PlayerPickMenu.OpenPlayerPickMenu(Utils.GetAllPlayerData(), (Action)(() =>
                {
                    if (!ChoiceIsCurrent(context, hostRound: true)) return;
                    if (LocalImpostorHandler.BlockOwnKill(PlayerControl.LocalPlayer)) return;
                    var oldPos = PlayerControl.LocalPlayer.GetTruePosition();
                    var target = PlayerPickMenu.targetPlayerData?.Object;
                    if (!OwnedActionContext.ValidTarget(target)) return;
                    Utils.MurderPlayer(target, MurderResultFlags.Succeeded);
                    AmongUsClient.Instance.StartCoroutine(ReturnAfterKill(context, oldPos));
                }));

                _telekillPlayerActive = true;
            }

            // Deactivate cheat if menu is closed
            if (PlayerPickMenu.playerpickMenu == null)
            {
                CheatToggles.telekillPlayer = false;
            }
        }
        else if (_telekillPlayerActive)
        {
            _telekillPlayerActive = false;
        }
    }

    public static void TeleportPlayerPPM()
    {
        if (!(OwnedActionContext.CanPickPlayer)) { CheatToggles.teleportPlayer = false; _teleportPlayerActive = false; return; }
        if (CheatToggles.teleportPlayer)
        {
            if (!_teleportPlayerActive)
            {
                // Close any player pick menus already open & their cheats
                if (PlayerPickMenu.playerpickMenu != null)
                {
                    PlayerPickMenu.playerpickMenu.Close();
                    CheatToggles.DisablePPMCheats("teleportPlayer");
                }

                List<NetworkedPlayerInfo> playerDataList = new List<NetworkedPlayerInfo>();

                // All players are saved to playerList apart from LocalPlayer
                foreach (var player in PlayerControl.AllPlayerControls)
                {
                    if (OwnedActionContext.ValidTarget(player, false) && !player.AmOwner)
                    {
                        playerDataList.Add(player.Data);
                    }
                }

                // Player pick menu made for teleporting LocalPlayer to any player's position
                var context = new OwnedActionContext.Snapshot(PlayerControl.LocalPlayer);
                PlayerPickMenu.OpenPlayerPickMenu(playerDataList, (Action)(() =>
                {
                    if (!ChoiceIsCurrent(context) || !Application.isFocused) return;
                    var local = PlayerControl.LocalPlayer;
                    var target = PlayerPickMenu.targetPlayerData?.Object;
                    if (!OwnedActionContext.ValidTarget(target, false) || local.inVent || local.onLadder ||
                        local.inMovingPlat || !local.NetTransform) return;
                    local.NetTransform.RpcSnapTo(target.GetTruePosition());
                }));

                _teleportPlayerActive = true;
            }

            // Deactivate cheat if menu is closed
            if (PlayerPickMenu.playerpickMenu == null)
            {
                CheatToggles.teleportPlayer = false;
            }
        }
        else if (_teleportPlayerActive)
        {
            _teleportPlayerActive = false;
        }
    }

    public static void SetFakeRolePPM()
    {
        RefreshPreviewSession();
        if (!(OwnedActionContext.CanPickPlayer)) { CheatToggles.setFakeRole = false; _setFakeRoleActive = false; return; }
        if (CheatToggles.setFakeRole)
        {

            if (!_setFakeRoleActive)
            {

                // Close any player pick menus already open & their cheats
                if (PlayerPickMenu.playerpickMenu != null)
                {
                    PlayerPickMenu.playerpickMenu.Close();
                    CheatToggles.DisablePPMCheats("setFakeRole");
                }

                var local = PlayerControl.LocalPlayer;
                if (!_previewContext.HasValue)
                {
                    _oldRole = LocalImpostorHandler.BlocksKillRequests(local) ? null
                        : local.Data.IsDead && local.Data.RoleWhenAlive.HasValue
                            ? local.Data.RoleWhenAlive.Value : local.Data.RoleType;
                    _previewContext = new OwnedActionContext.Snapshot(local);
                    _previewRolePointer = local.Data.Role ? local.Data.Role.Pointer : IntPtr.Zero;
                }
                var context = new OwnedActionContext.Snapshot(local);
                List<NetworkedPlayerInfo> playerDataList = new List<NetworkedPlayerInfo>();

                // Shapeshifter role can only be used if it was already assigned at the start of the game
                // This is done to prevent the anticheat from kicking players
                if (_oldRole == RoleTypes.Shapeshifter || Utils.isFreePlay)
                {
                    playerDataList.Add(PlayerPickMenu.CustomPPMChoice("Shapeshifter", OutfitPreset.Shapeshifter, Utils.GetBehaviourByRoleType(RoleTypes.Shapeshifter)));
                }

                // Phantom role can only be used if it was already assigned at the start of the game
                // This is done to prevent the anticheat from kicking players
                if (_oldRole == RoleTypes.Phantom || Utils.isFreePlay)
                {
                    playerDataList.Add(PlayerPickMenu.CustomPPMChoice("Phantom", OutfitPreset.Phantom, Utils.GetBehaviourByRoleType(RoleTypes.Phantom)));
                }

                // Viper role can only be used if it was already assigned at the start of the game
                // This is done to prevent the anticheat from kicking players
                if (_oldRole == RoleTypes.Viper || Utils.isFreePlay)
                {
                    playerDataList.Add(PlayerPickMenu.CustomPPMChoice("Viper", OutfitPreset.Viper, Utils.GetBehaviourByRoleType(RoleTypes.Viper)));
                }

                // Impostor role can only be used if it was already assigned at the start of the game or as host
                // This is done to prevent the anticheat from kicking players
                if ((_oldRole != null && Utils.GetBehaviourByRoleType((RoleTypes)_oldRole).TeamType == RoleTeamTypes.Impostor) || Utils.isFreePlay || Utils.isHost)
                {
                    playerDataList.Add(PlayerPickMenu.CustomPPMChoice("Impostor", OutfitPreset.Impostor, Utils.GetBehaviourByRoleType(RoleTypes.Impostor)));
                }

                // Judge role can only be used if it was already assigned at the start of the game
                // This is done to prevent the anticheat from kicking players
                if (_oldRole == RoleTypes.Judge || Utils.isFreePlay)
                {
                    playerDataList.Add(PlayerPickMenu.CustomPPMChoice("Judge", OutfitPreset.Judge, Utils.GetBehaviourByRoleType(RoleTypes.Judge)));
                }

                playerDataList.Add(PlayerPickMenu.CustomPPMChoice("Tracker", OutfitPreset.Tracker, Utils.GetBehaviourByRoleType(RoleTypes.Tracker)));
                playerDataList.Add(PlayerPickMenu.CustomPPMChoice("Noisemaker", OutfitPreset.Noisemaker, Utils.GetBehaviourByRoleType(RoleTypes.Noisemaker)));
                playerDataList.Add(PlayerPickMenu.CustomPPMChoice("Engineer", OutfitPreset.Engineer, Utils.GetBehaviourByRoleType(RoleTypes.Engineer)));
                playerDataList.Add(PlayerPickMenu.CustomPPMChoice("Scientist", OutfitPreset.Scientist, Utils.GetBehaviourByRoleType(RoleTypes.Scientist)));
                playerDataList.Add(PlayerPickMenu.CustomPPMChoice("Detective", OutfitPreset.Detective, Utils.GetBehaviourByRoleType(RoleTypes.Detective)));
                playerDataList.Add(PlayerPickMenu.CustomPPMChoice("Crewmate", OutfitPreset.Crewmate, Utils.GetBehaviourByRoleType(RoleTypes.Crewmate)));

                for (var i = playerDataList.Count - 1; i >= 0; i--)
                    if (!SpecialRoleAllowed(_oldRole, playerDataList[i].Role.Role, Utils.isFreePlay, Utils.isHost))
                        playerDataList.RemoveAt(i);
                RolePreviewStatus = Utils.isFreePlay ? "Freeplay role experiments are available."
                    : Utils.isHost ? "Online host previews keep your assigned team. Use Host Controls for balanced Impostor assignment."
                    : "Local role preview only. Special roles require your original assignment; online actions still need host acceptance.";

                // Player pick menu made for changing your roles with a custom choice list
                PlayerPickMenu.OpenPlayerPickMenu(playerDataList, (Action) (() =>
                {
                    if (!ChoiceIsCurrent(context) || !RoleManager.Instance) return;
                    RefreshPreviewSession();
                    var choice = PlayerPickMenu.targetPlayerData;
                    if (choice == null || !choice.Role ||
                        !SpecialRoleAllowed(_oldRole, choice.Role.Role, Utils.isFreePlay, Utils.isHost)) return;

                    if (PlayerControl.LocalPlayer.Data.IsDead) // Prevent accidential revives
                    {
                        if (PlayerPickMenu.targetPlayerData.Role.TeamType == RoleTeamTypes.Impostor)
                        {
                            RoleManager.Instance.SetRole(PlayerControl.LocalPlayer, RoleTypes.ImpostorGhost);
                        }
                        else
                        {
                            RoleManager.Instance.SetRole(PlayerControl.LocalPlayer, RoleTypes.CrewmateGhost);
                        }
                    }
                    else
                    {
                        /* if (PlayerPickMenu.targetPlayerData.Role.Role == RoleTypes.Shapeshifter && oldRole != RoleTypes.Shapeshifter){

                            Utils.showPopup("\n<size=125%>Changing into the Shapeshifter role is not recommended\nsince shapeshifting will get you kicked by the anticheat");

                        } else if (PlayerPickMenu.targetPlayerData.Role.Role == RoleTypes.Noisemaker && oldRole != RoleTypes.Noisemaker){

                            Utils.showPopup("\n<size=125%>Changing into the Noisemaker role is not recommended\nsince dying won't trigger the alert for other players");

                        } else if (oldRole == RoleTypes.Noisemaker){

                            Utils.showPopup("\n<size=125%>Your \"real\" role is still Noisemaker\nso other players will still see the alert when you die");

                        } */

                        RoleManager.Instance.SetRole(PlayerControl.LocalPlayer, PlayerPickMenu.targetPlayerData.Role.Role);
                    }
                    _previewRolePointer = PlayerControl.LocalPlayer.Data.Role ? PlayerControl.LocalPlayer.Data.Role.Pointer : IntPtr.Zero;
                }));

                _setFakeRoleActive = true;
            }

            // Deactivate cheat if menu is closed
            if (PlayerPickMenu.playerpickMenu == null)
            {
                CheatToggles.setFakeRole = false;
            }

        }
        else
        {
            if (_setFakeRoleActive)
            {
                _setFakeRoleActive = false;
            }
        }
    }

    public static void SetFakeAlivePPM()
    {
        if (!(Utils.isFreePlay && OwnedActionContext.CanPickPlayer)) { CheatToggles.setFakeAlive = false; _setFakeAliveActive = false; return; }
        if (CheatToggles.setFakeAlive)
        {

            if (!_setFakeAliveActive)
            {

                // Close any player pick menus already open & their cheats
                if (PlayerPickMenu.playerpickMenu != null)
                {
                    PlayerPickMenu.playerpickMenu.Close();
                    CheatToggles.DisablePPMCheats("setFakeAlive");
                }

                var context = new OwnedActionContext.Snapshot(PlayerControl.LocalPlayer);
                List<NetworkedPlayerInfo> playerDataList = new List<NetworkedPlayerInfo>();

                playerDataList.Add(PlayerPickMenu.CustomPPMChoice("Alive", OutfitPreset.Crewmate, Utils.GetBehaviourByRoleType(RoleTypes.Crewmate)));
                playerDataList.Add(PlayerPickMenu.CustomPPMChoice("Dead", OutfitPreset.Dead, Utils.GetBehaviourByRoleType(RoleTypes.CrewmateGhost)));

                // Player pick menu made for changing your alive state with a custom choice list
                PlayerPickMenu.OpenPlayerPickMenu(playerDataList, (Action) (() =>
                {
                    if (!ChoiceIsCurrent(context) || !Utils.isFreePlay || PlayerPickMenu.targetPlayerData?.Role == null) return;
                    if (PlayerPickMenu.targetPlayerData.Role.IsDead)
                    {
                        PlayerControl.LocalPlayer.Die(DeathReason.Exile, true);
                    }
                    else
                    {
                        PlayerControl.LocalPlayer.Revive();
                    }
                }));

                _setFakeAliveActive = true;
            }

            // Deactivate cheat if menu is closed
            if (PlayerPickMenu.playerpickMenu == null)
            {
                CheatToggles.setFakeAlive = false;
            }

        }
        else
        {
            if (_setFakeAliveActive)
            {
                _setFakeAliveActive = false;
            }
        }
    }

    public static void ForceRolePPM()
    {
        if (!(OwnedActionContext.CanPickPlayer && (Utils.isHost || Utils.isFreePlay))) { CheatToggles.forceRole = false; _forceRoleActive = false; return; }
        if (CheatToggles.forceRole)
        {
            if (!_forceRoleActive)
            {
                if (PlayerPickMenu.playerpickMenu != null)
                {
                    PlayerPickMenu.playerpickMenu.Close();
                    CheatToggles.DisablePPMCheats("forceRole");
                }

                var context = new OwnedActionContext.Snapshot(PlayerControl.LocalPlayer);
                List<NetworkedPlayerInfo> playerDataList = new List<NetworkedPlayerInfo>();

                playerDataList.Add(PlayerPickMenu.CustomPPMChoice("Shapeshifter", OutfitPreset.Shapeshifter, Utils.GetBehaviourByRoleType(RoleTypes.Shapeshifter)));
                playerDataList.Add(PlayerPickMenu.CustomPPMChoice("Phantom", OutfitPreset.Phantom, Utils.GetBehaviourByRoleType(RoleTypes.Phantom)));
                playerDataList.Add(PlayerPickMenu.CustomPPMChoice("Viper", OutfitPreset.Viper, Utils.GetBehaviourByRoleType(RoleTypes.Viper)));
                playerDataList.Add(PlayerPickMenu.CustomPPMChoice("Impostor", OutfitPreset.Impostor, Utils.GetBehaviourByRoleType(RoleTypes.Impostor)));
                playerDataList.Add(PlayerPickMenu.CustomPPMChoice("Tracker", OutfitPreset.Tracker, Utils.GetBehaviourByRoleType(RoleTypes.Tracker)));
                playerDataList.Add(PlayerPickMenu.CustomPPMChoice("Noisemaker", OutfitPreset.Noisemaker, Utils.GetBehaviourByRoleType(RoleTypes.Noisemaker)));
                playerDataList.Add(PlayerPickMenu.CustomPPMChoice("Engineer", OutfitPreset.Engineer, Utils.GetBehaviourByRoleType(RoleTypes.Engineer)));
                playerDataList.Add(PlayerPickMenu.CustomPPMChoice("Scientist", OutfitPreset.Scientist, Utils.GetBehaviourByRoleType(RoleTypes.Scientist)));
                playerDataList.Add(PlayerPickMenu.CustomPPMChoice("Detective", OutfitPreset.Detective, Utils.GetBehaviourByRoleType(RoleTypes.Detective)));
                playerDataList.Add(PlayerPickMenu.CustomPPMChoice("Crewmate", OutfitPreset.Crewmate, Utils.GetBehaviourByRoleType(RoleTypes.Crewmate)));

                // Player pick menu made for forcing a role onto another player
                PlayerPickMenu.OpenPlayerPickMenu(playerDataList, (Action)(() =>
                {
                    if (!ChoiceIsCurrent(context) || (!Utils.isHost && !Utils.isFreePlay) || PlayerPickMenu.targetPlayerData?.Role == null) return;
                    CheatToggles.forcedRole = PlayerPickMenu.targetPlayerData.Role.Role;
                }));

                _forceRoleActive = true;
            }

            // Deactivate cheat if menu is closed
            if (PlayerPickMenu.playerpickMenu == null)
            {
                CheatToggles.forceRole = false;
            }

        }
        else
        {
            if (_forceRoleActive)
            {
                _forceRoleActive = false;
            }
        }
    }

    public static void SpectatePPM()
    {
        if (!CheatToggles.spectate || !OwnedActionContext.CanPickPlayer)
        {
            CameraToolHandler.StopSpectate(); _spectateActive = false; return;
        }
        if (!_spectateActive)
        {
            if (PlayerPickMenu.playerpickMenu != null)
            {
                PlayerPickMenu.playerpickMenu.Close();
                CheatToggles.DisablePPMCheats("spectate");
            }
            if (!CameraToolHandler.BeginSpectateSelection())
            { CheatToggles.spectate = false; return; }
            var generation = CameraToolHandler.Generation;
            List<NetworkedPlayerInfo> choices = new();
            foreach (var player in PlayerControl.AllPlayerControls)
                if (OwnedActionContext.ValidTarget(player, false) && player.Pointer != PlayerControl.LocalPlayer.Pointer)
                    choices.Add(player.Data);
            if (choices.Count == 0) { CameraToolHandler.StopSpectate(); return; }
            PlayerPickMenu.OpenPlayerPickMenu(choices, (Action)(() =>
                CameraToolHandler.SelectSpectateTarget(PlayerPickMenu.targetPlayerData?.Object, generation)));
            _spectateActive = true;
        }
        CameraToolHandler.TickSpectate();
        if (!CheatToggles.spectate || (PlayerPickMenu.playerpickMenu == null && !CameraToolHandler.HasSpectateTarget))
        { CameraToolHandler.StopSpectate(); _spectateActive = false; }
    }

    private static System.Collections.IEnumerator ReturnAfterKill(OwnedActionContext.Snapshot context, Vector2 position)
    {
        yield return new WaitForSeconds(0.25f);
        if (ChoiceIsCurrent(context, hostRound: true) && PlayerControl.LocalPlayer.NetTransform)
            PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(position);
    }
}
