using System;
using System.Runtime.CompilerServices;

namespace UnityEngine
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
    }
    public struct Rect
    {
        private float x, y, width, height;
        public Rect(float x, float y, float width, float height)
        { this.x = x; this.y = y; this.width = width; this.height = height; }
        public bool Contains(Vector2 point) => point.x >= x && point.y >= y &&
            point.x < x + width && point.y < y + height;
    }
    public static class Screen { public static int height = 832; }
    public static class Input { public static Vector3 mousePosition; }
}

public class PassiveButton
{
    public int Down, Up, Over;
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public virtual void ReceiveClickDown() { Down++; }
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public virtual void ReceiveClickUp() { Up++; }
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public virtual void ReceiveMouseOver() { Over++; }
}
public class GameOptionButton
{
    public int Down, Up, Over;
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public virtual void ReceiveClickDown() { Down++; }
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public virtual void ReceiveClickUp() { Up++; }
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public virtual void ReceiveMouseOver() { Over++; }
}
public class SlideBar
{
    public int Drag;
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public virtual void ReceiveClickDrag(UnityEngine.Vector2 position) { Drag++; }
}
public class Scrollbar
{
    public int Drag;
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public virtual void ReceiveClickDrag(UnityEngine.Vector2 position) { Drag++; }
}
public class Scroller
{
    public int Updates;
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public virtual void UpdateScrollBars() { Updates++; }
}

public class MatchInfoGuide
{
    public System.Collections.Generic.List<int> NormalModeSettings = new System.Collections.Generic.List<int>();
    public System.Collections.Generic.List<int> HnSModeSettings = new System.Collections.Generic.List<int>();
    public System.Collections.Generic.List<int> ControllerSelectable = new System.Collections.Generic.List<int>();
    public int Opens, EntryRebuilds;
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public void Open() { Opens++; }
    public void CreatePlayerEntries() { EntryRebuilds++; }
}

namespace HAU
{
    public sealed class Flag { public bool Value; }
    public static class HAUPlugin { public static Flag menuAllowClickThrough = new Flag(); }
    // Registration tests exercise actual Harmony interception independently of
    // the separately linked pointer-release algorithm regression suite.
    public static class PointerInputGuard
    {
        public static bool Block;
        public static bool BlocksNativePointer(UnityEngine.Vector2 position) => Block;
    }
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
