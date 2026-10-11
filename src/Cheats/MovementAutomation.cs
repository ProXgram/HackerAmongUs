using System;
using System.Collections.Generic;
using UnityEngine;

namespace HAU;

public enum MovementMode { Off, TurboOrbit, ZigzagDash, TeleportYoYo, ShadowFollow, TaskWalker, LagWalk }

public static class MovementAutomation
{
    private const int MaximumJumps = 12;
    private const float OrbitRadius = 1.2f;
    private static PlayerPhysics _physics;
    private static IntPtr _motionPlayerPointer;
    private static IntPtr _playerPointer;
    private static IntPtr _shipPointer;
    private static bool _capturedSpeed;
    private static float _baseSpeed;
    private static PlayerControl _target;
    private static Vector2 _lastTargetPosition;
    private static Vector2 _targetDirection = Vector2.down;
    private static bool _observedTarget;
    private static Vector2 _pointA;
    private static Vector2 _pointB;
    private static double _nextJump;
    private static int _jumps;
    private static float _phase;
    private static double _blockedSince = -1d;
    private static double _nextNavigationRetry;
    private static bool _walkerWaitingForRound;
    private static Vector2 _progressPosition;
    private static Vector2 _progressGoal;
    private static bool _hasProgressGoal;
    private static double _progressAt = -1d;
    private static int _stuckRetries;
    private static double _recoveryUntil;
    private static double _headingSampleAt;
    private static bool _reportedNavigationIssue;
    private static Vector2 _followOffset, _followHeading;
    private static double _followGoalAt;
    private static bool _hasFollowGoal;
    private static float _orbitDirection = 1f;
    private static bool _waitingTargetFloor;
    private static double _nextTargetFloorRetry;
    private static Vector2 _failedGoalTarget;
    private static int _targetGoalAttempt, _goalChoiceCount;
    private static Vector2 _attemptTargetPosition;
    private static bool _hasAttemptTarget;
    private static double _lagNextJump = -1d, _lagLastClock = -1d;
    private static int _lagCorrectionIndex;
    private static Vector2 _lagLastPosition, _lagInputDirection;
    private static double _lagLastMovedAt;
    private static float _lagWalkDistance;
    private static readonly List<(double Time, Vector2 Position)> LagHistory = new(32);
    private static int _lagWalkingCorrections, _lagStationarySnaps;
    private static double _lagStationaryUntil;
    private static Vector2 _lagStationaryAnchor, _lagStationaryStep;

    public static bool Active => Mode != MovementMode.Off;
    public static bool LagStationaryPhase => Mode == MovementMode.LagWalk && _lagStationarySnaps > 0;
    public static bool NeedsGroundCollision => Mode == MovementMode.TaskWalker ||
        Mode == MovementMode.ShadowFollow || Mode == MovementMode.TurboOrbit || Mode == MovementMode.LagWalk;
    public static MovementMode Mode { get; private set; }
    public static string StatusText { get; private set; } = "Movement: stopped";
    public static string LastNavigationIssue { get; private set; } = "";
    public static string TargetName => _target && _target.Data != null
        ? _target.Data.PlayerName : "No player selected";
    public static PlayerControl SelectedPlayer => IsValidTarget() ? _target : null;
    public static bool HasPointA { get; private set; }
    public static bool HasPointB { get; private set; }

    public static void Start(MovementMode mode)
    {
        Stop();
        SprintHandler.Reset();
        var local = PlayerControl.LocalPlayer;
        if (mode == MovementMode.Off) return;
        if (!CanUseContext(local))
        {
            StatusText = "Movement: join a lobby or round first";
            return;
        }
        EnsureContext(local);
        if ((mode == MovementMode.TurboOrbit || mode == MovementMode.ShadowFollow) && !IsValidTarget())
            CycleTarget();
        if ((mode == MovementMode.TurboOrbit || mode == MovementMode.ShadowFollow) && !IsValidTarget())
        {
            StatusText = "Movement: another living player is needed";
            return;
        }
        if (mode == MovementMode.TeleportYoYo &&
            (!HasPointA || !HasPointB || Vector2.Distance(_pointA, _pointB) < 0.2f))
        {
            StatusText = "Yo-yo: save two different spots first";
            return;
        }
        Mode = mode;
        if (mode == MovementMode.TaskWalker)
        {
            CheatToggles.automaticTasks = false;
            TaskWalkerHandler.Reset();
            _walkerWaitingForRound = Utils.isLobby;
        }
        _phase = 0f;
        _reportedNavigationIssue = false;
        LastNavigationIssue = "";
        _observedTarget = false;
        _hasFollowGoal = false;
        _targetGoalAttempt = 0;
        _hasAttemptTarget = false;
        _orbitDirection = 1f;
        _waitingTargetFloor = false;
        _headingSampleAt = 0d;
        ResetProgress();
        _jumps = 0;
        _nextJump = Time.realtimeSinceStartupAsDouble + JumpInterval();
        StatusText = "Movement: close the menu to start " + mode;
    }

