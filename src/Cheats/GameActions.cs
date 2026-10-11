using System.Runtime.CompilerServices;
using AmongUs.GameOptions;
using AmongUs.InnerNet.GameDataMessages;
using UnityEngine;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace HAU;
public static class GameActions
{
    private static bool _isScanAnimActive;
    private static bool _isCamsAnimActive;
    private static System.IntPtr _effectPlayer, _effectShip;
    private static int _lastEffectFrame = -1;
    private static double _nextHeldEffectAt, _lastEffectClock;
    private const double HeldEffectInterval = 0.2;

    public static void CloseMeetingCheat()
    {
        if (!CheatToggles.closeMeeting) return;

        if (Utils.isMeeting)
        {


            MeetingHud.Instance.DespawnOnDestroy = false;
            Object.Destroy(MeetingHud.Instance.gameObject);


            DestroyableSingleton<HudManager>.Instance.StartCoroutine(DestroyableSingleton<HudManager>.Instance.CoFadeFullScreen(Color.black, Color.clear, 0.2f, false));
            PlayerControl.LocalPlayer.SetKillTimer(GameManager.Instance.LogicOptions.GetKillCooldown());
            ShipStatus.Instance.EmergencyCooldown = GameManager.Instance.LogicOptions.GetEmergencyCooldown();
            Camera.main.GetComponent<FollowerCamera>().Locked = false;
            DestroyableSingleton<HudManager>.Instance.SetMapAndInfoButtonsEnabled(true);
            DestroyableSingleton<HudManager>.Instance.SetHudActive(true);
            ControllerManager.Instance.CloseAndResetAll();

        }
        else if (ExileController.Instance)
        {
            ExileController.Instance.ReEnableGameplay();
            ExileController.Instance.WrapUp();
        }

        CheatToggles.closeMeeting = false;
    }

    public static void SkipMeetingCheat()
    {
        if (!CheatToggles.skipMeeting) return;
        CheatToggles.skipMeeting = false;
        if (!OwnedActionContext.HostMeetingAction) return;

        if (Utils.isMeeting)
        {
            MeetingHud.Instance.RpcVotingComplete(new Il2CppStructArray<MeetingHud.VoterState>(0L), null, true, false, ushort.MinValue);
        }

        CheatToggles.skipMeeting = false;
    }

    public static void CallMeetingCheat()
    {
        if (!CheatToggles.callMeeting) return;
        CheatToggles.callMeeting = false;
        if (!OwnedActionContext.StartMeeting) return;

        if (Utils.isHost)
        {

            MeetingRoomManager.Instance.AssignSelf(PlayerControl.LocalPlayer, null);
            DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(PlayerControl.LocalPlayer);
            PlayerControl.LocalPlayer.RpcStartMeeting(null);
        }
        else
        {
            PlayerControl.LocalPlayer.CmdReportDeadBody(null);
        }

        CheatToggles.callMeeting = false;
    }

    public static void ForceStartGameCheat()
    {
        if (!CheatToggles.forceStartGame) return;
        CheatToggles.forceStartGame = false;
        if (!OwnedActionContext.TryPlayer(out _)) return;

        if (Utils.isHost && Utils.isLobby)
        {
            AmongUsClient.Instance.SendStartGame();
        }

        CheatToggles.forceStartGame = false;
    }

    public static void CompleteMyTasksCheat()
    {
        if (CheatToggles.completeMyTasks)
        {
            foreach (var task in PlayerControl.LocalPlayer.myTasks)
            {
                Utils.CompleteTask(task);
            }

            CheatToggles.completeMyTasks = false;
        }
    }

    public static void OpenSabotageMapCheat()
    {
        if (!CheatToggles.sabotageMap) return;

        CheatToggles.sabotageMap = false;
        if (!OwnedActionContext.GameplayReady || Utils.isMeeting || !Utils.isNormalGame) return;


        MenuUI.CloseMenu();

        DestroyableSingleton<HudManager>.Instance.ToggleMapVisible(new MapOptions
        {
            Mode = MapOptions.Modes.Sabotage
        });
    }

