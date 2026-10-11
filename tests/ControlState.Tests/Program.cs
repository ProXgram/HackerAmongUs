using HAU;
using UnityEngine;

int checks = 0;
string workspace = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "HAU-ControlState-" + Guid.NewGuid().ToString("N")));
Directory.CreateDirectory(workspace);
HAUPlugin.ProfilePath = Path.Combine(workspace, "HAUProfile.txt");
try
{
    string[] requestedActions = { "teleportPlayer", "setFakeRole", "animShields", "animEmptyGarbage", "setFakeAlive", "completeMyTasks", "sabotageMap",
        "closeMeeting", "callMeeting", "reportBody",
        "mushSab", "mushSpore", "openAllDoors", "closeAllDoors", "kickVents", "forceRole", "skipMeeting",
        "forceStartGame", "ejectPlayer", "killPlayer", "telekillPlayer", "killAll", "killAllCrew", "killAllImps",
        "panicMode", "reloadConfig", "openConfig", "loadProfile", "saveProfile", "overloadAll", "overloadHost",
        "overloadCrew", "overloadImps", "overloadReset" };
    string[] nonRestorableState = { "automaticTasks", "spectate", "commsSab", "elecSab", "reactorSab", "oxygenSab", "unfixableLights" };
    foreach (string action in requestedActions)
        Check(ControlPolicy.IsMomentaryRequest(action), "audited action classification: " + action);
    foreach (string setting in new[] { "sprint", "noClip", "seeRoles", "mapCrew", "freecam", "multiRole", "ghostVoting", "animPet", "noGameEnd" })
        Check(!ControlPolicy.IsMomentaryRequest(setting), "audited persistent classification: " + setting);
    foreach (string state in nonRestorableState)
        Check(!ControlPolicy.HasRestorableState(state) && !ControlPolicy.IsMomentaryRequest(state), "live state keeps normal shortcut semantics: " + state);
    foreach (var entry in CheatToggles.ToggleFields) entry.Value.SetValue(null, true);
    CheatToggles.Keybinds[nameof(CheatToggles.completeMyTasks)] = KeyCode.F1;
    CheatToggles.Keybinds[nameof(CheatToggles.killAll)] = KeyCode.F2;
    CheatToggles.SaveTogglesToProfile();
    string saved = File.ReadAllText(HAUPlugin.ProfilePath);
    foreach (var entry in CheatToggles.ToggleFields)
    {
        if (!requestedActions.Contains(entry.Key) && !nonRestorableState.Contains(entry.Key)) continue;
        Check(saved.Split('\n').Any(line => line.StartsWith(entry.Key + " = False = ", StringComparison.Ordinal)),
            "saved request stays off: " + entry.Key);
        Check((bool)entry.Value.GetValue(null), "saving must not cancel an active request: " + entry.Key);
    }
    Check(saved.Contains("seeRoles = True = KeyCode.None"), "persistent ESP switch is saved");
    Check(saved.Contains("completeMyTasks = False = KeyCode.F1"), "request shortcut is retained");


    File.WriteAllLines(HAUPlugin.ProfilePath, CheatToggles.ToggleFields.Keys.Select(id => id + " = True = KeyCode.F3"));
    foreach (string state in nonRestorableState) CheatToggles.ToggleFields[state].SetValue(null, false);
    Utils.isHost = true;
    CheatToggles.LoadTogglesFromProfile();
    foreach (var entry in CheatToggles.ToggleFields)
    {
        bool expected = !requestedActions.Contains(entry.Key) && !nonRestorableState.Contains(entry.Key);
        Check((bool)entry.Value.GetValue(null) == expected, "host profile restore: " + entry.Key);
        Check(CheatToggles.Keybinds[entry.Key] == KeyCode.F3, "legacy shortcut restored: " + entry.Key);
    }
    Utils.isHost = false;
    CheatToggles.LoadTogglesFromProfile();
    foreach (var entry in CheatToggles.ToggleFields.Where(e => ControlPolicy.RequiresHostActivation(e.Key)))
        Check(!(bool)entry.Value.GetValue(null), "guest profile cannot activate host control: " + entry.Key);
    Check(CheatToggles.seeRoles, "guest keeps persistent ESP switch");
    Check(CheatToggles.ghostVoting, "guest retains local ghost-vote opt-in");
    Utils.isFreePlay = true;
    CheatToggles.LoadTogglesFromProfile();
    Check(CheatToggles.noKillCd, "practice can restore a host/practice setting");
    Check(!CheatToggles.killAll && !CheatToggles.automaticTasks, "practice still cannot replay actions/tasks");
    CheatToggles.reactorSab = CheatToggles.commsSab = true;
    File.WriteAllText(HAUPlugin.ProfilePath, "reactorSab = False = KeyCode.A\ncommsSab = False = KeyCode.B\n");
    CheatToggles.LoadTogglesFromProfile();
    Check(CheatToggles.reactorSab && CheatToggles.commsSab, "profile load does not request a repair by replacing live sabotage states");


    File.WriteAllText(HAUPlugin.ProfilePath, "# old partial profile\nseeRoles = True = KeyCode.A\nunknown = True = KeyCode.B\n");
    CheatToggles.completeMyTasks = CheatToggles.killAll = CheatToggles.panicMode = true;
    CheatToggles.LoadTogglesFromProfile();
    Check(!CheatToggles.completeMyTasks && !CheatToggles.killAll && !CheatToggles.panicMode, "partial profile clears stale requests");
    Check(CheatToggles.Keybinds[nameof(CheatToggles.seeRoles)] == KeyCode.A, "stable old field ID remains readable");
    HAUPlugin.ProfilePath = Path.Combine(workspace, "missing.txt");
    CheatToggles.completeMyTasks = CheatToggles.callMeeting = true;
    CheatToggles.LoadTogglesFromProfile();
    Check(!CheatToggles.completeMyTasks && !CheatToggles.callMeeting, "missing profile clears pending actions");

    var listener = new KeybindListener();
    ResetBindings();
    CheatToggles.Keybinds[nameof(CheatToggles.killAll)] = KeyCode.A;
    Input.Pressed.Add(KeyCode.A);
    Utils.isFreePlay = false;
    listener.Update();
    Check(!CheatToggles.killAll, "guest hotkey cannot submit host kill request");
    Utils.isHost = true;
    listener.Update();
    Check(CheatToggles.killAll, "host hotkey submits explicit request");
    listener.Update();
    Check(CheatToggles.killAll, "repeated pending action coalesces instead of toggling off");
    CheatToggles.killAll = false; Utils.isHost = false; Utils.isFreePlay = true;
    listener.Update();
    Check(CheatToggles.killAll, "practice hotkey submits explicit request");

    foreach (var blocked in new[] { "unfocused", "menu", "chat", "panic" })
    {
        ResetBindings();
        CheatToggles.Keybinds[nameof(CheatToggles.seeRoles)] = KeyCode.B;
        CheatToggles.Keybinds[nameof(CheatToggles.completeMyTasks)] = KeyCode.B;
        Input.Pressed.Add(KeyCode.B);
        Application.isFocused = blocked != "unfocused";
        MenuUI.isGUIActive = blocked == "menu";
        HAUPlugin.isPanicked = blocked == "panic";
        HudManager.Current = blocked == "chat" ? new HudManager { Chat = new ChatController { IsOpenOrOpening = true } } : null;
        listener.Update();
        Check(!CheatToggles.seeRoles && !CheatToggles.completeMyTasks, blocked + " blocks switches and requests");
    }
    ResetBindings();
    CheatToggles.Keybinds[nameof(CheatToggles.seeRoles)] = KeyCode.C;
    Input.Pressed.Add(KeyCode.C);
    listener.Update(); Check(CheatToggles.seeRoles, "focused guest can toggle ESP");
    listener.Update(); Check(!CheatToggles.seeRoles, "persistent switch still toggles off");
    foreach (string state in new[] { "spectate", "reactorSab", "commsSab" })
    {
        ResetBindings(); CheatToggles.Keybinds[state] = KeyCode.D; Input.Pressed.Add(KeyCode.D);
        listener.Update(); Check((bool)CheatToggles.ToggleFields[state].GetValue(null), "live state switches on: " + state);
        listener.Update(); Check(!(bool)CheatToggles.ToggleFields[state].GetValue(null), "live state switches off: " + state);
    }
    ResetBindings(); CheatToggles.Keybinds[nameof(CheatToggles.callMeeting)] = KeyCode.D; Input.Pressed.Add(KeyCode.D);
    listener.Update(); Check(CheatToggles.callMeeting, "guest may explicitly request a normal meeting");
    Input.Pressed.Clear(); Input.Pressed.Add(KeyCode.F8); MenuUI.isGUIActive = true;
    int priorStops = MovementAutomation.Stops;
    listener.Update(); Check(MovementAutomation.Stops == priorStops + 1, "F8 stop remains available with menu open");
    Console.WriteLine($"{checks} control/profile/keybind checks passed; actual source linked, no game or network mutation.");
}
finally
{
    if (!workspace.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase) ||
        !Path.GetFileName(workspace).StartsWith("HAU-ControlState-", StringComparison.Ordinal)) throw new InvalidOperationException("Unsafe test cleanup.");
    Directory.Delete(workspace, true);
}
void Check(bool condition, string message)
{ if (!condition) throw new Exception(message); checks++; }
void ResetBindings()
{
    CheatToggles.DisableAll();
    foreach (var key in CheatToggles.Keybinds.Keys.ToArray()) CheatToggles.Keybinds[key] = KeyCode.None;
    Input.Pressed.Clear(); Application.isFocused = true; MenuUI.isGUIActive = false;
    HAUPlugin.isPanicked = false; HudManager.Current = null; Utils.isHost = Utils.isFreePlay = false;
}
