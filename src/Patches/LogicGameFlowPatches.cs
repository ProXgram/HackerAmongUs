using HarmonyLib;

namespace HAU;

[HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlowNormal.CheckEndCriteria))]
public static class LogicGameFlowNormal_CheckEndCriteria
{

    public static bool Prefix()
    {
        return !CheatToggles.noGameEnd || (!Utils.isHost && !Utils.isFreePlay);
    }
}

[HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlowNormal.IsGameOverDueToDeath))]
public static class LogicGameFlowNormal_IsGameOverDueToDeath
{


    public static void Postfix(ref bool __result)
    {
        if (CheatToggles.noGameEnd && (Utils.isHost || Utils.isFreePlay))
        {
            __result = false;
        }

    }
}

[HarmonyPatch(typeof(LogicGameFlowHnS), nameof(LogicGameFlowHnS.CheckEndCriteria))]
public static class LogicGameFlowHnS_CheckEndCriteria
{

    public static bool Prefix()
    {
        return !CheatToggles.noGameEnd || (!Utils.isHost && !Utils.isFreePlay);
    }
}

[HarmonyPatch(typeof(LogicGameFlowHnS), nameof(LogicGameFlowHnS.IsGameOverDueToDeath))]
public static class LogicGameFlowHnS_IsGameOverDueToDeath
{


    public static void Postfix(ref bool __result)
    {
        if (CheatToggles.noGameEnd && (Utils.isHost || Utils.isFreePlay))
        {
            __result = false;
        }

    }
}