    public static void Stop()
    {
        bool wasWalker = Mode == MovementMode.TaskWalker;
        Pause();
        Mode = MovementMode.Off;
        _walkerWaitingForRound = false;
        if (wasWalker) TaskWalkerHandler.Reset();
        _blockedSince = -1d;
        _nextNavigationRetry = 0d;
        ResetProgress();
        StatusText = "Movement: stopped";
    }

    public static void Reset()
    {
        Stop();
        _playerPointer = IntPtr.Zero;
        _shipPointer = IntPtr.Zero;
        _target = null;
        _observedTarget = false;
        _targetDirection = Vector2.down;
        _hasFollowGoal = false;
        _targetGoalAttempt = 0;
        _hasAttemptTarget = false;
        _orbitDirection = 1f;
        _waitingTargetFloor = false;
        HasPointA = HasPointB = false;
    }

    public static void Pause()
    {
        ClearOwnMotion();
        ResetLagTiming();
        NavigationRouter.Reset();
        ResetProgress();
        _blockedSince = -1d;
        _nextNavigationRetry = 0d;
        if (Active) StatusText = "Movement: paused";
    }

    public static void CycleTarget()
    {
        var local = PlayerControl.LocalPlayer;
        if (!CanUseContext(local)) return;
        EnsureContext(local);
        var players = new List<PlayerControl>();
        foreach (var player in PlayerControl.AllPlayerControls)
            if (player && !player.AmOwner && player.Data != null &&
                !player.Data.IsDead && !player.Data.Disconnected)
                players.Add(player);
        players.Sort((left, right) => left.PlayerId.CompareTo(right.PlayerId));
        int index = players.FindIndex(player => _target && player.Pointer == _target.Pointer);
        _target = players.Count == 0 ? null : players[(index + 1) % players.Count];
        _observedTarget = false;
        _targetDirection = Vector2.down;
        _hasFollowGoal = false;
        _targetGoalAttempt = 0;
        _hasAttemptTarget = false;
        _orbitDirection = 1f;
        _waitingTargetFloor = false;
        _blockedSince = -1d;
        _nextNavigationRetry = 0d;
        NavigationRouter.Reset();
    }

    public static void SavePointA() => SavePoint(true);
    public static void SavePointB() => SavePoint(false);

    private static void SavePoint(bool first)
    {
        var local = PlayerControl.LocalPlayer;
        if (!CanUseContext(local)) return;
        EnsureContext(local);

        Vector2 point = local.transform.position;
        if (!IsFinite(point)) return;
        if (!NavigationRouter.CanStand(local.GetTruePosition()))
        {
            StatusText = "Yo-yo: save a spot on open floor";
            return;
        }
        if (first) { _pointA = point; HasPointA = true; }
        else { _pointB = point; HasPointB = true; }
        StatusText = first ? "Yo-yo: point A saved" : "Yo-yo: point B saved";
    }

