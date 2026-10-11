using UnityEngine;

namespace HAU;

public static class PointerInputGuard
{
    private static readonly NativePointerReleaseGuard Releases = new();

    public static void ConsumeGuiClose(string source = null, Rect? window = null)
    {
        var current = Event.current;
        PointerInputDiagnostics.TraceClose(source ?? "mod window", window);
        if (current == null || current.button != 0) return;



        bool mouseRelease = current.rawType == EventType.MouseUp ||
            (current.type == EventType.Used && current.rawType == EventType.Used && Input.GetMouseButtonUp(0));
        if (!mouseRelease) return;
        var pointer = Input.mousePosition;
        Releases.ConsumeCloseRelease(Time.frameCount, pointer.x, Screen.height - pointer.y);
    }

    public static bool BlocksNativePointer(Vector2 topLeftPosition) =>
        Releases.Blocks(Time.frameCount, topLeftPosition.x, topLeftPosition.y);
}
