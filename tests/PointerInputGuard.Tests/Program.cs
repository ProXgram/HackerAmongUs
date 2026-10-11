using HAU;
using UnityEngine;

int passed = 0, failed = 0, assertions = 0;
void Check(bool condition, string message)
{
    assertions++;
    if (!condition) throw new Exception(message);
}
void Test(string name, Action run)
{
    Reset();
    try { run(); passed++; Console.WriteLine("PASS " + name); }
    catch (Exception error) { failed++; Console.Error.WriteLine("FAIL " + name + ": " + error.Message); }
}
void Reset()
{
    Time.frameCount += 20;
    Event.current = null;
    Input.LeftMouseUp = false;
    HAUPlugin.menuAllowClickThrough.Value = false;
    MenuUI.isGUIActive = true;
    MenuUI.windowRect = new Rect(291, 136, 700, 590);
    OutfitPickerUI.IsOpen = true;
    OutfitPickerUI.windowRect = new Rect(210, 72, 860, 720);
    MultiRoleUI.Block = false;
    CheatToggles.showConsole = CheatToggles.showDoorsMenu = CheatToggles.showOverload =
        CheatToggles.showProtectMenu = CheatToggles.showRolesMenu = CheatToggles.showTasksMenu = false;
    Pointer(1019, 113);

    PointerInputGuard.BlocksNativePointer(new Vector2(1019, 113));
}
void Pointer(float x, float topY) => Input.mousePosition = new Vector3(x, Screen.height - topY, 0);
void GuiMouseClose()
{
    Event.current = new Event { type = EventType.Used, rawType = EventType.MouseUp, button = 0 };
    PointerInputGuard.ConsumeGuiClose();
    OutfitPickerUI.IsOpen = false;
}
void NativeRelease(PassiveButton button)
{
    if (PassiveUiElement_Patches.Prefix()) button.ReceiveClickUp();
}

Test("Regression setup: hidden picker leaves native element outside main bounds exposed", () =>
{
    var button = new PassiveButton();
    OutfitPickerUI.IsOpen = false;
    NativeRelease(button);
    Check(button.Clicks == 1, "The original outside-main-window leak must be reproduced by the fixture.");
});
Test("Close mouse-up is consumed after picker IsOpen becomes false", () =>
{
    var button = new PassiveButton();
    GuiMouseClose();
    NativeRelease(button);
    Check(button.Clicks == 0, "The underlying native task-guide button must not activate.");
    Check(MenuUI.isGUIActive, "Closing the picker need not close the main menu.");
});
Test("Native AU19 Used raw event plus observed left release is consumed", () =>
{
    var button = new PassiveButton();
    Event.current = new Event { type = EventType.Used, rawType = EventType.Used, button = 0 };
    Input.LeftMouseUp = true;
    PointerInputGuard.ConsumeGuiClose();
    OutfitPickerUI.IsOpen = false;
    NativeRelease(button);
    Check(button.Clicks == 0, "The observed native type=Used/rawType=Used close release must not leak.");
});
Test("A Used event without observed left release does not claim pointer input", () =>
{
    var button = new PassiveButton();
    Event.current = new Event { type = EventType.Used, rawType = EventType.Used, button = 0 };
    PointerInputGuard.ConsumeGuiClose();
    OutfitPickerUI.IsOpen = false; MenuUI.isGUIActive = false;
    NativeRelease(button);
    Check(button.Clicks == 1, "Consumed keyboard/programmatic activation alone must not create a pointer guard.");
});
Test("A consumed right-button event does not claim a coinciding left release", () =>
{
    Event.current = new Event { type = EventType.Used, rawType = EventType.Used, button = 1 };
    Input.LeftMouseUp = true;
    PointerInputGuard.ConsumeGuiClose();
    OutfitPickerUI.IsOpen = false;
    Check(PassiveUiElement_Patches.Prefix(), "A right-button close must not claim the left-button release.");
});
Test("Same-frame native callbacks cannot reuse the GUI release", () =>
{
    var button = new PassiveButton();
    GuiMouseClose();
    for (int callback = 0; callback < 12; callback++) NativeRelease(button);
    Check(button.Clicks == 0, "Repeated passive handlers must all consume the same release.");
});
Test("One following release frame is protected", () =>
{
    var button = new PassiveButton();
    GuiMouseClose();
    Time.frameCount++;
    NativeRelease(button);
    Check(button.Clicks == 0, "A deferred native mouse-up must not activate the old close position.");
});
Test("Suppression expires without leaving native UI frozen", () =>
{
    var button = new PassiveButton();
    GuiMouseClose();
    Time.frameCount += 2;
    NativeRelease(button);
    Check(button.Clicks == 1, "A later intentional click at that position must work normally.");
});
Test("A different outside-window pointer remains interactive immediately", () =>
{
    var button = new PassiveButton();
    GuiMouseClose();
    Pointer(1120, 100);
    NativeRelease(button);
    Check(button.Clicks == 1, "Suppression must not freeze outside-window UI.");
});
Test("Mouse movement tolerance covers tiny release jitter only", () =>
{
    var button = new PassiveButton();
    GuiMouseClose();
    Pointer(1020, 114); NativeRelease(button);
    Check(button.Clicks == 0, "Subpixel/small-pixel pointer jitter is part of the consumed release.");
    Pointer(1025, 119); NativeRelease(button);
    Check(button.Clicks == 1, "Another pointer location must not be globally blocked.");
});
Test("Closing the main menu by GUI click consumes its release too", () =>
{
    var button = new PassiveButton();
    OutfitPickerUI.IsOpen = false;
    Pointer(370, 686);
    Event.current = new Event { type = EventType.Used, rawType = EventType.MouseUp, button = 0 };
    PointerInputGuard.ConsumeGuiClose();
    MenuUI.isGUIActive = false;
    NativeRelease(button);
    Check(button.Clicks == 0, "Hiding both windows must not leak the close click.");
});
foreach (var keyboardEvent in new[] { EventType.KeyDown, EventType.KeyUp })
    Test("Keyboard close creates no mouse suppression / " + keyboardEvent, () =>
    {
        var button = new PassiveButton();
        Event.current = new Event { type = EventType.Used, rawType = keyboardEvent, button = 0 };
        Input.LeftMouseUp = true;
        PointerInputGuard.ConsumeGuiClose();
        OutfitPickerUI.IsOpen = false; MenuUI.isGUIActive = false;
        NativeRelease(button);
        Check(button.Clicks == 1, "Delete/Escape or keyboard button activation must not create a pointer guard.");
    });