    public static void FixedTick(PlayerPhysics physics)
    {
        if (!Active) return;
        try
        {
            var local = PlayerControl.LocalPlayer;
            if (!local) { Reset(); return; }

            if (!physics || !physics.AmOwner || !local || !local.MyPhysics ||
                physics.Pointer != local.MyPhysics.Pointer || !physics.myPlayer ||
                physics.myPlayer.Pointer != local.Pointer) return;
            if (_physics && _physics.Pointer != physics.Pointer) ClearOwnMotion();
            bool awaitingShip = _shipPointer == IntPtr.Zero &&
                (Mode == MovementMode.ShadowFollow || Mode == MovementMode.TurboOrbit);
            if ((Mode == MovementMode.TaskWalker && _walkerWaitingForRound || awaitingShip) && Utils.isClient &&
                Utils.isInGame && !ShipStatus.Instance && local.Data != null &&
                !local.Data.IsDead && !local.Data.Disconnected)
            {
                Pause();
                StatusText = "Movement: waiting for the round to load";
                return;
            }
            if (!CanUseContext(local)) { Reset(); return; }
            EnsureContext(local);
            if (!Active) return;
            if (HAUPlugin.isPanicked || local.Data.IsDead || local.Data.Disconnected)
            {
                Stop();
                return;
            }
            if (!Application.isFocused || MenuUI.isGUIActive || !local.CanMove ||
                local.inVent || local.onLadder || local.inMovingPlat ||
                Utils.isMeeting || Utils.isExiling || !HudManager.InstanceExists)
            {
                Pause();
                return;
            }
            var hud = HudManager.Instance;
            if (!hud || hud.IsIntroDisplayed || (hud.Chat && hud.Chat.IsOpenOrOpening))
            {
                Pause();
                return;
            }


            if (NeedsGroundCollision && local.Collider && !local.Collider.enabled)
                local.Collider.enabled = true;
            _physics = physics;
            _motionPlayerPointer = local.Pointer;
            double now = Time.realtimeSinceStartupAsDouble;
            if (!double.IsFinite(now)) { Stop(); return; }
            if (Mode == MovementMode.LagWalk)
            {
                LagTick(local, physics, hud, now);
                return;
            }
            if (Mode == MovementMode.TeleportYoYo)
            {
                physics.SetNormalizedVelocity(Vector2.zero);
                if (now < _nextJump) return;
                if (!HasPointA || !HasPointB || !local.NetTransform) { Stop(); return; }
                Vector2 destination = (_jumps & 1) == 0 ? _pointA : _pointB;
                Vector2 positionOffset = local.GetTruePosition() - (Vector2)local.transform.position;
                if (Vector2.Distance(_pointA, _pointB) < 0.2f ||
                    !NavigationRouter.CanStand(destination + positionOffset))
                {
                    Stop();
                    StatusText = "Yo-yo: saved destination is blocked";
                    return;
                }
                local.NetTransform.RpcSnapTo(destination);
                _jumps++;

                _nextJump = now + JumpInterval();
                StatusText = "Yo-yo: jump " + _jumps + "/" + MaximumJumps;
                if (_jumps >= MaximumJumps) { Stop(); StatusText = "Yo-yo: finished 12 jumps"; }
                return;
            }
            if (now < _nextNavigationRetry) { ClearOwnMotion(); return; }

            Vector2 from = local.GetTruePosition();
            Vector2 direction;
            float targetDistance = 0f;
            bool movingTarget = Mode == MovementMode.ShadowFollow || Mode == MovementMode.TurboOrbit;
            string navigationStatus = "";
            string activityStatus = "";
            _phase += Mathf.Clamp(Time.fixedDeltaTime, 0f, 0.1f);
            if (Mode == MovementMode.ZigzagDash)
            {
                float side = ((int)(_phase / 0.25f) & 1) == 0 ? 1f : -1f;
                Vector2 forward = hud.joystick != null ? hud.joystick.DeltaL : Vector2.zero;
                if (forward.sqrMagnitude > 0.01f)
                {
                    forward.Normalize();
                    direction = forward * 0.5f + new Vector2(-forward.y, forward.x) * (0.866f * side);
                }
                else direction = new Vector2(side, 0f);
            }
            else if (Mode == MovementMode.TaskWalker)
            {
                if (!TaskWalkerHandler.TryGetDestination(out var goal, out var arrivalDistance, out var taskStatus))
                {
                    Pause();
                    if (TaskWalkerHandler.IsFinished) Stop();
                    StatusText = taskStatus;
                    return;
                }
                if (!IsFinite(goal) || !IsFinite(arrivalDistance) || arrivalDistance < 0f)
                {
                    Stop();
                    StatusText = "Task Walker: invalid destination";
                    return;
                }
                if (Vector2.Distance(from, goal) <= arrivalDistance)
                {
                    Pause();
                    TaskWalkerHandler.OnArrived();
                    StatusText = taskStatus;
                    return;
                }
                if (!IsFinite(physics.TrueSpeed) || Mathf.Abs(physics.TrueSpeed) < 0.001f)
                {
                    Pause();
                    StatusText = "Task Walker: increase your movement speed";
                    return;
                }
                if (!CheckProgress(from, goal, now, Mathf.Abs(physics.TrueSpeed)))
                {
                    ClearOwnMotion();
                    NavigationRouter.Reset();
                    if (_stuckRetries >= 5)
                    {
                        RetryOrSkipTask();
                        _nextNavigationRetry = now + 0.2d;
                        return;
                    }
                    _recoveryUntil = now + 0.45d;


                    _physics = physics;
                    _motionPlayerPointer = local.Pointer;
                }
                if (now < _recoveryUntil && !NavigationRouter.CanStand(from))
                    direction = NavigationRouter.RecoverDirection(from, out navigationStatus);
                else
                {
                    _recoveryUntil = 0d;
                    direction = NavigationRouter.Direction(from, goal, out navigationStatus);
                }
                activityStatus = taskStatus;
                if (!IsFinite(Time.fixedDeltaTime) || Time.fixedDeltaTime <= 0f) { Stop(); return; }
            }
            else
            {
                if (!IsValidTarget()) { Stop(); StatusText = "Movement: selected player unavailable"; return; }
                Vector2 targetPosition = _target.GetTruePosition();
                if (!IsFinite(targetPosition)) { Stop(); return; }
                if (!_hasAttemptTarget || (targetPosition - _attemptTargetPosition).sqrMagnitude > 1f)
                {
                    _targetGoalAttempt = 0;
                    _hasFollowGoal = false;
                    _attemptTargetPosition = targetPosition;
                    _hasAttemptTarget = true;
                }
                if (!_observedTarget)
                {
                    _lastTargetPosition = targetPosition;
                    _headingSampleAt = now;
                    _observedTarget = true;


                    var towardTarget = targetPosition - from;
                    if (towardTarget.sqrMagnitude > 0.0001f)
                        _targetDirection = towardTarget.normalized;
                }
                Vector2 observedMotion = targetPosition - _lastTargetPosition;


                if (observedMotion.sqrMagnitude >= 0.08f * 0.08f)
                {
                    float previousAngle = Mathf.Atan2(_targetDirection.y, _targetDirection.x);
                    float nextAngle = Mathf.Atan2(observedMotion.y, observedMotion.x);
                    const float pi = (float)Math.PI;
                    float turn = (nextAngle - previousAngle + 3f * pi) % (2f * pi) - pi;
                    float blend = Mathf.Clamp((float)Math.Max(0d, now - _headingSampleAt) * 8f, 0f, 1f);
                    float heading = previousAngle + turn * blend;
                    _targetDirection = new Vector2(Mathf.Cos(heading), Mathf.Sin(heading));
                    _lastTargetPosition = targetPosition;
                    _headingSampleAt = now;
                }
                Vector2 goal;
                if (_waitingTargetFloor && now < _nextTargetFloorRetry &&
                    (targetPosition - _failedGoalTarget).sqrMagnitude < 0.04f)
                {
                    ClearOwnMotion();
                    StatusText = "Movement: waiting for the player to reach open floor";
                    return;
                }
                bool hasGoal = Mode == MovementMode.TurboOrbit
                    ? TryOrbitGoal(from, targetPosition, out goal)
                    : TryFollowGoal(from, targetPosition, now, out goal);
                if (!hasGoal)
                {
                    ClearOwnMotion();
                    _blockedSince = -1d;
                    _nextNavigationRetry = 0d;
                    NavigationRouter.Reset();
                    _waitingTargetFloor = true;
                    _nextTargetFloorRetry = now + 0.25d;
                    _failedGoalTarget = targetPosition;
                    LastNavigationIssue = "No open spot beside the selected player";
                    StatusText = "Movement: waiting for the player to reach open floor";
                    return;
                }
                _waitingTargetFloor = false;
                targetDistance = Vector2.Distance(from, goal);
                if (targetDistance <= 0.12f)
                {
                    ClearOwnMotion();
                    _blockedSince = -1d;
                    StatusText = Mode + ": in position";
                    return;
                }
                direction = NavigationRouter.DirectionToMovingTarget(from, goal, out navigationStatus);
            }

            if (!IsFinite(direction) || direction.sqrMagnitude < 0.0001f)
            {
                bool alternateGoal = movingTarget && NavigationRouter.MovingRouteFailed &&
                    _targetGoalAttempt + 1 < _goalChoiceCount;
                if (alternateGoal)
                {



                    _targetGoalAttempt++;
                    _hasFollowGoal = false;
                    NavigationRouter.Reset();
                }
                if (movingTarget && !_reportedNavigationIssue)
                {
                    _reportedNavigationIssue = true;
                    if (!NavigationRouter.IsMovingRoutePending) LastNavigationIssue = navigationStatus;
                    var targetPoint = _target.GetTruePosition();
                    HAUPlugin.Log?.LogInfo("Movement route " + Mode + ": " + navigationStatus +
                        "; own=(" + from.x + "," + from.y + "); target=(" + targetPoint.x + "," + targetPoint.y + ")" +
                        "; " + NavigationRouter.Diagnostics);
                }


                ClearOwnMotion();


                if (!movingTarget)
                {
                    NavigationRouter.Reset();
                    ResetProgress();
                }
                if (_blockedSince < 0d) _blockedSince = now;
                _nextNavigationRetry = alternateGoal ? now + 0.1d : movingTarget ? 0d : now + 0.5d;
                StatusText = alternateGoal ? "Movement: trying another approach to the player" :
                    string.IsNullOrEmpty(navigationStatus) ? "Movement: path blocked; retrying" : navigationStatus;
                double blockedFor = now - _blockedSince;
                if (blockedFor >= 5d &&
                    (!movingTarget || (!NavigationRouter.IsMovingRoutePending && !alternateGoal) || blockedFor >= 30d))
                {
                    if (Mode == MovementMode.TaskWalker)
                    {
                        RetryOrSkipTask();
                        _blockedSince = -1d;
                        _nextNavigationRetry = now + 0.5d;
                    }
                    else { Stop(); StatusText = "Movement: no route; stopped"; }
                }
                return;
            }
            _nextNavigationRetry = 0d;
            if (movingTarget || Mode == MovementMode.ZigzagDash)
            {
                if (!_capturedSpeed)
                {
                    if (!IsFinite(physics.Speed)) { Stop(); return; }
                    _baseSpeed = physics.Speed;
                    _capturedSpeed = true;
                }
                float multiplier = movingTarget ? HAUPlugin.targetMovementMultiplier.Value : HAUPlugin.stuntMultiplier.Value;
                if (!IsFinite(multiplier)) multiplier = movingTarget ? 3f : 2f;
                if (movingTarget && targetDistance > 2f) multiplier = Mathf.Max(2f, multiplier);
                physics.Speed = Mathf.Clamp(_baseSpeed * Mathf.Clamp(multiplier, 1f, 4f), -20f, 20f);
            }
            if (direction.sqrMagnitude > 1f) direction.Normalize();
            if (Mode == MovementMode.TaskWalker || movingTarget)
            {


                if (!IsFinite(Time.fixedDeltaTime) || Time.fixedDeltaTime <= 0f ||
                    !IsFinite(physics.TrueSpeed)) { Stop(); return; }
                if (Mathf.Abs(physics.TrueSpeed) < 0.001f) { Pause(); return; }
                float dt = Mathf.Max(0.001f, Time.fixedDeltaTime);
                float walkSpeed;
                if (!movingTarget)
                    walkSpeed = Mathf.Min(2.5f, 0.06f / dt);
                else
                {



                    float steeringDistance = NavigationRouter.SteeringDistance;
                    if (float.IsNaN(steeringDistance) || steeringDistance <= 0f)
                    { ClearOwnMotion(); StatusText = "Movement: waiting for a clear ground route"; return; }
                    bool recovery = NavigationRouter.IsRecovering;
                    walkSpeed = recovery ? Mathf.Min(2.5f, 0.06f / dt)
                        : Mathf.Min(12f, 0.24f / dt);
                    if (Mode == MovementMode.ShadowFollow)
                        walkSpeed = Mathf.Min(walkSpeed, targetDistance * 6f);
                    walkSpeed = Mathf.Min(walkSpeed, Mathf.Abs(physics.TrueSpeed));
                    float step = Mathf.Min(walkSpeed * dt, Mathf.Min(targetDistance, steeringDistance));
                    if (!recovery)
                    {



                        var unit = direction.normalized;
                        bool clear = false;
                        for (int attempt = 0; attempt < 4 && step > 0.001f; attempt++)
                        {
                            if (NavigationRouter.CanTravel(from, from + unit * step))
                            { clear = true; break; }
                            step *= 0.5f;
                        }
                        if (!clear)
                        {
                            ClearOwnMotion();
                            NavigationRouter.Reset();
                            if (_blockedSince < 0d) _blockedSince = now;
                            _nextNavigationRetry = now + 0.1d;
                            StatusText = "Movement: checking a blocked corner";
                            if (now - _blockedSince >= 5d)
                            { Stop(); StatusText = "Movement: blocked corner; stopped"; }
                            return;
                        }
                    }
                    walkSpeed = step / dt;
                }
                direction *= Mathf.Min(1f, walkSpeed / Mathf.Abs(physics.TrueSpeed));
            }


            if ((Mode == MovementMode.TaskWalker || movingTarget ? physics.TrueSpeed : physics.Speed) < 0f) direction = -direction;
            physics.SetNormalizedVelocity(direction);
            _blockedSince = -1d;
            LastNavigationIssue = "";
            StatusText = Mode == MovementMode.TaskWalker ? activityStatus : Mode + ": active";
        }
        catch (Exception error)
        {
            HAUPlugin.Log?.LogWarning("Movement local error: " + error.GetType().Name + ": " + error.Message);
            Stop();
            StatusText = "Movement: stopped after a local error";
        }
    }

