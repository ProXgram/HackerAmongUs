using System;
using System.Diagnostics;
using UnityEngine;
#if HAU_POINTER_DIAGNOSTICS
using System.Collections.Generic;
#endif

namespace HAU;


public static class PointerInputDiagnostics
{
#if HAU_POINTER_DIAGNOSTICS
    private static readonly Queue<string> BeforeClose = new();
    private static bool _recordedClose;
    private static int _closeFrame = -1, _remaining = 40, _guideBeforeLogs;
    private static PassiveButtonManager _manager;

    private static string Point(Vector2 point) => $"({point.x:0.##},{point.y:0.##})";
    private static string Bounds(Rect rect) => $"({rect.x:0.##},{rect.y:0.##},{rect.width:0.##},{rect.height:0.##})";
    private static string State(Vector2 pointer)
    {
        var input = Input.mousePosition;
        return $"frame={Time.frameCount} pointer={Point(pointer)} input=({input.x:0.##},{input.y:0.##}) screen={Screen.width}x{Screen.height}" +
            $" down={Input.GetMouseButtonDown(0)} up={Input.GetMouseButtonUp(0)} held={Input.GetMouseButton(0)}" +
            $" clickThrough={HAUPlugin.menuAllowClickThrough.Value} menu={MenuUI.isGUIActive}:{Bounds(MenuUI.windowRect)}" +
            $" picker={OutfitPickerUI.IsOpen}:{Bounds(OutfitPickerUI.windowRect)}" + DispatcherState();
    }
    private static string DispatcherState()
    {
        try
        {
            if (!_manager) _manager = UnityEngine.Object.FindObjectOfType<PassiveButtonManager>();
            if (!_manager) return " dispatcher=absent";
            var live = _manager.GetTouch(false);
            var down = _manager.GetTouch(true);
            string state = $" dispatcher={_manager.currentState} nativeTouch={Point(live)} nativeDown={Point(down)}" +
                $" nativeOver={(_manager.currentOver ? _manager.currentOver.GetType().Name : "none")}";
            if (_manager.controller != null)
            {
                var touch = _manager.controller.GetTouch(0);
                if (touch != null)
                    state += $" touchScreen={Point(touch.ScreenPosition)} touchScreenDown={Point(touch.ScreenDownAt)}" +
                        $" touchIsDown={touch.IsDown} touchWasDown={touch.WasDown} touchStart={touch.TouchStart} touchEnd={touch.TouchEnd}";
            }
            return state;
        }
        catch (Exception error) { return " dispatcherError=" + error.GetType().Name; }
    }
    private static void Write(string message) => HAUPlugin.Log?.LogInfo("HAU POINTER PROBE " + message);
#endif

    [Conditional("HAU_POINTER_DIAGNOSTICS")]
    public static void TraceNative(string handler, Vector2 pointer, bool allowed)
    {
#if HAU_POINTER_DIAGNOSTICS
        bool click = handler?.Contains("Click") == true || handler == "MatchInfoGuide.Open";
        if (!_recordedClose)
        {
            if (!OutfitPickerUI.IsOpen || !click) return;
            if (BeforeClose.Count >= 20) BeforeClose.Dequeue();
            string entry = $"before handler={handler} allowed={allowed} {State(pointer)}";
            BeforeClose.Enqueue(entry);

            if (handler == "MatchInfoGuide.Open" && _guideBeforeLogs++ < 2) Write(entry);
            return;
        }
        if (_remaining <= 0 || Time.frameCount > _closeFrame + 3) return;
        _remaining--;
        Write($"after handler={handler} allowed={allowed} {State(pointer)}");
#endif
    }

    [Conditional("HAU_POINTER_DIAGNOSTICS")]
    public static void TraceClose(string source, Rect? window)
    {
#if HAU_POINTER_DIAGNOSTICS
        if (_recordedClose) return;
        _recordedClose = true;
        _closeFrame = Time.frameCount;
        while (BeforeClose.Count > 0) Write(BeforeClose.Dequeue());
        var current = Event.current;
        var input = Input.mousePosition;
        var pointer = new Vector2(input.x, Screen.height - input.y);
        Write($"close={source} type={current?.type} raw={current?.rawType} button={current?.button}" +
            $" eventMouse={(current == null ? "none" : Point(current.mousePosition))}" +
            $" closingRect={(window.HasValue ? Bounds(window.Value) : "none")} {State(pointer)}");
#endif
    }
}
