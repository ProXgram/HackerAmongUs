namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class HarmonyPatch : Attribute
    {
        public HarmonyPatch() { }
        public HarmonyPatch(Type type, string method) { }
    }
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class HarmonyPrefix : Attribute { }
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class HarmonyTargetMethods : Attribute { }
    public static class AccessTools
    {
        public static System.Reflection.MethodInfo DeclaredMethod(Type type, string name) =>
            type.GetMethod(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
    }
    public sealed class Patch
    {
        public string owner;
        public System.Reflection.MethodInfo PatchMethod;
    }
    public sealed class Patches { public List<Patch> Prefixes = new(); }
    public static class Harmony { public static Patches GetPatchInfo(System.Reflection.MethodBase original) => null; }
}

namespace UnityEngine
{
    public readonly struct Vector2
    {
        public readonly float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
    }
    public readonly struct Vector3
    {
        public readonly float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
    }
    public readonly struct Rect
    {
        private readonly float x, y, width, height;
        public Rect(float x, float y, float width, float height)
        { this.x = x; this.y = y; this.width = width; this.height = height; }
        public bool Contains(Vector2 point) => point.x >= x && point.y >= y &&
            point.x < x + width && point.y < y + height;
    }
    public enum EventType { MouseDown, MouseUp, KeyDown, KeyUp, Used, Repaint }
    public sealed class Event
    {
        public static Event current;
        public EventType type, rawType;
        public int button;
    }
    public static class Time { public static int frameCount = 100; }
    public static class Screen { public static int height = 832; }
    public static class Input
    {
        public static Vector3 mousePosition;
        public static bool LeftMouseUp;
        public static bool GetMouseButtonUp(int button) => button == 0 && LeftMouseUp;
    }
}

public class PassiveButton
{
    public int Clicks;
    public void ReceiveClickDown() { }
    public void ReceiveClickUp() => Clicks++;
    public void ReceiveMouseOver() { }
}
public class GameOptionButton
{
    public void ReceiveClickDown() { }
    public void ReceiveClickUp() { }
    public void ReceiveMouseOver() { }
}
public class SlideBar { public void ReceiveClickDrag() { } }
public class Scrollbar { public void ReceiveClickDrag() { } }
public class Scroller { public void UpdateScrollBars() { } }

namespace HAU
{
    public sealed class Flag { public bool Value; }
    public static class HAUPlugin { public static Flag menuAllowClickThrough = new(); }
    public static class MultiRoleUI
    {
        public static bool Block;
        public static bool BlocksNativeInput(UnityEngine.Vector2 position) => Block;
    }
    public static class MenuUI { public static bool isGUIActive; public static UnityEngine.Rect windowRect; }
    public static class OutfitPickerUI { public static bool IsOpen; public static UnityEngine.Rect windowRect; }
    public static class ConsoleUI { public static UnityEngine.Rect windowRect; }
    public static class DoorsUI { public static UnityEngine.Rect windowRect; }
    public static class OverloadUI { public static UnityEngine.Rect windowRect; }
    public static class ProtectUI { public static UnityEngine.Rect windowRect; }
    public static class RolesUI { public static UnityEngine.Rect windowRect; }
    public static class TasksUI { public static UnityEngine.Rect windowRect; }
    public static class CheatToggles
    {
        public static bool showConsole, showDoorsMenu, showOverload, showProtectMenu, showRolesMenu, showTasksMenu;
    }
}