    private static void ResetLagTiming()
    {
        _lagNextJump = _lagLastClock = -1d;
        _lagCorrectionIndex = 0;
        _lagWalkDistance = 0f;
        _lagLastMovedAt = -1d;
        _lagLastPosition = _lagInputDirection = Vector2.zero;
        _lagWalkingCorrections = _lagStationarySnaps = 0;
        _lagStationaryUntil = -1d;
        _lagStationaryAnchor = _lagStationaryStep = Vector2.zero;
        LagHistory.Clear();
    }

    private static double LagInterval()
    {
        float interval = HAUPlugin.lagInterval.Value;
        if (!IsFinite(interval)) interval = 0.75f;
        interval = Mathf.Clamp(interval, 0.5f, 2f);


        float variation = (_lagCorrectionIndex % 4) switch
        { 1 => 1.4f, 2 => 1.1f, 3 => 1.7f, _ => 1f };
        return Math.Min(2d, interval * (double)variation);
    }

    private static void BeginLagHistory(Vector2 position, Vector2 input, double now)
    {
        ResetLagTiming();
        _lagLastPosition = position;
        _lagInputDirection = input;
        _lagLastClock = _lagLastMovedAt = now;
        _lagNextJump = now + LagInterval();
        LagHistory.Add((now, position));
    }

