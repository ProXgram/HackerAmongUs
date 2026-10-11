using HarmonyLib;

namespace HAU;

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.FixedUpdate))]
public static class ShipStatus_FixedUpdate
{
    public static void Postfix(ShipStatus __instance)
    {
        SabotageTools.Process(__instance);
        GameActions.OpenSabotageMapCheat();

        GameActions.CloseMeetingCheat();
        GameActions.SkipMeetingCheat();
        GameActions.CallMeetingCheat();
        GameActions.WalkInVentCheat();
        GameActions.KickVentsCheat();

        PlayerActions.ReportBodyPPM();
    }
}

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.FixedUpdate))]
public static class FungleShipStatus_FixedUpdate
{
    public static void Postfix(ShipStatus __instance)
    {
        if (!__instance) return;
        var fungle = __instance.TryCast<FungleShipStatus>();
        if (fungle) SabotageTools.ProcessFungle(fungle);
    }
}
