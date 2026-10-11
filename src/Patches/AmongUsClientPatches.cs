using HarmonyLib;

namespace HAU;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.Update))]
public static class AmongUsClient_Update
{
    public static void Postfix()
    {
        PlayerDisplayOverrides.SpoofLevel();
        AlwaysImpostorHandler.TickFreeplay();
        LocalImpostorHandler.Tick();
        AutomaticTasksHandler.Tick();
        InfiniteJudgeHandler.Tick();













    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
public static class AmongUsClient_OnGameJoined
{

    public static string lastGameIdString = "";

    public static void Postfix(string gameIdString)
    {
        lastGameIdString = gameIdString;
        CheatToggles.sabotageMap = false;
        PlayerActions.ResetSession();
        GameActions.StopShipAnimCheats();
        ColorCycleHandler.Stop();
        MovementAutomation.Reset();
        SprintHandler.Reset();
        LocalImpostorHandler.Reset();
        AlwaysImpostorHandler.ResetFreeplayAttempt();
        CheatToggles.automaticTasks = false;
        AutomaticTasksHandler.Reset();
        InfiniteJudgeHandler.Reset();
#if GUEST_KILL_EXPERIMENT
        GuestRoundTest.Reset();
#endif
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CoStartGame))]
public static class AmongUsClient_CoStartGame
{
    public static void Postfix()
    {

        CheatToggles.sabotageMap = false;
        PlayerActions.ResetSession();
        GameActions.StopShipAnimCheats();
        CheatToggles.automaticTasks = false;
        AutomaticTasksHandler.Reset();
        if (CheatToggles.logGameState)
            ConsoleUI.Log("Game started");
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
public static class AmongUsClient_OnGameEnd
{
    public static void Postfix(EndGameResult endGameResult)
    {
        CheatToggles.sabotageMap = false;
        PlayerActions.ResetSession();
        GameActions.StopShipAnimCheats();
        ColorCycleHandler.Stop();
        MovementAutomation.Reset();
        SprintHandler.Reset();
        LocalImpostorHandler.Reset();
        AlwaysImpostorHandler.ResetFreeplayAttempt();
        CheatToggles.automaticTasks = false;
        AutomaticTasksHandler.Reset();
        InfiniteJudgeHandler.Reset();
#if GUEST_KILL_EXPERIMENT
        GuestRoundTest.Reset();
#endif
        if (CheatToggles.logGameState)
            ConsoleUI.Log($"Game ended with reason {endGameResult.GameOverReason}");
    }
}
