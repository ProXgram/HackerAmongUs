using HarmonyLib;
using UnityEngine;

namespace HAU;

[HarmonyPatch(typeof(MatchInfoGuide), nameof(MatchInfoGuide.Open))]
public static class MatchInfoGuide_Open
{


    public static bool Prefix(MatchInfoGuide __instance)
    {
        Vector2 pointer = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        bool allowed = PassiveUiElement_Patches.AllowsNativePointer(pointer);
        PointerInputDiagnostics.TraceNative("MatchInfoGuide.Open", pointer, allowed);
        if (!allowed) return false;


        if (__instance.NormalModeSettings.Count > 0 || __instance.HnSModeSettings.Count > 0)
        {
            __instance.ControllerSelectable.Clear();
            __instance.CreatePlayerEntries();
        }
        return true;
    }
}
