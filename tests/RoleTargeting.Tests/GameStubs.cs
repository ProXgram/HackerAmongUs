namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class HarmonyPatch : Attribute
    {
        public HarmonyPatch(Type type, string method) { }
    }
}

namespace UnityEngine
{
    public class Object
    {
        private static long _nextPointer;
        public IntPtr Pointer { get; set; } = new(Interlocked.Increment(ref _nextPointer));
        public bool Destroyed { get; set; }
        public static implicit operator bool(Object value) => value is not null && !value.Destroyed;
    }
}

public sealed class Collider : UnityEngine.Object
{
    public bool enabled = true;
}

public sealed class PlayerControl : UnityEngine.Object
{
    public static PlayerControl LocalPlayer;
    public bool AmOwner;
    public bool Visible = true;
    public NetworkedPlayerInfo Data;
    public Collider Collider = new();
    public float Position;
}

public sealed class NetworkedPlayerInfo : UnityEngine.Object
{
    public bool IsDead;
    public bool Disconnected;
    public PlayerControl Object;
    public RoleBehaviour Role;
}

public class RoleBehaviour : UnityEngine.Object
{
    public PlayerControl Player;
    public bool IsDead;
    public bool CanBeKilled = true;
    public Func<NetworkedPlayerInfo, bool> Validation;
    public int ValidationCalls;
    public int TypedValidationCalls;
    public virtual bool IsValidTarget(NetworkedPlayerInfo target)
    {
        ValidationCalls++;
        if (!target || !target.Object || !target.Role)
            throw new InvalidOperationException("Malformed candidate reached native validation.");
        return Validation?.Invoke(target) ?? (!target.IsDead && !target.Disconnected && target.Role.CanBeKilled);
    }
}

public class EngineerRole : RoleBehaviour { public void FixedUpdate() { } }
public class ShapeshifterRole : RoleBehaviour { public void FixedUpdate() { } }
public class ScientistRole : RoleBehaviour { public void Update() { } }
public class PhantomRole : RoleBehaviour { }
public class ImpostorRole : RoleBehaviour
{
    public PlayerControl FindClosestTarget() => null;
    public override bool IsValidTarget(NetworkedPlayerInfo target)
    {
        TypedValidationCalls++;
        bool result = base.IsValidTarget(target);
        HAU.ImpostorRole_IsValidTarget.Postfix(this, target, ref result);
        return result;
    }
}
public class DetectiveRole : RoleBehaviour
{
    public PlayerControl FindClosestTarget() => null;
    public override bool IsValidTarget(NetworkedPlayerInfo target)
    { TypedValidationCalls++; return base.IsValidTarget(target); }
}
public class TrackerRole : RoleBehaviour
{
    public void FixedUpdate() { }
    public PlayerControl FindClosestTarget() => null;
    public override bool IsValidTarget(NetworkedPlayerInfo target)
    { TypedValidationCalls++; return base.IsValidTarget(target); }
}

public sealed class GameData : UnityEngine.Object
{
    public static GameData Instance = new();
}

namespace HAU
{
    public static class CheatToggles
    {
        public static bool killReach, interrogateReach, trackReach, killAnyone, killVanished;
    }
    public static class Utils
    {
        public static bool isInGame = true, isFreePlay, isMeeting, isExiling, isHost;
        public static List<PlayerControl> Players = new();
        public static int SnapshotReads;
        public static List<PlayerControl> GetPlayersSortedByDistance()
        {
            SnapshotReads++;
            return Players?.OrderBy(player => player is null ? float.NegativeInfinity : player.Position).ToList();
        }
        public static bool IsValidTarget(NetworkedPlayerInfo target) => target && !target.Disconnected &&
            target.Object && target.Object.Visible && target.Object.Pointer != PlayerControl.LocalPlayer.Pointer &&
            target.Role && (CheatToggles.killAnyone || (!target.IsDead && target.Role.CanBeKilled));
    }
    public static class GameActions
    {
        public static void HandleEngineerCheats(EngineerRole role) { }
        public static void HandleShapeshifterCheats(ShapeshifterRole role) { }
        public static void HandleScientistCheats(ScientistRole role) { }
        public static void HandleTrackerCheats(TrackerRole role) { }
    }
}
