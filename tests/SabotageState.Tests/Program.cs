using HAU;
using UnityEngine.SceneManagement;
var tests = new List<(string Name, Action Run)>();
void Test(string name,Action action)=>tests.Add((name,action));
void Check(bool value,string message="Check failed"){if(!value)throw new Exception(message);}
ShipStatus Setup(byte map=0)
{
    SabotageTools.ResetSession(); Utils.Map=map; Utils.isHost=false; Utils.isFreePlay=false; Utils.isMeeting=false; Utils.isNormalGame=true;
    OwnedActionContext.Ready=true; LocalImpostorHandler.Blocks=false; SceneManager.Handle++;
    AmongUsClient.Instance=new(); PlayerControl.LocalPlayer=new(); PlayerControl.LocalPlayer.Data.Role.IsImpostor=true;
    var ship=new ShipStatus(); ShipStatus.Instance=ship;
    ship.Systems[map==2?SystemTypes.Laboratory:SystemTypes.Reactor]=new ReactorSystemType();
    if(map==4)ship.Systems[SystemTypes.HeliSabotage]=new HeliSabotageSystem();
    if(map is not(2 or 4 or 5))ship.Systems[SystemTypes.LifeSupp]=new LifeSuppSystemType();
    ship.Systems[SystemTypes.Comms]=map is 1 or 5?new HqHudSystemType():new HudOverrideSystemType();
    if(map!=5)ship.Systems[SystemTypes.Electrical]=new SwitchSystem();
    CheatToggles.mushSab=CheatToggles.mushSpore=CheatToggles.closeAllDoors=CheatToggles.openAllDoors=CheatToggles.spamCloseAllDoors=CheatToggles.spamOpenAllDoors=false;
    return ship;
}
void Active(ShipStatus ship,bool value)
{
    foreach(var system in ship.Systems.Values)
    {
        if(system is ReactorSystemType r)r.IsActive=value;
        if(system is HeliSabotageSystem h)h.IsActive=value;
        if(system is LifeSuppSystemType o)o.IsActive=value;
        if(system is HqHudSystemType q)q.IsActive=value;
        if(system is HudOverrideSystemType c)c.IsActive=value;
        if(system is SwitchSystem e){e.IsActive=value;e.ActualSwitches=0;e.ExpectedSwitches=(byte)(value?31:0);}
    }
}
Test("Initial sync displays active systems without repairing or re-sabotaging",()=>
{var s=Setup();Active(s,true);SabotageTools.Process(s);Check(s.Requests.Count==0);Check(CheatToggles.reactorSab&&CheatToggles.oxygenSab&&CheatToggles.commsSab&&CheatToggles.elecSab);});
Test("Stale true flags on a new ship are discarded without requests",()=>
{var s=Setup();SabotageTools.Process(s);CheatToggles.reactorSab=CheatToggles.oxygenSab=CheatToggles.commsSab=CheatToggles.elecSab=true;var n=new ShipStatus{Systems=s.Systems};ShipStatus.Instance=n;SabotageTools.Process(n);Check(n.Requests.Count==0&&!CheatToggles.reactorSab&&!CheatToggles.oxygenSab&&!CheatToggles.commsSab&&!CheatToggles.elecSab);});
foreach(var change in new Action[]{()=>Utils.isHost=true,()=>AmongUsClient.Instance=new(),()=>PlayerControl.LocalPlayer=new(),()=>PlayerControl.LocalPlayer.Data.Role=new(){IsImpostor=true},()=>SceneManager.Handle++,()=>OwnedActionContext.Ready=false,()=>Utils.isMeeting=true,()=>Utils.isNormalGame=false})
Test("A changed round/context cannot replay old sabotage input "+tests.Count,()=>
{var s=Setup();SabotageTools.Process(s);CheatToggles.reactorSab=true;change();SabotageTools.Process(s);Check(s.Requests.Count==0&&!CheatToggles.reactorSab);});
Test("Remote active and repaired states only update displayed switches",()=>
{var s=Setup();SabotageTools.Process(s);Active(s,true);SabotageTools.Process(s);Check(s.Requests.Count==0&&CheatToggles.reactorSab);Active(s,false);SabotageTools.Process(s);Check(s.Requests.Count==0&&!CheatToggles.reactorSab);});
Test("Remote changes override a stale pending flag",()=>
{var s=Setup();SabotageTools.Process(s);CheatToggles.reactorSab=true;Active(s,true);SabotageTools.Process(s);Check(s.Requests.Count==0&&CheatToggles.reactorSab);});
Test("Repeated menu sync retains explicit input until the tick consumes it once",()=>
{var s=Setup();SabotageTools.SyncForMenu();CheatToggles.reactorSab=true;SabotageTools.SyncForMenu();SabotageTools.SyncForMenu();SabotageTools.Process(s);Check(s.Requests.SequenceEqual(new[]{(SystemTypes.Reactor,(byte)128)}));SabotageTools.Process(s);Check(s.Requests.Count==1);});
Test("An explicit repair keeps native reactor amount16",()=>
{var s=Setup();Active(s,true);SabotageTools.SyncForMenu();CheatToggles.reactorSab=false;SabotageTools.Process(s);Check(s.Requests.SequenceEqual(new[]{(SystemTypes.Reactor,(byte)16)}));});
Test("Genuine guest Impostor ghosts may request sabotage",()=>
{var s=Setup();PlayerControl.LocalPlayer.Data.IsDead=true;SabotageTools.Process(s);CheatToggles.reactorSab=true;SabotageTools.Process(s);Check(s.Requests.SequenceEqual(new[]{(SystemTypes.Reactor,(byte)128)}));});
Test("Local-only Impostor previews cannot request sabotage, even after death",()=>
{foreach(var dead in new[]{false,true}){var s=Setup();LocalImpostorHandler.Blocks=true;PlayerControl.LocalPlayer.Data.IsDead=dead;SabotageTools.Process(s);CheatToggles.reactorSab=true;SabotageTools.Process(s);Check(s.Requests.Count==0&&!CheatToggles.reactorSab);}});
Test("Crew cannot start sabotage but may send a normal living repair request",()=>
{var s=Setup();PlayerControl.LocalPlayer.Data.Role.IsImpostor=false;SabotageTools.Process(s);CheatToggles.reactorSab=true;SabotageTools.Process(s);Check(s.Requests.Count==0);Active(s,true);SabotageTools.Process(s);CheatToggles.reactorSab=false;SabotageTools.Process(s);Check(s.Requests.SequenceEqual(new[]{(SystemTypes.Reactor,(byte)16)}));});
Test("Dead guests cannot issue repair requests",()=>
{var s=Setup();Active(s,true);PlayerControl.LocalPlayer.Data.IsDead=true;SabotageTools.Process(s);CheatToggles.reactorSab=false;SabotageTools.Process(s);Check(s.Requests.Count==0&&CheatToggles.reactorSab);});
Test("Host authority changes discard old unfixable-light edges",()=>
{var s=Setup();Utils.isHost=true;SabotageTools.Process(s);CheatToggles.unfixableLights=true;Utils.isHost=false;SabotageTools.Process(s);Check(s.Requests.Count==0&&!CheatToggles.unfixableLights);Utils.isHost=true;SabotageTools.Process(s);Check(s.Requests.Count==0);});
Test("Explicit host unfixable-light edges retain amount69 without an extra light action",()=>
{var s=Setup();Utils.isHost=true;SabotageTools.Process(s);CheatToggles.unfixableLights=true;SabotageTools.Process(s);Check(s.Requests.SequenceEqual(new[]{(SystemTypes.Electrical,(byte)69)}));CheatToggles.unfixableLights=false;SabotageTools.Process(s);Check(s.Requests.SequenceEqual(new[]{(SystemTypes.Electrical,(byte)69),(SystemTypes.Electrical,(byte)69)}));});
Test("Missing, wrong and replaced systems are safely initialized, never replayed",()=>
{var s=Setup();s.Systems.Clear();SabotageTools.Process(s);Check(s.Requests.Count==0);s.Systems[SystemTypes.Reactor]=new SwitchSystem{ThrowOnCast=true};CheatToggles.reactorSab=true;SabotageTools.Process(s);Check(s.Requests.Count==0&&!CheatToggles.reactorSab);s.Systems[SystemTypes.Reactor]=new ReactorSystemType{IsActive=true};SabotageTools.Process(s);Check(s.Requests.Count==0&&CheatToggles.reactorSab);});
Test("Null ship and system table reset safely",()=>
{Setup();ShipStatus.Instance=null;SabotageTools.SyncForMenu();SabotageTools.Process(null);var s=Setup();s.Systems=null;SabotageTools.Process(s);Check(s.Requests.Count==0);});
Test("Polus reactor uses laboratory and Airship repair uses both native flags",()=>
{var p=Setup(2);SabotageTools.Process(p);CheatToggles.reactorSab=true;SabotageTools.Process(p);Check(p.Requests.SequenceEqual(new[]{(SystemTypes.Laboratory,(byte)128)}));var a=Setup(4);Active(a,true);SabotageTools.Process(a);CheatToggles.reactorSab=false;SabotageTools.Process(a);Check(a.Requests.SequenceEqual(new[]{(SystemTypes.HeliSabotage,(byte)16),(SystemTypes.HeliSabotage,(byte)17)}));});
Test("Mira HQ oxygen is supported and Mira/Fungle communications repair preserves both flags",()=>
{var m=Setup(1);SabotageTools.Process(m);CheatToggles.oxygenSab=true;SabotageTools.Process(m);Check(m.Requests.SequenceEqual(new[]{(SystemTypes.LifeSupp,(byte)128)}));foreach(var map in new byte[]{1,5}){var s=Setup(map);Active(s,true);SabotageTools.Process(s);CheatToggles.commsSab=false;SabotageTools.Process(s);Check(s.Requests.SequenceEqual(new[]{(SystemTypes.Comms,(byte)16),(SystemTypes.Comms,(byte)17)}));}});
Test("Lights use existing sabotage mask and repair only mismatched switch indices",()=>
{var s=Setup();SabotageTools.Process(s);CheatToggles.elecSab=true;SabotageTools.Process(s);Check(s.Requests.SequenceEqual(new[]{(SystemTypes.Electrical,(byte)132)}));var e=(SwitchSystem)s.Systems[SystemTypes.Electrical];e.IsActive=true;e.ActualSwitches=2;e.ExpectedSwitches=3;SabotageTools.Process(s);CheatToggles.elecSab=false;SabotageTools.Process(s);Check(s.Requests.Last()==(SystemTypes.Electrical,(byte)0));});
var failures=0;foreach(var test in tests){try{test.Run();Console.WriteLine("PASS "+test.Name);}catch(Exception ex){failures++;Console.WriteLine("FAIL "+test.Name+": "+ex);}}
Console.WriteLine($"{tests.Count-failures}/{tests.Count} sabotage state checks passed. No game process was used.");return failures==0?0:1;