    private static void LagTick(PlayerControl local, PlayerPhysics physics, HudManager hud, double now)
    {


        Vector2 input = hud.joystick != null ? hud.joystick.DeltaL : Vector2.zero;
        if (!IsFinite(input) || !IsFinite(input.sqrMagnitude) || input.sqrMagnitude <= 0.01f)
        {
            ResetLagTiming();
            StatusText = "Lag mode: hold a movement key for mixed walking and teleports";
            return;
        }
        if (!local.NetTransform) { Stop(); StatusText = "Lag mode: player unavailable"; return; }
        Vector2 from = local.GetTruePosition();
        Vector2 transformPosition = local.transform.position;
        if (!IsFinite(from) || !IsFinite(transformPosition))
        { Stop(); StatusText = "Lag mode: invalid position"; return; }
        Vector2 direction = input.normalized;
        double elapsed = now - _lagLastClock;
        float travelled = Vector2.Distance(from, _lagLastPosition);
        float speed = Mathf.Abs(physics.TrueSpeed);
        if (_lagNextJump < 0d || elapsed < 0d || elapsed > 0.5d ||
            !IsFinite(travelled) || !IsFinite(speed) ||
            travelled > speed * elapsed + 0.2d ||
            (LagStationaryPhase && ((from - _lagStationaryAnchor).sqrMagnitude > 0.04f || now >= _lagStationaryUntil)) ||
            direction.x * _lagInputDirection.x + direction.y * _lagInputDirection.y < 0.8f)
        {


            BeginLagHistory(from, direction, now);
            StatusText = "Lag mode: walking normally";
            return;
        }
        _lagLastClock = now;
        _lagLastPosition = from;


        if (LagStationaryPhase)
        {
            if (now < _lagNextJump)
            {
                physics.SetNormalizedVelocity(Vector2.zero);
                return;
            }
            Vector2 stationaryStep = _lagStationarySnaps == 2 ? -_lagStationaryStep : _lagStationaryStep;
            bool sentStationary = TryLagSnap(local, from, transformPosition, ref stationaryStep);
            _lagStationaryAnchor = sentStationary ? from + stationaryStep : from;
            _lagLastPosition = _lagStationaryAnchor;
            _lagStationarySnaps--;
            _lagCorrectionIndex++;
            if (_lagStationarySnaps == 0)
            {


                BeginLagHistory(_lagStationaryAnchor, direction, now);
                StatusText = "Lag mode: walking again";
            }
            else
            {
                _lagNextJump = now + LagInterval();
                physics.SetNormalizedVelocity(Vector2.zero);
                StatusText = sentStationary ? "Lag mode: stationary teleport; one more then walk" :
                    "Lag mode: teleport blocked; brief pause then walk";
            }
            return;
        }
        _lagWalkDistance += travelled;
        if (travelled > 0.001f) _lagLastMovedAt = now;
        if (now - _lagLastMovedAt > 0.18d)
        {
            BeginLagHistory(from, direction, now);
            StatusText = "Lag mode: waiting for normal walking";
            return;
        }
        while (LagHistory.Count > 0 && now - LagHistory[0].Time > 1d)
            LagHistory.RemoveAt(0);
        if (LagHistory.Count == 0 || now - LagHistory[LagHistory.Count - 1].Time >= 0.04d)
        {
            if (LagHistory.Count >= 32) LagHistory.RemoveAt(0);
            LagHistory.Add((now, from));
        }
        if (now < _lagNextJump) return;
        _lagNextJump = now + LagInterval();


        if (_lagWalkDistance < 0.25f) return;
        float limit = HAUPlugin.lagJumpDistance.Value;
        if (!IsFinite(limit)) limit = 1.25f;
        limit = Mathf.Clamp(limit, 0.25f, 2f);
        Vector2 recentMotion = Vector2.zero;
        foreach (var sample in LagHistory)
        {
            double age = now - sample.Time;
            if (age > 0.45d || age < 0.12d) continue;
            Vector2 motion = from - sample.Position;
            float distance = motion.magnitude;
            if (!IsFinite(distance) || distance < 0.08f) continue;


            recentMotion = motion * (Mathf.Min(distance, limit) / distance);
            break;
        }
        if (recentMotion.sqrMagnitude < 0.0064f) return;



        bool forward = _lagCorrectionIndex % 5 == 1 || _lagCorrectionIndex % 5 == 4;
        Vector2 displacement = forward ? recentMotion : -recentMotion;
        bool sent = TryLagSnap(local, from, transformPosition, ref displacement);
        _lagCorrectionIndex++;
        _lagNextJump = now + LagInterval();


        LagHistory.Clear();
        _lagWalkDistance = 0f;
        _lagLastPosition = sent ? from + displacement : from;
        _lagLastMovedAt = now;
        LagHistory.Add((now, _lagLastPosition));
        StatusText = sent
            ? forward ? "Lag mode: teleported forward; keep walking" : "Lag mode: snapped backward; keep walking"
            : "Lag mode: correction blocked; keep walking";
        if (sent && ++_lagWalkingCorrections >= 2)
        {
            _lagStationarySnaps = 2;
            _lagStationaryAnchor = _lagLastPosition;
            _lagStationaryStep = recentMotion;
            _lagStationaryUntil = now + 4.1d;
            physics.SetNormalizedVelocity(Vector2.zero);
            StatusText = "Lag mode: brief freeze with two teleports, then walking";
        }
    }

