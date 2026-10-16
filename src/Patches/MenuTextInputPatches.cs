using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace HAU;

[HarmonyPatch]
public static class MenuTextInputPatches
{
    private static int _lastSearchInputFrame = -1;

    public static void CaptureSearchInputFrame() => _lastSearchInputFrame = Time.frameCount;

    [HarmonyTargetMethods]
    public static IEnumerable<MethodBase> TargetMethods()
    {
        var targets = new[]
        {
            (typeof(KeyboardJoystick), nameof(KeyboardJoystick.Update)),
            (typeof(ConsoleJoystick), nameof(ConsoleJoystick.Update))
        };
        foreach (var target in targets)
            yield return AccessTools.DeclaredMethod(target.Item1, target.Item2)
                ?? throw new MissingMethodException(target.Item1.FullName, target.Item2);
    }

    [HarmonyPrefix]
    public static bool Prefix() => !Application.isFocused || HAUPlugin.isPanicked ||
        !(MenuUI.isGUIActive && MenuUI.IsSearchEditing || _lastSearchInputFrame == Time.frameCount);
}
