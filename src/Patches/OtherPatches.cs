using HarmonyLib;
using AmongUs.Data;
using AmongUs.Data.Player;
using UnityEngine;
using System;
using System.Security.Cryptography;
using InnerNet;
using System.Collections.Generic;

namespace HAU;

[HarmonyPatch(typeof(Constants), nameof(Constants.GetPlatformData))]
public static class Constants_GetPlatformData
{

    public static void Postfix(ref PlatformSpecificData __result)
    {
        if (Utils.StringToPlatformType(HAUPlugin.spoofPlatform.Value, out Platforms? platformType))
        {
            __result = new PlatformSpecificData
            {
                Platform = (Platforms)platformType,
                PlatformName = Constants.GetPlatformName()
            };
        }
    }
}

[HarmonyPatch(typeof(GameData), nameof(GameData.HandleDisconnect), new[] { typeof(PlayerControl), typeof(DisconnectReasons) })]
public static class GameData_HandleDisconnect
{
    public static HashSet<int> disconnectQueue = new();


    public static void Prefix(PlayerControl player)
    {
        if (!CheatToggles.runOverload) return;

        NetworkedPlayerInfo playerData = player?.Data;
        if (playerData == null) return;

        bool isTarget = OverloadUI.currentTargets.Contains(playerData);

        if (isTarget) disconnectQueue.Add(playerData.ClientId);
    }



    public static void Postfix(PlayerControl player)
    {
        if (!CheatToggles.runOverload) return;

        NetworkedPlayerInfo playerData = player?.Data;
        if (playerData == null) return;

        int clientId = player.Data.ClientId;

        if (disconnectQueue.Contains(clientId))
        {
            OverloadUI.numSuccesses++;

            if (CheatToggles.olLogDisconnect)
            {
                int total = OverloadUI.currentTargets.Count
                            + OverloadUI.numSuccesses
                            - disconnectQueue.Count;

                string colorStr = ColorUtility.ToHtmlStringRGB(Color.green);

                OverloadUI.LogConsole($"> <b><color=#{colorStr}>!! {playerData.DefaultOutfit.PlayerName} (ID : {playerData.ClientId}) Disconnected !! - [{OverloadUI.numSuccesses}/{total}]</color></b>");
            }

            disconnectQueue.Remove(clientId);
        }
    }
}

[HarmonyPatch(typeof(FreeChatInputField), nameof(FreeChatInputField.UpdateCharCount))]
public static class FreeChatInputField_UpdateCharCount
{

    public static void Postfix(FreeChatInputField __instance)
    {

        if (!CheatToggles.longerMessages) return;


        int length = __instance.textArea.text.Length;
        __instance.charCountText.SetText($"{length}/{__instance.textArea.characterLimit}");

        if (length < 90)
        {
            __instance.charCountText.color = Color.black;
        }
        else if (length < 120)
        {
            __instance.charCountText.color = new Color(1f, 1f, 0f, 1f);
        }
        else
        {
            __instance.charCountText.color = Color.red;
        }
    }
}

[HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetName))]
public static class ChatBubble_SetName
{
    public static void Postfix(ChatBubble __instance)
	{
        InformationTools.ChatNametags(__instance);
    }
}

[HarmonyPatch(typeof(SystemInfo), nameof(SystemInfo.deviceUniqueIdentifier), MethodType.Getter)]
public static class SystemInfo_deviceUniqueIdentifier_Getter
{


    public static void Postfix(ref string __result)
    {
        if (!HAUPlugin.spoofDeviceId.Value) return;

        var bytes = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }

        __result = BitConverter.ToString(bytes).Replace("-", "").ToLower();
    }
}

[HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
public static class VersionShower_Start
{

    public static void Postfix(VersionShower __instance)
    {
        if (HAUPlugin.inStealthMode || HAUPlugin.isPanicked) return;

        if (HAUPlugin.supportedAU.Contains(Application.version))
        {
            __instance.text.text =  $"{ModBranding.Name} v{HAUPlugin.displayVersion} (v{Application.version})";
        }
        else
        {
            __instance.text.text =  $"{ModBranding.Name} v{HAUPlugin.displayVersion} (<color=red>v{Application.version}</color>)";
        }
    }
}

[HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
public static class PingTracker_Update
{

    public static void Postfix(PingTracker __instance)
    {
        if (HAUPlugin.inStealthMode)
        {
            __instance.text.alignment = TMPro.TextAlignmentOptions.TopLeft;

            return;
        }

        __instance.text.alignment = TMPro.TextAlignmentOptions.Center;

        int ping = Utils.GetPing();
        string pingText = Utils.GetColoredPingText($"PING: {ping} ms", ping);
        var sprint = SprintHandler.Indicator;
        var sprintLine = string.IsNullOrEmpty(sprint) ? "" : "\n" + sprint;
        if (MovementAutomation.Active) sprintLine += "\n" + MovementAutomation.StatusText;

        if (AmongUsClient.Instance.IsGameStarted)
        {
            __instance.aspectPosition.DistanceFromEdge = new Vector3(-0.21f, 0.50f, 0f);

            __instance.text.text = $"{ModBranding.Name} by {ModBranding.Creator} ~ {pingText}" + sprintLine;

            return;
        }

        __instance.text.text = $"{ModBranding.Name} by {ModBranding.Creator}\n{pingText}" + sprintLine;

    }
}

[HarmonyPatch(typeof(DisconnectPopup), nameof(DisconnectPopup.DoShow))]
public static class DisconnectPopup_DoShow
{

    public static void Postfix(DisconnectPopup __instance)
    {
        if (!CheatToggles.copyLobbyCodeOnDisconnect) return;

        GUIUtility.systemCopyBuffer = AmongUsClient_OnGameJoined.lastGameIdString;

        __instance.SetText(__instance._textArea.text + "\n\n<size=60%>Lobby code has been copied to the clipboard</size>");
    }
}

[HarmonyPatch(typeof(PlayerBanData), nameof(PlayerBanData.BanMinutesLeft), MethodType.Getter)]
public static class PlayerBanData_BanMinutesLeft_Getter
{

    public static void Postfix(PlayerBanData __instance, ref int __result)
    {
        if (!CheatToggles.avoidPenalties) return;

        __instance.BanPoints = 0f;
        __result = 0;
    }
}

[HarmonyPatch(typeof(FullAccount), nameof(FullAccount.CanSetCustomName))]
public static class FullAccount_CanSetCustomName
{

    public static void Prefix(ref bool canSetName)
    {
        if (CheatToggles.unlockFeatures)
        {
            canSetName = true;
        }
    }
}

[HarmonyPatch(typeof(AccountManager), nameof(AccountManager.CanPlayOnline))]
public static class AccountManager_CanPlayOnline
{

    public static void Postfix(ref bool __result)
    {
        if (CheatToggles.unlockFeatures)
        {
            __result = true;
        }
    }
}

[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.JoinGame))]
public static class InnerNetClient_JoinGame
{

    public static void Prefix()
    {
        if (CheatToggles.unlockFeatures)
        {
            DataManager.Player.Account.LoginStatus = EOSManager.AccountLoginStatus.LoggedIn;
        }
    }
}

[HarmonyPatch(typeof(GameManager), nameof(GameManager.CheckTaskCompletion))]
public static class GameManager_CheckTaskCompletion
{

    public static bool Prefix(ref bool __result)
    {
        if (!CheatToggles.noGameEnd || (!Utils.isHost && !Utils.isFreePlay)) return true;

        __result = false;

        return false;
    }
}

[HarmonyPatch(typeof(Mushroom), nameof(Mushroom.FixedUpdate))]
public static class Mushroom_FixedUpdate
{
    public static void Postfix(Mushroom __instance)
    {
        InformationTools.SporeCloudVision(__instance);
    }
}


[HarmonyPatch(typeof(DoorBreakerGame), nameof(DoorBreakerGame.Start))]
public static class DoorBreakerGame_Start
{

    public static bool Prefix(DoorBreakerGame __instance)
    {
        if (!CheatToggles.autoOpenDoorsOnUse) return true;

        DoorsHandler.OpenDoor(__instance.MyDoor);
        __instance.MyDoor.SetDoorway(true);
        __instance.Close();

        return false;
    }
}


[HarmonyPatch(typeof(DoorCardSwipeGame), nameof(DoorCardSwipeGame.Begin))]
public static class DoorCardSwipeGame_Begin
{

    public static bool Prefix(DoorCardSwipeGame __instance)
    {
        if (!CheatToggles.autoOpenDoorsOnUse) return true;

        DoorsHandler.OpenDoor(__instance.MyDoor);
        __instance.MyDoor.SetDoorway(true);
        __instance.Close();

        return false;
    }
}


