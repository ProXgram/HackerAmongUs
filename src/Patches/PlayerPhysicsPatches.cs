using System;
using HarmonyLib;
using UnityEngine;

namespace HAU;

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.LateUpdate))]
public static class PlayerPhysics_LateUpdate
{
    public static void Postfix(PlayerPhysics __instance)
    {
        InformationTools.PlayerNametags(__instance);
        InformationTools.SeeGhostsCheat(__instance);

        GameActions.NoClipCheat();
        GameActions.ProtectCheat();
        GameActions.KillAllCheat();
        GameActions.KillAllCrewCheat();
        GameActions.KillAllImpsCheat();
        GameActions.ForceStartGameCheat();
        GameActions.TeleportCursorCheat();
        GameActions.CompleteMyTasksCheat();
        GameActions.PlayAnimationCheat();
        GameActions.PlayScannerCheat();

        PlayerActions.EjectPlayerPPM();
        PlayerActions.SpectatePPM();
        PlayerActions.KillPlayerPPM();
        PlayerActions.TelekillPlayerPPM();
        PlayerActions.TeleportPlayerPPM();
        PlayerActions.SetFakeRolePPM();
        PlayerActions.SetFakeAlivePPM();
        // PlayerActions.ForceRolePPM();

        // This check ensures there is only one run per frame
        // so that OverloadHandler._timer progression remains accurate
        // if (__instance.AmOwner)
        // {
        //     OverloadHandler.Run();
        // }

        TracersHandler.DrawPlayerTracer(__instance);

        GameObject[] bodyObjects = GameObject.FindGameObjectsWithTag("DeadBody");
        foreach(GameObject bodyObject in bodyObjects) // Finds and loops through all dead bodies
        {
            DeadBody deadBody = bodyObject.GetComponent<DeadBody>();
            if (!deadBody) continue;

            TracersHandler.DrawBodyTracer(deadBody);

            if (CheatToggles.autoReportBodies)
            {
                if (deadBody.Reported) continue;

                deadBody.Reported = true;

                PlayerControl.LocalPlayer.CmdReportDeadBody(GameData.Instance.GetPlayerById(deadBody.ParentId));
            }
        }

        try
        {
            if (CheatToggles.invertControls)
            {
                PlayerControl.LocalPlayer.MyPhysics.Speed = -Mathf.Abs(PlayerControl.LocalPlayer.MyPhysics.Speed);
                PlayerControl.LocalPlayer.MyPhysics.GhostSpeed = -Mathf.Abs(PlayerControl.LocalPlayer.MyPhysics.GhostSpeed);
            }
            else
            {
                PlayerControl.LocalPlayer.MyPhysics.Speed = Mathf.Abs(PlayerControl.LocalPlayer.MyPhysics.Speed);
                PlayerControl.LocalPlayer.MyPhysics.GhostSpeed = Mathf.Abs(PlayerControl.LocalPlayer.MyPhysics.GhostSpeed);
            }
        } catch (NullReferenceException) { }
    }
}

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleAnimation))]
public static class PlayerPhysics_HandleAnimation
{
    // Prefix patch of PlayerPhysics.HandleAnimation to disable walking animation
    public static bool Prefix(PlayerPhysics __instance)
    {
        if (CheatToggles.moonWalk && __instance.AmOwner)
        {
            __instance.ResetAnimState();

            return false;
        }

        return true;
    }
}
