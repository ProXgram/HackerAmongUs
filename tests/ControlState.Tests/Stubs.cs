namespace AmongUs.GameOptions { public enum RoleTypes { Crewmate, Impostor } }
namespace UnityEngine
{
    public class MonoBehaviour { }
    public enum KeyCode { None, A, B, C, D, F1, F2, F3, F8, Delete }
    public static class Application { public static bool isFocused = true; }
    public static class Input
    {
        public static readonly HashSet<KeyCode> Pressed = new();
        public static bool GetKeyDown(KeyCode key) => Pressed.Contains(key);
    }
}
public class HudManager
{
    public static HudManager Current;
    public static bool InstanceExists => Current != null;
    public static HudManager Instance => Current ?? throw new InvalidOperationException("HUD must not be created.");
    public ChatController Chat = new();
}
public class ChatController
{
    public bool IsOpenOrOpening;
    public static implicit operator bool(ChatController value) => value != null;
}
namespace HAU
{
    public static class HAUPlugin { public static string ProfilePath; public static bool isPanicked; }
    public static class Utils { public static bool isHost, isFreePlay; }
    public static class ModBranding { public const string Name = "Hacker Among Us"; }
    public static class AutomaticTasksHandler { public static int Resets; public static void Reset() => Resets++; }
    public static class MovementAutomation { public static int Stops; public static void Stop() => Stops++; }
    public static class ColorCycleHandler { public static int Stops; public static void Stop() => Stops++; public static void Tick() { } }
    public static class SprintHandler { public static void Tick() { } public static void Reset() { } }
    public static class NicknameEntry { public static void Tick() { } public static void StopEditing() { } }
    public static class MenuUI { public static bool isGUIActive; }
}
