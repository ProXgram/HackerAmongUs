using UnityEngine;

namespace HAU;

public class TasksTab : ITab
{
    public string name => "Tasks";

    public void Draw()
    {
        MovementAutomation.Pause();
        UIHelpers.BeginSection("Automatic Tasks", "Completes your own assigned tasks. You must enable it manually each round.");
        CheatToggles.automaticTasks = GUILayout.Toggle(CheatToggles.automaticTasks, " Enable for this round");
        if (CheatToggles.automaticTasks) GUILayout.Label(AutomaticTasksHandler.StatusText);
        UIHelpers.Hint("Off at the start of each round. Saved profiles do not start task completion.");
        UIHelpers.EndSection();

        UIHelpers.BeginSection("Task Walker", "Walks to your task consoles and completes supported steps after reaching them.");
        bool running = MovementAutomation.Mode == MovementMode.TaskWalker;
        if (GUILayout.Button(running ? "Stop Task Walker" : "Start Task Walker"))
        {
            if (running) MovementAutomation.Stop();
            else MovementAutomation.Start(MovementMode.TaskWalker);
        }
        GUILayout.Label(MovementAutomation.Mode == MovementMode.TaskWalker ? TaskWalkerHandler.StatusText : "Task Walker: stopped");
        GUILayout.Label(MovementAutomation.StatusText);
        if (!string.IsNullOrEmpty(MovementAutomation.LastNavigationIssue))
            UIHelpers.Hint("Last route issue: " + MovementAutomation.LastNavigationIssue);
        UIHelpers.Hint("Close the menu and chat to walk. Unsupported timed steps stay manual. F8 stops movement. Starting Task Walker turns Automatic Tasks off.");
        UIHelpers.EndSection();

        UIHelpers.BeginSection("Task display");
        CheatToggles.taskArrows = GUILayout.Toggle(CheatToggles.taskArrows, " Point arrows toward my tasks");
        CheatToggles.showTasksMenu = GUILayout.Toggle(CheatToggles.showTasksMenu, " Open task list window");
        UIHelpers.Hint("The task list shows observed player tasks. Completion buttons apply only to your own tasks.");
        UIHelpers.EndSection();
    }
}