[HarmonyPatch(typeof(MushroomDoorSabotageMinigame), nameof(MushroomDoorSabotageMinigame.Begin))]
public static class MushroomDoorSabotageMinigame_Begin
{

    public static bool Prefix(MushroomDoorSabotageMinigame __instance)
    {
        if (!CheatToggles.autoOpenDoorsOnUse) return true;

        __instance.FixDoorAndCloseMinigame();

        return false;
    }
}














[HarmonyPatch(typeof(IntroCutscene), "CoBegin")]
public static class IntroCutscene_CoBegin
{

    public static void Prefix()
    {


        InfiniteJudgeHandler.OnRoundIntro();
        LocalImpostorHandler.OnRoundIntro();
#if GUEST_KILL_EXPERIMENT
        GuestRoundTest.Reset();
#endif
        if (CheatToggles.alwaysImpostor)
        {
            AlwaysImpostorHandler.AssignHostedRole();
            LocalImpostorHandler.Tick();
            return;
        }

        if (!Utils.isHost || !CheatToggles.forcedRole.HasValue) return;

        var forcedRole = CheatToggles.forcedRole.Value;


        if (PlayerControl.LocalPlayer.Data.RoleType == forcedRole)
        {
            return;
        }


        PlayerControl roleSwapTarget = null;
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player.Data.RoleType != forcedRole) continue;
            roleSwapTarget = player;
            break;
        }

        DestroyableSingleton<RoleManager>.Instance.SetRole(PlayerControl.LocalPlayer, forcedRole);

        if (roleSwapTarget != null)
        {
            DestroyableSingleton<RoleManager>.Instance.SetRole(roleSwapTarget, PlayerControl.LocalPlayer.Data.RoleType);
        }
    }
}


[HarmonyPatch(typeof(GameContainer), nameof(GameContainer.SetupGameInfo))]
public static class GameContainer_SetupGameInfo
{


    public static void Postfix(GameContainer __instance)
    {
        if (!CheatToggles.seeLobbyInfo) return;


        const string separator = "<#0000>000000000000000</color>";

        var trueHostName = __instance.gameListing.TrueHostName;

        var age = __instance.gameListing.Age;
        var lobbyTime = $"Age: {age / 60}:{(age % 60 < 10 ? "0" : "")}{age % 60}";

        var platform = Utils.PlatformTypeToString(__instance.gameListing.Platform);


        __instance.capacity.text = $"<size=40%>{separator}\n{trueHostName}\n{__instance.capacity.text}\n" +
                                   $"<#fb0>{GameCode.IntToGameName(__instance.gameListing.GameId)}</color>\n" +
                                   $"<#b0f>{platform}</color>\n{lobbyTime}\n{separator}</size>";
    }
}

[HarmonyPatch(typeof(BanMenu), nameof(BanMenu.SetVisible))]
public static class BanMenu_SetVisible
{

    public static bool Prefix(BanMenu __instance, bool show)
    {
        if (!Utils.isHost) return true;

        show &= PlayerControl.LocalPlayer && PlayerControl.LocalPlayer.Data != null;

        __instance.BanButton.gameObject.SetActive(true);
        __instance.KickButton.gameObject.SetActive(true);
        __instance.MenuButton.gameObject.SetActive(show);

        return false;
    }
}

[HarmonyPatch(typeof(IGameOptionsExtensions), nameof(IGameOptionsExtensions.GetAdjustedNumImpostors))]
public static class IGameOptionsExtensions_GetAdjustedNumImpostors
{

    public static bool Prefix(ref int __result)
    {
        if (!CheatToggles.noOptionsLimits) return true;

        __result = GameOptionsManager.Instance.CurrentGameOptions.NumImpostors;

        return false;
    }
}

[HarmonyPatch(typeof(MatchInfoHudButton), nameof(MatchInfoHudButton.Update))]
public static class MatchInfoHudButton_Update
{

    public static bool Prefix(MatchInfoHudButton __instance)
    {
        if (CheatToggles.enableChat)
        {
            __instance.aspectPosition.DistanceFromEdge = MatchInfoHudButton.adjustedDistanceFromEdge;

            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(PlayerPurchasesData), nameof(PlayerPurchasesData.GetPurchase))]
public static class PlayerPurchasesData_GetPurchase
{

    public static void Postfix(ref bool __result)
    {
        if (!CheatToggles.freeCosmetics) return;

        __result = true;
    }
}
