using HarmonyLib;

namespace HAU;

[HarmonyPatch(typeof(KillButton), nameof(KillButton.DoClick))]
public static class KillButton_DoClick
{
    public static bool Prefix() =>
        !LocalImpostorHandler.BlockOwnKill(PlayerControl.LocalPlayer);
}



[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcMurderPlayer))]
public static class PlayerControl_RpcMurderPlayer
{
    public static bool Prefix(PlayerControl __instance) =>
        !LocalImpostorHandler.BlockOwnKill(__instance);
}
