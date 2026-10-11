using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HAU;

// Execution checks live beside the actions, independently of GUI/keybind gates.
internal static class OwnedActionContext
{
    internal readonly struct Snapshot
    {
        internal readonly IntPtr Player, Client, Ship;
        internal readonly int Scene;
        internal Snapshot(PlayerControl player)
        {
            Player = player.Pointer;
            Client = AmongUsClient.Instance.Pointer;
            Ship = ShipStatus.Instance ? ShipStatus.Instance.Pointer : IntPtr.Zero;
            Scene = SceneManager.GetActiveScene().handle;
        }
        internal bool IsCurrent => TryPlayer(out var local) && local.Pointer == Player &&
            AmongUsClient.Instance.Pointer == Client &&
            (ShipStatus.Instance ? ShipStatus.Instance.Pointer : IntPtr.Zero) == Ship &&
            SceneManager.GetActiveScene().handle == Scene;
    }

    internal static bool TryPlayer(out PlayerControl local)
    {
        local = PlayerControl.LocalPlayer;
        return !HAUPlugin.isPanicked && Utils.isClient && AmongUsClient.Instance &&
            (Utils.isLobby || Utils.isInGame || Utils.isFreePlay) && local && local.AmOwner &&
            local.Data && !local.Data.Disconnected;
    }

    internal static bool GameplayReady => TryPlayer(out _) && ShipStatus.Instance &&
        (Utils.isInGame || Utils.isFreePlay) && HudManager.InstanceExists &&
        !HudManager.Instance.IsIntroDisplayed && !Utils.isExiling;

    internal static bool HostRoundAction => GameplayReady && (Utils.isHost || Utils.isFreePlay) && !Utils.isMeeting;
    internal static bool HostMeetingAction => GameplayReady && (Utils.isHost || Utils.isFreePlay) &&
        MeetingHud.Instance && MeetingHud.Instance.state < MeetingHud.MeetingStates.Results;
    internal static bool StartMeeting => GameplayReady && !Utils.isMeeting && !PlayerControl.LocalPlayer.Data.IsDead;

    internal static bool CanPickPlayer => TryPlayer(out _) && HudManager.InstanceExists &&
        !HudManager.Instance.IsIntroDisplayed && !Utils.isMeeting && !Utils.isExiling;

    internal static bool InputPaused => !Application.isFocused || MenuUI.isGUIActive ||
        Utils.isMeeting || Utils.isExiling || !HudManager.InstanceExists ||
        HudManager.Instance.IsIntroDisplayed || Minigame.Instance ||
        (HudManager.Instance.Chat && HudManager.Instance.Chat.IsOpenOrOpening);

    internal static bool MovementReady => CanPickPlayer && !InputPaused &&
        !PlayerControl.LocalPlayer.inVent && !PlayerControl.LocalPlayer.onLadder &&
        !PlayerControl.LocalPlayer.inMovingPlat && PlayerControl.LocalPlayer.CanMove;

    internal static bool ValidTarget(PlayerControl target, bool living = true) => target &&
        target.Data && !target.Data.Disconnected && (!living || !target.Data.IsDead);
}