Test("No GUI event creates no pointer suppression", () =>
{
    var button = new PassiveButton();
    Input.LeftMouseUp = true;
    PointerInputGuard.ConsumeGuiClose();
    OutfitPickerUI.IsOpen = false; MenuUI.isGUIActive = false;
    NativeRelease(button);
    Check(button.Clicks == 1, "Programmatic/keyboard closes outside OnGUI must preserve native pointer input.");
});
Test("Repaint cannot create pointer suppression", () =>
{
    Event.current = new Event { type = EventType.Repaint, rawType = EventType.Repaint };
    PointerInputGuard.ConsumeGuiClose();
    OutfitPickerUI.IsOpen = false;
    Check(PassiveUiElement_Patches.Prefix(), "Rendering the menu must not consume arbitrary input.");
});
Test("A right-button event does not consume a left close release", () =>
{
    Event.current = new Event { type = EventType.Used, rawType = EventType.MouseUp, button = 1 };
    PointerInputGuard.ConsumeGuiClose();
    OutfitPickerUI.IsOpen = false;
    Check(PassiveUiElement_Patches.Prefix(), "A different mouse button must not be claimed as a GUI close.");
});
Test("Consumed close stays consumed even when normal click-through is enabled", () =>
{
    var button = new PassiveButton();
    HAUPlugin.menuAllowClickThrough.Value = true;
    GuiMouseClose();
    NativeRelease(button);
    Check(button.Clicks == 0, "The click explicitly used by Close must not trigger two actions.");
});
Test("Open picker still blocks its interior with click-through enabled", () =>
{
    HAUPlugin.menuAllowClickThrough.Value = true;
    Check(!PassiveUiElement_Patches.Prefix(), "Existing picker containment protection must remain intact.");
});
Test("Click outside open main and picker stays allowed", () =>
{
    Pointer(1180, 50);
    Check(PassiveUiElement_Patches.Prefix(), "Open windows must not blanket-freeze game UI.");
});
Test("Open main menu follows the existing click-through preference", () =>
{
    OutfitPickerUI.IsOpen = false;
    Pointer(700, 500);
    Check(!PassiveUiElement_Patches.Prefix(), "Main menu interior is blocked when preference is off.");
    HAUPlugin.menuAllowClickThrough.Value = true;
    Check(PassiveUiElement_Patches.Prefix(), "Preference-on behavior must not be broadened by the fix.");
});
Test("Clock rollback clears stale suppression", () =>
{
    var guard = new NativePointerReleaseGuard();
    guard.ConsumeCloseRelease(100, 12, 20);
    Check(!guard.Blocks(99, 12, 20), "A new/reset frame clock must not inherit stale input.");
    Check(!guard.Blocks(100, 12, 20), "An invalidated old release must not reactivate.");
});
Test("Frame arithmetic cannot overflow into indefinite suppression", () =>
{
    var guard = new NativePointerReleaseGuard();
    guard.ConsumeCloseRelease(int.MaxValue, 12, 20);
    Check(!guard.Blocks(int.MinValue, 12, 20), "A wrapped frame count must clear old input.");
});
foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
    Test("Nonfinite pointer never creates or broadens a guard / " + invalid, () =>
    {
        var guard = new NativePointerReleaseGuard();
        guard.ConsumeCloseRelease(100, invalid, 20);
        Check(!guard.Blocks(100, 12, 20), "Invalid recorded coordinates must not freeze valid input.");
        guard.ConsumeCloseRelease(100, 12, 20);
        Check(!guard.Blocks(100, invalid, 20), "Invalid queried coordinates must not freeze input.");
    });

Console.WriteLine(passed + "/" + (passed + failed) + " pointer scenarios passed; " + assertions + " assertions.");
Console.WriteLine("Linked real pointer prefix and release guard, with offline GUI/input state contracts; native test remains required.");
return failed == 0 ? 0 : 1;
