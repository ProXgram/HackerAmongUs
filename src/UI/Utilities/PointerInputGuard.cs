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
        // AU19's native GUILayout.Button consumes both type and rawType. Claim
        // that Used event only when the left release is independently observed;
        // keyboard/programmatic activation must not create a pointer guard.
        bool mouseRelease = current.rawType == EventType.MouseUp ||
            (current.type == EventType.Used && current.rawType == EventType.Used && Input.GetMouseButtonUp(0));
        if (!mouseRelease) return;
        var pointer = Input.mousePosition;
        Releases.ConsumeCloseRelease(Time.frameCount, pointer.x, Screen.height - pointer.y);
    }

    public static bool BlocksNativePointer(Vector2 topLeftPosition) =>
        Releases.Blocks(Time.frameCount, topLeftPosition.x, topLeftPosition.y);
}
