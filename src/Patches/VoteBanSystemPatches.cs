using HarmonyLib;

namespace HAU;

[HarmonyPatch(typeof(VoteBanSystem), nameof(VoteBanSystem.AddVote))]
public static class VoteBanSystem_AddVote
{

    public static bool Prefix(VoteBanSystem __instance, int srcClient, int clientId)
    {
        if (!Utils.isHost) return true;

        if (AmongUsClient.Instance.ClientId == srcClient)
        {
            AmongUsClient.Instance.KickPlayer(clientId, false);
        }

        return false;
    }
}

[HarmonyPatch(typeof(VoteBanSystem), nameof(VoteBanSystem.CmdAddVote))]
public static class VoteBanSystem_CmdAddVote
{

    public static bool Prefix()
    {
        return !Utils.isHost;
    }
}