    public static void HandleEngineerCheats(EngineerRole engineerRole)
    {
        if (CheatToggles.endlessVentTime)
        {

            engineerRole.inVentTimeRemaining = float.MaxValue;
        }
        else if (engineerRole.inVentTimeRemaining > engineerRole.GetCooldown())
        {

            engineerRole.inVentTimeRemaining = engineerRole.GetCooldown();
        }

        if (CheatToggles.noVentCooldown)
        {
            if (engineerRole.cooldownSecondsRemaining > 0f)
            {
                engineerRole.cooldownSecondsRemaining = 0f;

                DestroyableSingleton<HudManager>.Instance.AbilityButton.ResetCoolDown();
                DestroyableSingleton<HudManager>.Instance.AbilityButton.SetCooldownFill(0f);
            }
        }
    }

    public static void HandleShapeshifterCheats(ShapeshifterRole shapeshifterRole)
    {
        if (CheatToggles.endlessSsDuration)
        {

            shapeshifterRole.durationSecondsRemaining = float.MaxValue;
        }
        else if (shapeshifterRole.durationSecondsRemaining > GameManager.Instance.LogicOptions.GetRoleFloat(FloatOptionNames.ShapeshifterDuration))
        {

            shapeshifterRole.durationSecondsRemaining = GameManager.Instance.LogicOptions.GetRoleFloat(FloatOptionNames.ShapeshifterDuration);

        }
    }

    public static void HandleScientistCheats(ScientistRole scientistRole)
    {
        if (CheatToggles.noVitalsCooldown)
        {
            scientistRole.currentCooldown = 0f;
        }

        if (CheatToggles.endlessBattery)
        {

            scientistRole.currentCharge = float.MaxValue;
        }
        else if (scientistRole.currentCharge > scientistRole.RoleCooldownValue)
        {

            scientistRole.currentCharge = scientistRole.RoleCooldownValue;
        }
    }

    public static void HandleTrackerCheats(TrackerRole trackerRole)
    {
        if (CheatToggles.noTrackingCooldown)
        {
            trackerRole.cooldownSecondsRemaining = 0f;
            trackerRole.delaySecondsRemaining = 0f;

            DestroyableSingleton<HudManager>.Instance.AbilityButton.ResetCoolDown();
            DestroyableSingleton<HudManager>.Instance.AbilityButton.SetCooldownFill(0f);
        }

        if (CheatToggles.noTrackingDelay && MapBehaviour.Instance != null)
        {
            MapBehaviour.Instance.trackedPointDelayTime = GameManager.Instance.LogicOptions.GetRoleFloat(FloatOptionNames.TrackerDelay);
        }

        if (CheatToggles.endlessTracking)
        {

            trackerRole.durationSecondsRemaining = float.MaxValue;
        }
        else if (trackerRole.durationSecondsRemaining > GameManager.Instance.LogicOptions.GetRoleFloat(FloatOptionNames.TrackerDuration))
        {

            trackerRole.durationSecondsRemaining = GameManager.Instance.LogicOptions.GetRoleFloat(FloatOptionNames.TrackerDuration);
        }
    }

    public static void UseVentCheat(HudManager hudManager)
    {

        try
        {




			if (!PlayerControl.LocalPlayer.Data.Role.CanVent && !PlayerControl.LocalPlayer.Data.IsDead)
            {
				hudManager.ImpostorVentButton.gameObject.SetActive(CheatToggles.unlockVents || MultiRoleHandler.VentAccess);
			}

        } catch { }
    }

    public static void WalkInVentCheat()
    {
        try
        {
            if (!CheatToggles.walkInVents) return;

            PlayerControl.LocalPlayer.inVent = false;
            PlayerControl.LocalPlayer.moveable = true;

        } catch { }
    }

    public static void KickVentsCheat()
    {
        if (!CheatToggles.kickVents) return;
        CheatToggles.kickVents = false;
        if (!OwnedActionContext.HostRoundAction) return;

        foreach(var vent in ShipStatus.Instance.AllVents)
        {
            VentilationSystem.Update(VentilationSystem.Operation.BootImpostors, vent.Id);
        }

        CheatToggles.kickVents = false;
    }

