using System;
using System.Linq;
using HAU;
using HarmonyLib;
using UnityEngine;

int checks = 0;
void Check(bool value, string message)
{
    checks++;
    if (!value) throw new Exception(message);
}

var targets = MenuTextInputPatches.TargetMethods().ToArray();
Check(targets.Length == 2 && targets.Distinct().Count() == 2, "Exactly the two local native input components must be targeted.");
Check(targets.Any(method => method.DeclaringType == typeof(KeyboardJoystick) && method.Name == "Update"), "Keyboard input must be covered.");
Check(targets.Any(method => method.DeclaringType == typeof(ConsoleJoystick) && method.Name == "Update"), "Controller input must be covered.");
Check(targets.All(method => method.DeclaringType != typeof(HudManager) && method.DeclaringType != typeof(NetworkReceiver)), "HUD ticks and network processing must remain untouched.");
var harmony = new Harmony("HAU.MenuTextInput.Tests");
var keyboard = new KeyboardJoystick();
var controller = new ConsoleJoystick();
var hud = new HudManager();
var network = new NetworkReceiver();
try
{
    harmony.PatchAll(typeof(MenuTextInputPatches).Assembly);
    foreach (var target in targets)
        Check(Harmony.GetPatchInfo(target)?.Prefixes.Any(patch => patch.owner == harmony.Id) == true,
            "The input guard must be registered on " + target.DeclaringType.Name + "." + target.Name);
    foreach (bool menu in new[] { false, true })
    foreach (bool typing in new[] { false, true })
    foreach (bool focused in new[] { false, true })
    foreach (bool panicked in new[] { false, true })
    {
        Time.frameCount++;
        MenuUI.isGUIActive = menu; MenuUI.IsSearchEditing = typing;
        Application.isFocused = focused; HAUPlugin.isPanicked = panicked;
        int beforeKeyboard = keyboard.Actions, beforeController = controller.Actions;
        keyboard.Update(); controller.Update(); hud.Update(); network.HandleRpc();
        bool blocked = menu && typing && focused && !panicked;
        Check(keyboard.Actions == beforeKeyboard + (blocked ? 0 : 1), "Keyboard guard must apply only to focused search typing.");
        Check(controller.Actions == beforeController + (blocked ? 0 : 1), "Controller guard must apply only to focused search typing.");
    }
    Check(hud.Updates == 16 && network.Received == 16, "Normal HUD updates and incoming network events must run during search typing.");
    HAUPlugin.isPanicked = false; Application.isFocused = true;
    MenuUI.isGUIActive = true; MenuUI.IsSearchEditing = true;
    Time.frameCount++; MenuTextInputPatches.CaptureSearchInputFrame();
    MenuUI.IsSearchEditing = false;
    int beforeSubmit = keyboard.Actions;
    keyboard.Update(); controller.Update();
    Check(keyboard.Actions == beforeSubmit, "The Enter submit frame must not reach native game chat or abilities after editing stops.");
    MenuUI.isGUIActive = false;
    keyboard.Update();
    Check(keyboard.Actions == beforeSubmit, "The Escape or close frame must retain owned keyboard input even after the menu closes.");
    Time.frameCount++;
    keyboard.Update();
    Check(keyboard.Actions == beforeSubmit + 1, "Normal native keyboard behavior must resume on the next frame.");
    MenuUI.isGUIActive = true;
    int beforeNormal = controller.Actions; controller.Update();
    Check(controller.Actions == beforeNormal + 1, "A visible menu without active search typing must retain normal controller behavior.");
    Check(Harmony.GetPatchInfo(typeof(HudManager).GetMethod("Update"))?.Prefixes.Any(patch => patch.owner == harmony.Id) != true,
        "The general HUD update must not receive this prefix.");
}
finally { harmony.UnpatchSelf(); }
foreach (var target in targets)
    Check(Harmony.GetPatchInfo(target)?.Prefixes.Any(patch => patch.owner == harmony.Id) != true, "Fixture patches must be removed.");
Console.WriteLine(checks + " native search-input registration and interception checks passed.");
Console.WriteLine("Actual HarmonyX patched managed fixtures. Native IL2CPP gameplay validation remains separate.");
