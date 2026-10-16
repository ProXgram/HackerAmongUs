using System.Runtime.CompilerServices;

namespace UnityEngine
{
    public static class Application { public static bool isFocused = true; }
    public static class Time { public static int frameCount; }
}

namespace HAU
{
    public static class HAUPlugin { public static bool isPanicked; }
    public static class MenuUI { public static bool isGUIActive; public static bool IsSearchEditing; }
}

public sealed class KeyboardJoystick
{
    public int Actions;
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public void Update() { Actions++; }
}

public sealed class ConsoleJoystick
{
    public int Actions;
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public void Update() { Actions++; }
}

public sealed class HudManager
{
    public int Updates;
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public void Update() { Updates++; }
}

public sealed class NetworkReceiver
{
    public int Received;
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public void HandleRpc() { Received++; }
}