    public static void KillAllCheat()
    {
        if (!CheatToggles.killAll) return;
        CheatToggles.killAll = false;
        if (!OwnedActionContext.HostRoundAction) return;

        if (Utils.isLobby)
        {
            HudManager.Instance.Notifier.AddDisconnectMessage("Killing in lobby disabled for being too buggy");
        }
        else
        {

            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (!OwnedActionContext.HostRoundAction || !OwnedActionContext.ValidTarget(player)) continue;
                Utils.MurderPlayer(player, MurderResultFlags.Succeeded);
            }
        }

        CheatToggles.killAll = false;
    }

    public static void KillAllCrewCheat()
    {
        if (!CheatToggles.killAllCrew) return;
        CheatToggles.killAllCrew = false;
        if (!OwnedActionContext.HostRoundAction) return;

        if (Utils.isLobby)
        {
            HudManager.Instance.Notifier.AddDisconnectMessage("Killing in lobby disabled for being too buggy");
        }
        else
        {

            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (OwnedActionContext.HostRoundAction && OwnedActionContext.ValidTarget(player) &&
                    player.Data.Role && player.Data.Role.TeamType == RoleTeamTypes.Crewmate)
                {
                    Utils.MurderPlayer(player, MurderResultFlags.Succeeded);
                }
            }
        }

        CheatToggles.killAllCrew = false;
    }

    public static void KillAllImpsCheat()
    {
        if (!CheatToggles.killAllImps) return;
        CheatToggles.killAllImps = false;
        if (!OwnedActionContext.HostRoundAction) return;

        if (Utils.isLobby)
        {
            HudManager.Instance.Notifier.AddDisconnectMessage("Killing in lobby disabled for being too buggy");
        }
        else
        {

            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (OwnedActionContext.HostRoundAction && OwnedActionContext.ValidTarget(player) &&
                    player.Data.Role && player.Data.Role.TeamType == RoleTeamTypes.Impostor)
                {
                    Utils.MurderPlayer(player, MurderResultFlags.Succeeded);
                }
            }
        }

        CheatToggles.killAllImps = false;
    }

    public static void ProtectCheat()
    {
        if (!OwnedActionContext.HostRoundAction) return;

        for (var i = ProtectUI.playersToProtect.Count - 1; i >= 0; i--)
        {
            var player = ProtectUI.playersToProtect[i];
            if (!OwnedActionContext.ValidTarget(player) || !player.cosmetics)
            { ProtectUI.playersToProtect.RemoveAt(i); continue; }
            if (player.protectedByGuardianId == -1)
            {

                PlayerControl.LocalPlayer.RpcProtectPlayer(player, PlayerControl.LocalPlayer.cosmetics.ColorId);
            }
        }
    }

    public static void TeleportCursorCheat()
    {
        if (!OwnedActionContext.MovementReady) return;
        if (PlayerControl.LocalPlayer?.NetTransform == null || Camera.main == null) return;
        if (!CheatToggles.teleportCursor) return;


        if (Input.GetMouseButtonDown(1))
        {
            PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(Camera.main.ScreenToWorldPoint(Input.mousePosition));
        }
    }

    public static void NoClipCheat()
    {
        try
        {

            PlayerControl.LocalPlayer.Collider.enabled =
                !PlayerControl.LocalPlayer.onLadder &&
                (!CheatToggles.noClip || MovementAutomation.NeedsGroundCollision);

        } catch { }
    }

    public static void PlayScannerCheat()
    {
        if (CheatToggles.animMedScan && !_isScanAnimActive)
        {
            Utils.ForceSetScanner(PlayerControl.LocalPlayer, true);
            _isScanAnimActive = true;
        }
        else if (!CheatToggles.animMedScan && _isScanAnimActive)
        {
            Utils.ForceSetScanner(PlayerControl.LocalPlayer, false);
            _isScanAnimActive = false;
        }
    }

    public static void PlayAnimationCheat(PlayerPhysics physics)
    {
        var local = PlayerControl.LocalPlayer;
        if (!local || !local.MyPhysics || !physics || !physics.AmOwner || !physics.myPlayer ||
            physics.myPlayer.Pointer != local.Pointer || physics.Pointer != local.MyPhysics.Pointer) return;
        if (!OwnedActionContext.GameplayReady || Utils.isMeeting) return;
        var shipPointer = ShipStatus.Instance.Pointer;
        if (_effectPlayer != local.Pointer || _effectShip != shipPointer)
        {
            _effectPlayer = local.Pointer; _effectShip = shipPointer;
            _lastEffectFrame = -1; _nextHeldEffectAt = _lastEffectClock = 0;
        }
        if (_lastEffectFrame == Time.frameCount) return;
        _lastEffectFrame = Time.frameCount;
        var now = Time.realtimeSinceStartupAsDouble;
        bool validClock = !double.IsNaN(now) && !double.IsInfinity(now);
        if (validClock && now < _lastEffectClock) _nextHeldEffectAt = now + HeldEffectInterval;
        if (validClock) _lastEffectClock = now;
        bool heldReady = validClock && now >= _nextHeldEffectAt;
        bool sentHeld = false;
        if (heldReady && CheatToggles.animPet && local.cosmetics != null && local.cosmetics.CurrentPet != null)
        {

            RpcPetMessage rpcMessage = new(PlayerControl.LocalPlayer.MyPhysics.NetId,
                PlayerControl.LocalPlayer.cosmetics.CurrentPet.PettingPlayerPosition,
                PlayerControl.LocalPlayer.cosmetics.CurrentPet.transform.position);
            AmongUsClient.Instance.LateBroadcastReliableMessage(Unsafe.As<IGameDataMessage>(rpcMessage));
            sentHeld = true;
            _nextHeldEffectAt = now + HeldEffectInterval;
        }

        byte mapId = Utils.GetCurrentMapID();

        if (mapId == byte.MaxValue) return;

        var map = (MapNames)mapId;

        if (CheatToggles.animShields)
        {
            if (map is MapNames.Skeld or MapNames.Dleks)
            {
                Utils.ForcePlayAnimation((byte)TaskTypes.PrimeShields);
            }
            CheatToggles.animShields = false;
        }

        if (CheatToggles.animAsteroids)
        {
            if (map is MapNames.Skeld or MapNames.Dleks or MapNames.Polus)
            {
                if (heldReady)
                {
                    Utils.ForcePlayAnimation((byte)TaskTypes.ClearAsteroids);
                    sentHeld = true;
                }
            }
            else
            {
                CheatToggles.animAsteroids = false;
            }
        }

        if (CheatToggles.animEmptyGarbage)
        {
            if (map is MapNames.Skeld or MapNames.Dleks)
            {
                Utils.ForcePlayAnimation((byte)TaskTypes.EmptyGarbage);
            }

            CheatToggles.animEmptyGarbage = false;
        }

        if (map is not (MapNames.MiraHQ or MapNames.Fungle))
        {
            if (CheatToggles.animCamsInUse && !_isCamsAnimActive)
            {

                ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Security, 1);
                _isCamsAnimActive = true;
            }
            else if (!CheatToggles.animCamsInUse && _isCamsAnimActive)
            {

                ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Security, 0);
                _isCamsAnimActive = false;
            }
        }
        else
        {
            CheatToggles.animCamsInUse = false;
        }
        if (sentHeld) _nextHeldEffectAt = now + HeldEffectInterval;
    }

    public static void StopShipAnimCheats()
    {
        CheatToggles.animShields = false;
        CheatToggles.animAsteroids = false;
        CheatToggles.animEmptyGarbage = false;
        CheatToggles.animMedScan = false;
        CheatToggles.animCamsInUse = false;




        _isCamsAnimActive = false;
        _isScanAnimActive = false;
        _effectPlayer = _effectShip = System.IntPtr.Zero;
        _lastEffectFrame = -1;
        _nextHeldEffectAt = _lastEffectClock = 0;
    }
}
