using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HAU;
using HarmonyLib;
using UnityEngine;

int checks = 0;
void Check(bool condition, string message)
{
    checks++;
    if (!condition) throw new Exception(message);
}

string owner = "HAU.PointerPatchRegistration.Tests";
var harmony = new Harmony(owner);
var expected = new HashSet<string>
{
    "PassiveButton.ReceiveClickDown", "PassiveButton.ReceiveClickUp", "PassiveButton.ReceiveMouseOver",
    "GameOptionButton.ReceiveClickDown", "GameOptionButton.ReceiveClickUp", "GameOptionButton.ReceiveMouseOver",
    "SlideBar.ReceiveClickDrag", "Scrollbar.ReceiveClickDrag", "Scroller.UpdateScrollBars"
};
var targets = PassiveUiElement_Patches.TargetMethods().ToArray();
Check(targets.Length == 9 && targets.Distinct().Count() == 9, "Exactly nine distinct originals must be declared.");
Check(targets.Select(method => method.DeclaringType.Name + "." + method.Name).ToHashSet().SetEquals(expected),
    "The target inventory must contain every original pointer handler.");
Check(typeof(PassiveUiElement_Patches).GetMethod("Prefix").GetCustomAttributes(typeof(HarmonyPatch), true).Length == 0,
    "The prefix must not carry merged single-original patch descriptions.");
Console.WriteLine("Harmony assembly: " + typeof(Harmony).Assembly.GetName().Version);
Check(typeof(Harmony).Assembly.GetName().Version == new Version(2, 10, 2, 0), "Use the actual pinned HarmonyX 2.10.2 assembly.");

try
{
    // Real PatchAll resolves annotations and installs interceptors. Tests below
    // call originals directly, rather than calling Prefix as a fixture helper.
    harmony.PatchAll(typeof(PassiveUiElement_Patches).Assembly);
    foreach (var original in targets)
        Check(Harmony.GetPatchInfo(original)?.Prefixes.Any(patch => patch.owner == owner) == true,
            "Original is missing its registered prefix: " + original.DeclaringType.Name + "." + original.Name);
    var receipt = new List<string>();
    Check(PassiveUiElement_Patches.ReportRegistration(owner, receipt.Add), "Runtime receipt must confirm all nine originals.");
    Check(receipt.Last() == "HAU pointer patch receipt: 9/9", "The receipt must report nine successes.");
    var guideOriginal = typeof(MatchInfoGuide).GetMethod(nameof(MatchInfoGuide.Open));
    Check(Harmony.GetPatchInfo(guideOriginal)?.Prefixes.Any(patch => patch.owner == owner) == true,
        "The independent guide-open path must really be patched.");

    var button = new PassiveButton(); var option = new GameOptionButton();
    var slide = new SlideBar(); var scrollbar = new Scrollbar(); var scroller = new Scroller();
    void InvokeAll()
    {
        button.ReceiveClickDown(); button.ReceiveClickUp(); button.ReceiveMouseOver();
        option.ReceiveClickDown(); option.ReceiveClickUp(); option.ReceiveMouseOver();
        slide.ReceiveClickDrag(new Vector2(1019, 113));
        scrollbar.ReceiveClickDrag(new Vector2(1019, 113));
        scroller.UpdateScrollBars();
    }
    Input.mousePosition = new Vector3(1019, Screen.height - 113, 0);
    OutfitPickerUI.windowRect = new Rect(210, 72, 860, 720);
    MenuUI.windowRect = new Rect(291, 136, 700, 590);
    MenuUI.isGUIActive = true; OutfitPickerUI.IsOpen = true;
    HAUPlugin.menuAllowClickThrough.Value = false;
    InvokeAll();
    Check(button.Down == 0 && button.Up == 0 && button.Over == 0, "All passive-button calls must really be intercepted.");
    Check(option.Down == 0 && option.Up == 0 && option.Over == 0, "All option-button calls must really be intercepted.");
    Check(slide.Drag == 0 && scrollbar.Drag == 0 && scroller.Updates == 0, "All drag/scroll targets must really be intercepted.");
    var guide = new MatchInfoGuide();
    guide.NormalModeSettings.Add(1); guide.ControllerSelectable.Add(5);
    guide.Open();
    Check(guide.Opens == 0 && guide.EntryRebuilds == 0 && guide.ControllerSelectable.Count == 1,
        "Independent HUD guide-open cannot run or clear entries under the open picker.");

    OutfitPickerUI.IsOpen = false; PointerInputGuard.Block = true;
    InvokeAll();
    Check(button.Up == 0 && scroller.Updates == 0, "An owned close release must remain blocked after visibility changes.");
    guide.Open();
    Check(guide.Opens == 0 && guide.EntryRebuilds == 0,
        "The guide must also reject an owned close release after the picker disappears.");
    PointerInputGuard.Block = false;
    Input.mousePosition = new Vector3(1180, Screen.height - 50, 0);
    InvokeAll();
    Check(button.Down == 1 && button.Up == 1 && button.Over == 1, "Outside pointer calls must remain available.");
    Check(option.Down == 1 && option.Up == 1 && option.Over == 1, "Outside option input must remain available.");
    Check(slide.Drag == 1 && scrollbar.Drag == 1 && scroller.Updates == 1, "Outside drag/scroll must remain available.");
    guide.Open();
    Check(guide.Opens == 1 && guide.EntryRebuilds == 1 && guide.ControllerSelectable.Count == 0,
        "Outside guide-open remains available and rebuilds current entries.");
    MenuUI.isGUIActive = false;
    guide.Open();
    Check(guide.Opens == 2 && guide.EntryRebuilds == 2,
        "An ordinary guide activation with the mod windows closed remains available.");
    var firstOpen = new MatchInfoGuide();
    firstOpen.ControllerSelectable.Add(9);
    firstOpen.Open();
    Check(firstOpen.Opens == 1 && firstOpen.EntryRebuilds == 0 && firstOpen.ControllerSelectable.Count == 1,
        "First guide-open must preserve the native initial-entry setup.");
    var hideAndSeek = new MatchInfoGuide();
    hideAndSeek.HnSModeSettings.Add(1); hideAndSeek.ControllerSelectable.Add(7);
    hideAndSeek.Open();
    Check(hideAndSeek.Opens == 1 && hideAndSeek.EntryRebuilds == 1 && hideAndSeek.ControllerSelectable.Count == 0,
        "Hide-and-seek guide-open must retain the existing entry refresh behavior.");
    foreach (var line in receipt) Console.WriteLine(line);
}
finally
{
    harmony.UnpatchSelf();
}
foreach (var original in targets)
    Check(Harmony.GetPatchInfo(original)?.Prefixes.Any(patch => patch.owner == owner) != true, "Own fixture patches must be removed.");
Check(Harmony.GetPatchInfo(typeof(MatchInfoGuide).GetMethod(nameof(MatchInfoGuide.Open)))?.Prefixes.Any(patch => patch.owner == owner) != true,
    "The independent guide fixture patch must also be removed.");
Console.WriteLine(checks + " real-Harmony registration and interception checks passed.");
Console.WriteLine("Nine managed originals were actually patched by HarmonyX 2.10.2. Native IL2CPP validation remains separate.");