    private static bool TryLagSnap(PlayerControl local, Vector2 from, Vector2 transformPosition, ref Vector2 displacement)
    {
        float limit = HAUPlugin.lagJumpDistance.Value;
        if (!IsFinite(limit)) limit = 1.25f;
        limit = Mathf.Clamp(limit, 0.25f, 2f);
        float distance = displacement.magnitude;
        if (distance > limit) displacement *= limit / distance;
        for (int attempt = 0; attempt < 5 && displacement.sqrMagnitude >= 0.0064f; attempt++)
        {
            if (NavigationRouter.CanTravel(from, from + displacement))
            {
                local.NetTransform.RpcSnapTo(transformPosition + displacement);
                return true;
            }
            displacement *= 0.5f;
        }
        return false;
    }

    private static void ClearOwnMotion()
    {
        var previous = _physics;
        bool restore = _capturedSpeed;
        float speed = _baseSpeed;
        IntPtr owner = _motionPlayerPointer;
        _physics = null;
        _capturedSpeed = false;
        _motionPlayerPointer = IntPtr.Zero;
        try
        {
            if (previous && previous.AmOwner && previous.myPlayer &&
                previous.myPlayer.AmOwner && previous.myPlayer.Pointer == owner)
            {
                if (restore) previous.Speed = speed;
                previous.SetNormalizedVelocity(Vector2.zero);
            }
        }
        catch (Exception) { }
    }

