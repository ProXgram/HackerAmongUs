using UnityEngine;

namespace HAU;

public class ConsoleTab : ITab
{
    public string name => "Logs";

    public void Draw()
    {
        UIHelpers.BeginSection("Event log", "Records observed events while their switches are enabled. Recent entries stay in this session.");
        CheatToggles.showConsole = GUILayout.Toggle(CheatToggles.showConsole, " Open event log window");
        GUILayout.Label("Saved entries this session: " + ConsoleUI.EntryCount + " / 300 maximum");
        if (GUILayout.Button("Clear current log")) ConsoleUI.Clear();
        UIHelpers.EndSection();
        UIHelpers.BeginSection("Events to record");
        CheatToggles.logDeaths = GUILayout.Toggle(CheatToggles.logDeaths, " Kills and protection saves");
        CheatToggles.logShapeshifts = GUILayout.Toggle(CheatToggles.logShapeshifts, " Shapeshifts and reversions");
        CheatToggles.logVents = GUILayout.Toggle(CheatToggles.logVents, " Vent entry and exit");
        CheatToggles.logTasks = GUILayout.Toggle(CheatToggles.logTasks, " Task completion");
        CheatToggles.logGameState = GUILayout.Toggle(CheatToggles.logGameState, " Round and meeting state");
        UIHelpers.Hint("Enable the events you want before they happen. The log cannot recover earlier events.");
        UIHelpers.EndSection();
    }
}
