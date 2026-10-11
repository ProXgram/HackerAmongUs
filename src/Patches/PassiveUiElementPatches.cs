using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace HAU;

[HarmonyPatch]
public static class PassiveUiElement_Patches
{
    [HarmonyTargetMethods]
    public static IEnumerable<MethodBase> TargetMethods()
    {
        var targets = new (Type Type, string Name)[]
        {
            (typeof(PassiveButton), nameof(PassiveButton.ReceiveClickDown)),
            (typeof(PassiveButton), nameof(PassiveButton.ReceiveClickUp)),
            (typeof(PassiveButton), nameof(PassiveButton.ReceiveMouseOver)),
            (typeof(GameOptionButton), nameof(GameOptionButton.ReceiveClickDown)),
            (typeof(GameOptionButton), nameof(GameOptionButton.ReceiveClickUp)),
            (typeof(GameOptionButton), nameof(GameOptionButton.ReceiveMouseOver)),
            (typeof(SlideBar), nameof(SlideBar.ReceiveClickDrag)),
            (typeof(Scrollbar), nameof(Scrollbar.ReceiveClickDrag)),
            (typeof(Scroller), nameof(Scroller.UpdateScrollBars))
        };
        foreach (var target in targets)
        {
            var method = AccessTools.DeclaredMethod(target.Type, target.Name);
            if (method == null) throw new MissingMethodException(target.Type.FullName, target.Name);
            yield return method;
        }
    }

    public static bool ReportRegistration(string owner, Action<string> report)
    {
        var prefix = AccessTools.DeclaredMethod(typeof(PassiveUiElement_Patches), nameof(Prefix));
        int registered = 0, expected = 0;
        foreach (var target in TargetMethods())
        {
            expected++;
            bool present = Harmony.GetPatchInfo(target)?.Prefixes.Any(patch =>
                patch.owner == owner && patch.PatchMethod == prefix) == true;
            if (present) registered++;
            report?.Invoke("HAU pointer patch " + target.DeclaringType.Name + "." + target.Name +
                ": " + (present ? "registered" : "MISSING"));
        }
        report?.Invoke("HAU pointer patch receipt: " + registered + "/" + expected);
        return registered == expected && expected == 9;
    }

    // Prevent mod-window interactions from also activating native pointer handlers.
    [HarmonyPrefix]
    public static bool Prefix(MethodBase __originalMethod = null)
    {
        // Input.mousePosition has a bottom-left origin
        // Convert it to a top-left origin by flipping the Y coordinate
        Vector2 mousePosition = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

        bool allowed = AllowsNativePointer(mousePosition);
        PointerInputDiagnostics.TraceNative(__originalMethod?.DeclaringType.Name + "." +
            __originalMethod?.Name, mousePosition, allowed);
        return allowed;
    }

    // MatchInfoHudButton has its own MonoBehaviour input path; it must use the
    // same window/release policy even when no PassiveUiElement callback occurs.
    public static bool AllowsNativePointer(Vector2 mousePosition)
    {

        if (PointerInputGuard.BlocksNativePointer(mousePosition)) return false;

        // Ability-panel clicks must never also activate a native action below it.
        if (MultiRoleUI.BlocksNativeInput(mousePosition)) return false;
        if (OutfitPickerUI.IsOpen && OutfitPickerUI.windowRect.Contains(mousePosition)) return false;
        if (HAUPlugin.menuAllowClickThrough.Value) return true;

        // Rect.Contains() uses GUI coordinates (top-left origin)
        return !((MenuUI.isGUIActive && MenuUI.windowRect.Contains(mousePosition)) ||
                 (CheatToggles.showConsole && ConsoleUI.windowRect.Contains(mousePosition)) ||
                 (CheatToggles.showDoorsMenu && DoorsUI.windowRect.Contains(mousePosition)) ||
                 (CheatToggles.showOverload && OverloadUI.windowRect.Contains(mousePosition)) ||
                 (CheatToggles.showProtectMenu && ProtectUI.windowRect.Contains(mousePosition)) ||
                 (CheatToggles.showRolesMenu && RolesUI.windowRect.Contains(mousePosition)) ||
                 (CheatToggles.showTasksMenu && TasksUI.windowRect.Contains(mousePosition)));
    }
}
