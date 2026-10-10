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
    public static void Postfix(FungleShipStatus __instance)
    {
        SabotageTools.ProcessFungle(__instance);
    }
}