    private static bool TryFollowGoal(Vector2 from, Vector2 target, double now, out Vector2 goal)
    {
        goal = target;
        if (_hasFollowGoal && now >= _followGoalAt && now - _followGoalAt < 0.25d &&
            (_targetDirection - _followHeading).sqrMagnitude < 0.1f &&
            NavigationRouter.CanStand(target + _followOffset))
        { goal = target + _followOffset; return true; }

        float bearing = Mathf.Atan2(-_targetDirection.y, -_targetDirection.x);
        var choices = new List<(float Score, Vector2 Point)>(24);
        float bestScore = float.PositiveInfinity;


        for (int radiusIndex = 0; radiusIndex < 3; radiusIndex++)
        {
            float radius = radiusIndex == 0 ? 0.65f : radiusIndex == 1 ? 1f : 0.35f;
            for (int step = 0; step < 8; step++)
            {
                int turn = step == 0 ? 0 : ((step + 1) / 2) * ((step & 1) == 1 ? 1 : -1);
                float angle = bearing + turn * ((float)Math.PI / 4f);
                Vector2 candidate = target + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                if (!NavigationRouter.CanStand(candidate)) continue;
                bool direct = NavigationRouter.CanTravel(from, candidate);
                float score = (direct ? 0f : 100f) + Math.Abs(turn) * 0.15f + radiusIndex * 0.2f +
                    Vector2.Distance(from, candidate) * 0.02f;
                choices.Add((score, candidate));
                if (score < bestScore) bestScore = score;
                if (radiusIndex == 0 && step == 0 && direct)
                { _targetGoalAttempt = 0; break; }
            }
            if (choices.Count > 0 && bestScore < 0.2f) break;
        }
        _goalChoiceCount = choices.Count;
        _hasFollowGoal = choices.Count > 0;
        if (!_hasFollowGoal) return false;
        choices.Sort((left, right) => left.Score.CompareTo(right.Score));
        goal = choices[Math.Min(_targetGoalAttempt, choices.Count - 1)].Point;
        _followOffset = goal - target;
        _followHeading = _targetDirection;
        _followGoalAt = now;
        return true;
    }

