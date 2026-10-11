using System;
using UnityEngine;

namespace HAU;



public static class CameraToolHandler
{
    private enum Mode { None, Freecam, Spectate }
    private static Mode _mode;
    private static PlayerControl _player, _target;
    private static MonoBehaviour _originalTarget;
    private static Camera _camera;
    private static FollowerCamera _follower;
    private static OwnedActionContext.Snapshot _context;
    private static bool _originalMoveable, _originalEnabled, _paused;
    private static MonoBehaviour _writtenTarget;
    private static bool _writtenEnabled;
    private static int _generation;
    public static bool HasSpectateTarget => _mode == Mode.Spectate && OwnedActionContext.ValidTarget(_target, false);
    public static int Generation => _generation;

    private static bool Capture(Mode mode)
    {
        Reset();
        if (!OwnedActionContext.CanPickPlayer || !Camera.main) return false;
        var follower = Camera.main.GetComponent<FollowerCamera>();
        if (!follower) return false;
        _player = PlayerControl.LocalPlayer;
        _camera = Camera.main;
        _follower = follower;
        _context = new OwnedActionContext.Snapshot(_player);
        _originalMoveable = _player.moveable;
        _originalEnabled = follower.enabled;
        _originalTarget = follower.Target;
        _writtenTarget = _originalTarget; _writtenEnabled = _originalEnabled;
        _mode = mode;
        _paused = false;
        _generation++;
        return true;
    }

    public static bool BeginSpectateSelection()
    {
        CheatToggles.freecam = false;
        if (!Capture(Mode.Spectate)) return false;
        _player.moveable = false;
        return true;
    }

    public static bool SelectSpectateTarget(PlayerControl target, int generation)
    {
        if (_mode != Mode.Spectate || generation != _generation || !IsCurrent() ||
            !OwnedActionContext.CanPickPlayer || !OwnedActionContext.ValidTarget(target, false) ||
            target.Pointer == _player.Pointer)
        {
            if (generation == _generation) StopSpectate();
            return false;
        }
        _target = target;
        return true;
    }

    public static void TickFreecam()
    {
        try
        {
            if (!CheatToggles.freecam || HAUPlugin.isPanicked)
            { if (_mode == Mode.Freecam) Reset(); return; }
            if (_mode != Mode.Freecam)
            {
                if (_mode == Mode.Spectate) { CheatToggles.spectate = false; Reset(); }
                if (!OwnedActionContext.MovementReady) return;
                CheatToggles.spectate = false;
                if (!Capture(Mode.Freecam)) { CheatToggles.freecam = false; return; }
            }
            if (!IsCurrent()) { CheatToggles.freecam = false; Reset(); return; }
            if (!_paused && !StillOwnsCameraState()) { CheatToggles.freecam = false; Reset(); return; }
            if (OwnedActionContext.InputPaused || _player.inVent || _player.onLadder || _player.inMovingPlat)
            { Pause(); return; }
            _paused = false;
            _follower.enabled = false;
            _follower.Target = null;
            _writtenEnabled = false; _writtenTarget = null;
            _player.moveable = false;
            var movement = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0f);
            _camera.transform.position += movement * 10f * Time.deltaTime;
        }
        catch (Exception) { CheatToggles.freecam = false; Reset(); }
    }

    public static void TickSpectate()
    {
        try
        {
            if (!CheatToggles.spectate || HAUPlugin.isPanicked)
            { StopSpectate(); return; }
            if (_mode != Mode.Spectate || !IsCurrent()) { StopSpectate(); return; }
            if (!_paused && !StillOwnsCameraState()) { StopSpectate(); return; }
            if (_target && !OwnedActionContext.ValidTarget(_target, false)) { StopSpectate(); return; }
            if (OwnedActionContext.InputPaused)
            { Pause(); return; }
            _paused = false;
            _player.moveable = false;
            if (_target)
            {
                _follower.enabled = true;
                if (!_follower.Target || _follower.Target.Pointer != _target.Pointer) _follower.SetTarget(_target);
                _writtenEnabled = true; _writtenTarget = _target;
            }
        }
        catch (Exception) { StopSpectate(); }
    }

    public static void StopSpectate()
    {
        CheatToggles.spectate = false;
        if (_mode == Mode.Spectate) Reset();
    }

    private static bool IsCurrent() => _player && _camera && _follower && _context.IsCurrent &&
        Camera.main && Camera.main.Pointer == _camera.Pointer &&
        Camera.main.GetComponent<FollowerCamera>() &&
        Camera.main.GetComponent<FollowerCamera>().Pointer == _follower.Pointer;

    private static bool StillOwnsCameraState() => _follower && _follower.enabled == _writtenEnabled &&
        ((!_follower.Target && !_writtenTarget) || (_follower.Target && _writtenTarget &&
            _follower.Target.Pointer == _writtenTarget.Pointer));

    private static void Pause()
    {
        if (_paused) return;
        RestoreCaptured();
        _paused = true;
    }

    private static void RestoreCaptured()
    {
        try
        {
            if (_player && _player.AmOwner && !_player.moveable &&
                _player.Data != null && !_player.Data.Disconnected &&
                !Utils.isMeeting && !Utils.isExiling && !Minigame.Instance &&
                !(HudManager.InstanceExists && (HudManager.Instance.IsIntroDisplayed ||
                    (HudManager.Instance.Chat && HudManager.Instance.Chat.IsOpenOrOpening))))
                _player.moveable = _originalMoveable;
            if (StillOwnsCameraState())
            {
                if (_originalTarget) _follower.SetTarget(_originalTarget);
                else _follower.Target = null;
                _follower.enabled = _originalEnabled;
                _writtenTarget = _originalTarget; _writtenEnabled = _originalEnabled;
            }
        }
        catch (Exception) { }
    }

    public static void Reset()
    {
        if (_mode != Mode.None) RestoreCaptured();
        _mode = Mode.None;
        _player = _target = null;
        _originalTarget = null;
        _writtenTarget = null;
        _camera = null; _follower = null; _paused = false;
        _generation++;
    }
}
