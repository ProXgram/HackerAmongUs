using HarmonyLib;

namespace HAU;

[HarmonyPatch(typeof(EngineerRole), nameof(EngineerRole.FixedUpdate))]
public static class EngineerRole_FixedUpdate
{
    public static void Postfix(EngineerRole __instance)
    {
        if(__instance.Player.AmOwner)
        {
            GameActions.HandleEngineerCheats(__instance);
        }
    }
}

[HarmonyPatch(typeof(ShapeshifterRole), nameof(ShapeshifterRole.FixedUpdate))]
public static class ShapeshifterRole_FixedUpdate
{
    public static void Postfix(ShapeshifterRole __instance)
    {
        try
        {
            if(__instance.Player.AmOwner)
            {
                GameActions.HandleShapeshifterCheats(__instance);
            }
        } catch { }
    }
}

[HarmonyPatch(typeof(ScientistRole), nameof(ScientistRole.Update))]
public static class ScientistRole_Update
{
    public static void Postfix(ScientistRole __instance)
    {
        if(__instance.Player.AmOwner)
        {
            GameActions.HandleScientistCheats(__instance);
        }
    }
}

[HarmonyPatch(typeof(TrackerRole), nameof(TrackerRole.FixedUpdate))]
public static class TrackerRole_FixedUpdate
{
    public static void Postfix(TrackerRole __instance)
    {
        if(__instance.Player.AmOwner)
        {
            GameActions.HandleTrackerCheats(__instance);
        }
    }
}

[HarmonyPatch(typeof(PhantomRole), nameof(PhantomRole.IsValidTarget))]
public static class PhantomRole_IsValidTarget
{

    public static void Postfix(PhantomRole __instance, NetworkedPlayerInfo target, ref bool __result)
    {
        if (CheatToggles.killVanished && (Utils.isHost || Utils.isFreePlay) && LocalRoleTargeting.CanOverride(__instance))
        {
            __result = Utils.IsValidTarget(target);
        }
    }
}

[HarmonyPatch(typeof(ImpostorRole), nameof(ImpostorRole.IsValidTarget))]
public static class ImpostorRole_IsValidTarget
{


    public static void Postfix(ImpostorRole __instance, NetworkedPlayerInfo target, ref bool __result)
    {
        if (CheatToggles.killAnyone && (Utils.isHost || Utils.isFreePlay) && LocalRoleTargeting.CanOverride(__instance))
        {
           __result = Utils.IsValidTarget(target);
        }
    }
}

[HarmonyPatch(typeof(ImpostorRole), nameof(ImpostorRole.FindClosestTarget))]
public static class ImpostorRole_FindClosestTarget
{

    public static bool Prefix(ImpostorRole __instance, ref PlayerControl __result)
    {
        if (!CheatToggles.killReach || !LocalRoleTargeting.CanOverride(__instance)) return true;

        __result = LocalRoleTargeting.FindClosest(__instance);

        return false;
    }
}

[HarmonyPatch(typeof(DetectiveRole), nameof(DetectiveRole.FindClosestTarget))]
public static class DetectiveRole_FindClosestTarget
{

    public static bool Prefix(DetectiveRole __instance, ref PlayerControl __result)
    {
        if (!CheatToggles.interrogateReach || !LocalRoleTargeting.CanOverride(__instance)) return true;

        __result = LocalRoleTargeting.FindClosest(__instance);

        return false;
    }
}

[HarmonyPatch(typeof(TrackerRole), nameof(TrackerRole.FindClosestTarget))]
public static class TrackerRole_FindClosestTarget
{

    public static bool Prefix(TrackerRole __instance, ref PlayerControl __result)
    {
        if (!CheatToggles.trackReach || !LocalRoleTargeting.CanOverride(__instance)) return true;

        __result = LocalRoleTargeting.FindClosest(__instance);

        return false;
    }
}




internal static class LocalRoleTargeting
{
    internal static bool CanOverride(RoleBehaviour role)
    {
        var local = PlayerControl.LocalPlayer;
        return role && local && local.AmOwner && local.Data && local.Data.Role &&
            !local.Data.IsDead && !local.Data.Role.IsDead && !local.Data.Disconnected &&
            local.Data.Role.Pointer == role.Pointer && role.Player && role.Player.AmOwner &&
            role.Player.Pointer == local.Pointer && local.Collider && GameData.Instance &&
            (Utils.isInGame || Utils.isFreePlay) && !Utils.isMeeting && !Utils.isExiling;
    }

    internal static PlayerControl FindClosest(RoleBehaviour role)
    {
        var local = PlayerControl.LocalPlayer;
        var players = Utils.GetPlayersSortedByDistance();
        if (players == null) return null;
        foreach (var player in players)
        {
            if (!player || player.Pointer == local.Pointer || !player.Data ||
                player.Data.Disconnected || !player.Data.Role || !player.Data.Object ||
                player.Data.Object.Pointer != player.Pointer || !player.Collider || !player.Collider.enabled)
                continue;
            if (role.IsValidTarget(player.Data)) return player;
        }
        return null;
    }
}