    private static bool TryOrbitGoal(Vector2 from, Vector2 target, out Vector2 goal)
    {
        var radial = from - target;
        float angle = radial.sqrMagnitude > 0.0001f ? Mathf.Atan2(radial.y, radial.x) : 0f;
        goal = target;
        var choices = new List<Vector2>(18);
        for (int arc = 1; arc <= 3; arc++)
        for (int side = 0; side < 2; side++)
        {
            float turn = side == 0 ? _orbitDirection : -_orbitDirection;
            float bearing = angle + turn * (0.35f * arc);
            for (int radiusIndex = 0; radiusIndex < 3; radiusIndex++)
            {
                float radius = radiusIndex == 0 ? OrbitRadius : radiusIndex == 1 ? 0.9f : 0.6f;
                Vector2 candidate = target + new Vector2(Mathf.Cos(bearing), Mathf.Sin(bearing)) * radius;
                if (!NavigationRouter.CanStand(candidate)) continue;
                choices.Add(candidate);
                if (!NavigationRouter.CanTravel(from, candidate)) continue;
                goal = candidate;
                _orbitDirection = turn;
                _targetGoalAttempt = 0;
                _goalChoiceCount = 1;
                return true;
            }
        }
        _goalChoiceCount = choices.Count;
        if (choices.Count == 0) return false;
        goal = choices[Math.Min(_targetGoalAttempt, choices.Count - 1)];


        return true;
    }

    private static void ResetProgress()
    {
        _progressAt = -1d;
        _progressPosition = default;
        _progressGoal = default;
        _hasProgressGoal = false;
        _stuckRetries = 0;
        _recoveryUntil = 0d;
    }

    private static bool CheckProgress(Vector2 from, Vector2 goal, double now, float speed)
    {
        if (!_hasProgressGoal || (goal - _progressGoal).sqrMagnitude > 0.0001f)
        {
            ResetProgress();
            _progressGoal = goal;
            _hasProgressGoal = true;
        }
        if (_progressAt < 0d || now < _progressAt)
        {
            _progressPosition = from;
            _progressAt = now;
            return true;
        }
        var needed = Mathf.Min(0.06f, speed * 0.25f);
        if ((from - _progressPosition).sqrMagnitude >= needed * needed)
        {
            _progressPosition = from;
            _progressAt = now;
            return true;
        }
        if (now - _progressAt < 1d) return true;
        _progressPosition = from;
        _progressAt = now;
        _stuckRetries++;
        return false;
    }

    private static void RetryOrSkipTask()
    {
        var alternate = TaskWalkerHandler.TryAlternateApproach();
        if (!alternate) TaskWalkerHandler.SkipCurrent("No clear walking route to this task step");
        ResetProgress();
        NavigationRouter.Reset();
        StatusText = alternate ? "Task Walker: trying another side of the task"
            : "Task Walker: skipped an unreachable step";
    }

    private static bool CanUseContext(PlayerControl local) => Utils.isClient && local &&
        local.AmOwner && local.Data != null && !local.Data.IsDead && !local.Data.Disconnected &&
        (Utils.isLobby || Utils.isInGame || Utils.isFreePlay) && (Utils.isLobby || ShipStatus.Instance);

    private static void EnsureContext(PlayerControl local)
    {
        var ship = ShipStatus.Instance;
        IntPtr shipPointer = ship ? ship.Pointer : IntPtr.Zero;
        if (_playerPointer != IntPtr.Zero && (_playerPointer != local.Pointer || _shipPointer != shipPointer))
        {


            var startWalkerRound = Mode == MovementMode.TaskWalker && _walkerWaitingForRound && shipPointer != IntPtr.Zero;
            bool keepTargetMode = _playerPointer == local.Pointer && _shipPointer == IntPtr.Zero &&
                shipPointer != IntPtr.Zero &&
                (Mode == MovementMode.ShadowFollow || Mode == MovementMode.TurboOrbit) && IsValidTarget();
            var previousMode = Mode;
            var previousTarget = _target;
            Reset();
            if (startWalkerRound)
            {
                Mode = MovementMode.TaskWalker;
                CheatToggles.automaticTasks = false;
                TaskWalkerHandler.Reset();
            }
            else if (keepTargetMode)
            {
                Mode = previousMode;
                _target = previousTarget;
            }
        }
        _playerPointer = local.Pointer;
        _shipPointer = shipPointer;
    }

    private static bool IsValidTarget()
    {
        if (!_target || _target.AmOwner || _target.Data == null ||
            _target.Data.IsDead || _target.Data.Disconnected) return false;
        foreach (var player in PlayerControl.AllPlayerControls)
            if (player && player.Pointer == _target.Pointer) return true;
        return false;
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private static bool IsFinite(Vector2 value) => IsFinite(value.x) && IsFinite(value.y);

    private static float JumpInterval()
    {
        float interval = HAUPlugin.yoyoInterval.Value;
        return IsFinite(interval) ? Mathf.Clamp(interval, 1f, 5f) : 1f;
    }
}
