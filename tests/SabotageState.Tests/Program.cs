using HAU;
using UnityEngine.SceneManagement;
var tests = new List<(string Name, Action Run)>();
void Test(string name,Action action)=>tests.Add((name,action));
void Check(bool value,string message="Check failed"){if(!value)throw new Exception(message);}
ShipStatus Setup(byte map=0)
{
    SabotageTools.ResetSession(); Utils.Map=map; Utils.isHost=false; Utils.isFreePlay=false; Utils.isMeeting=false; Utils.isNormalGame=true;
    OwnedActionContext.Ready=true; SceneManager.Handle++;
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
Test("Guest crew and local presentation roles send one explicit request without changing their role",()=>
{
    foreach(var presentationIsImpostor in new[]{false,true})foreach(var dead in new[]{false,true})
    {
        var s=Setup();var local=PlayerControl.LocalPlayer;var role=local.Data.Role;
        role.IsImpostor=presentationIsImpostor;local.Data.IsDead=dead;
        var pointer=role.Pointer;SabotageTools.SyncForMenu();CheatToggles.reactorSab=true;SabotageTools.Process(s);
        Check(s.Requests.SequenceEqual(new[]{(SystemTypes.Reactor,(byte)128)}));
        Check(ReferenceEquals(local.Data.Role,role)&&role.Pointer==pointer&&role.IsImpostor==presentationIsImpostor&&local.Data.IsDead==dead,"A request must not alter role or life state.");
        for(var tick=0;tick<4;tick++){SabotageTools.SyncForMenu();SabotageTools.Process(s);}
        Check(s.Requests.Count==1&&!CheatToggles.reactorSab,"A denied/unconfirmed request must be consumed once and mirror actual state.");
        Check(SabotageTools.StatusText.Contains("request sent")&&SabotageTools.StatusText.Contains("host may reject"));
        Check(!SabotageTools.StatusText.Contains("accepted",StringComparison.OrdinalIgnoreCase)&&!SabotageTools.StatusText.Contains("success",StringComparison.OrdinalIgnoreCase));
    }
});
Test("Living crew may send explicit start and repair requests",()=>
{var s=Setup();PlayerControl.LocalPlayer.Data.Role.IsImpostor=false;SabotageTools.Process(s);CheatToggles.reactorSab=true;SabotageTools.Process(s);Active(s,true);SabotageTools.Process(s);CheatToggles.reactorSab=false;SabotageTools.Process(s);Check(s.Requests.SequenceEqual(new[]{(SystemTypes.Reactor,(byte)128),(SystemTypes.Reactor,(byte)16)}));Check(SabotageTools.StatusText.StartsWith("Repair request sent.")&&SabotageTools.StatusText.Contains("host may reject"));});
Test("Dead guests cannot issue repair requests",()=>
{var s=Setup();Active(s,true);PlayerControl.LocalPlayer.Data.IsDead=true;SabotageTools.Process(s);CheatToggles.reactorSab=false;SabotageTools.Process(s);Check(s.Requests.Count==0&&CheatToggles.reactorSab);Check(SabotageTools.StatusText.Contains("unavailable while dead"));});
Test("Host and Freeplay crew starts preserve the explicit native request path",()=>
{
    foreach(var practice in new[]{false,true})
    {
        var s=Setup();Utils.isFreePlay=practice;Utils.isHost=!practice;var role=PlayerControl.LocalPlayer.Data.Role;role.IsImpostor=false;
        SabotageTools.SyncForMenu();CheatToggles.reactorSab=true;SabotageTools.Process(s);SabotageTools.Process(s);
        Check(s.Requests.SequenceEqual(new[]{(SystemTypes.Reactor,(byte)128)}));Check(!role.IsImpostor);
        Check(SabotageTools.StatusText=="Sabotage request sent.","Local authority still reports sending, not acceptance.");
    }
});
Test("Inactive meeting disconnected missing and unowned contexts cannot send fresh requests",()=>
{
    foreach(var change in new Action[]{()=>OwnedActionContext.Ready=false,()=>Utils.isMeeting=true,()=>Utils.isNormalGame=false,()=>PlayerControl.LocalPlayer.AmOwner=false,()=>PlayerControl.LocalPlayer.Data.Disconnected=true,()=>AmongUsClient.Instance=null,()=>ShipStatus.Instance=null})
    {
        var s=Setup();SabotageTools.Process(s);change();SabotageTools.Process(s);CheatToggles.reactorSab=true;SabotageTools.Process(s);
        Check(s.Requests.Count==0&&!CheatToggles.reactorSab);Check(SabotageTools.StatusText.Contains("active Normal round"));
    }
});
Test("A native state acknowledgement during request delivery is mirrored without replay",()=>
{
    var s=Setup();var reactor=(ReactorSystemType)s.Systems[SystemTypes.Reactor];
    s.OnRequest=(system,amount)=>{if(system==SystemTypes.Reactor)reactor.IsActive=amount==128;};
    SabotageTools.SyncForMenu();CheatToggles.reactorSab=true;SabotageTools.Process(s);Check(CheatToggles.reactorSab&&reactor.IsActive);
    for(var tick=0;tick<4;tick++){SabotageTools.SyncForMenu();SabotageTools.Process(s);}Check(s.Requests.Count==1);
    CheatToggles.reactorSab=false;SabotageTools.Process(s);Check(!CheatToggles.reactorSab&&!reactor.IsActive);
    SabotageTools.Process(s);Check(s.Requests.SequenceEqual(new[]{(SystemTypes.Reactor,(byte)128),(SystemTypes.Reactor,(byte)16)}));
    Check(!SabotageTools.StatusText.Contains("accepted",StringComparison.OrdinalIgnoreCase),"Observed system state must not be presented as a host acceptance guarantee.");
});
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
Test("Button availability follows native systems on every supported map",()=>
{
    foreach(var map in new byte[]{0,1,2,4,5})
    {
        var s=Setup(map);
        Check(SabotageTools.IsAvailable(SabotageKind.Reactor));
        Check(SabotageTools.IsAvailable(SabotageKind.Communications));
        Check(SabotageTools.IsAvailable(SabotageKind.Oxygen)==(map is 0 or 1));
        Check(SabotageTools.IsAvailable(SabotageKind.Lights)==(map!=5));
        s.Systems.Clear();
        foreach(var kind in Enum.GetValues<SabotageKind>())Check(!SabotageTools.IsAvailable(kind));
    }
});
Test("Button status reads native state independently of a queued request",()=>
{
    var s=Setup();
    Check(SabotageTools.Request(SabotageKind.Reactor,true));
    Check(CheatToggles.reactorSab&&!SabotageTools.IsActive(SabotageKind.Reactor));
    Check(s.Requests.Count==0&&SabotageTools.StatusText=="Sabotage request queued.");
    Active(s,true);
    foreach(var kind in Enum.GetValues<SabotageKind>())Check(SabotageTools.IsActive(kind));
    Active(s,false);
    foreach(var kind in Enum.GetValues<SabotageKind>())Check(!SabotageTools.IsActive(kind));
});
Test("Activate and Repair buttons preserve the old native request payloads",()=>
{
    foreach(var map in new byte[]{0,1,2,4,5})foreach(var kind in Enum.GetValues<SabotageKind>())
    {
        var s=Setup(map);PlayerControl.LocalPlayer.Data.Role.IsImpostor=false;
        if(!SabotageTools.IsAvailable(kind))continue;
        var system=kind switch
        {
            SabotageKind.Reactor=>map==4?SystemTypes.HeliSabotage:map==2?SystemTypes.Laboratory:SystemTypes.Reactor,
            SabotageKind.Oxygen=>SystemTypes.LifeSupp,
            SabotageKind.Lights=>SystemTypes.Electrical,
            _=>SystemTypes.Comms
        };
        Check(SabotageTools.Request(kind,true));
        SabotageTools.SyncForMenu();SabotageTools.Process(s);SabotageTools.Process(s);
        Check(s.Requests.SequenceEqual(new[]{(system,kind==SabotageKind.Lights?(byte)132:(byte)128)}));
        Active(s,true);SabotageTools.SyncForMenu();
        Check(SabotageTools.Request(kind,false));
        SabotageTools.Process(s);SabotageTools.Process(s);
        var repairs=kind==SabotageKind.Lights
            ?Enumerable.Range(0,5).Select(index=>(system,(byte)index))
            :kind==SabotageKind.Reactor&&map==4||kind==SabotageKind.Communications&&map is 1 or 5
                ?new[]{(system,(byte)16),(system,(byte)17)}:new[]{(system,(byte)16)};
        Check(s.Requests.Skip(1).SequenceEqual(repairs));
        Check(!PlayerControl.LocalPlayer.Data.Role.IsImpostor);
    }
});
Test("Repeated button clicks cannot multiply queued requests",()=>
{
    var s=Setup();
    Check(SabotageTools.Request(SabotageKind.Reactor,true));
    Check(SabotageTools.Request(SabotageKind.Reactor,true));
    SabotageTools.Process(s);SabotageTools.Process(s);
    Check(s.Requests.SequenceEqual(new[]{(SystemTypes.Reactor,(byte)128)}));
});
Test("Redundant and unavailable button actions do not send requests",()=>
{
    var s=Setup(5);
    Check(!SabotageTools.Request(SabotageKind.Oxygen,true));
    Check(!SabotageTools.Request(SabotageKind.Lights,true));
    Check(!SabotageTools.Request((SabotageKind)99,true));
    Check(!SabotageTools.Request(SabotageKind.Reactor,false));
    Active(s,true);
    Check(!SabotageTools.Request(SabotageKind.Reactor,true));
    SabotageTools.Process(s);Check(s.Requests.Count==0);
    ShipStatus.Instance=null;
    foreach(var kind in Enum.GetValues<SabotageKind>())
        Check(!SabotageTools.IsAvailable(kind)&&!SabotageTools.IsActive(kind)&&!SabotageTools.Request(kind,true));
});
Test("Button actions retain meeting and dead guest restrictions",()=>
{
    var s=Setup();Active(s,true);PlayerControl.LocalPlayer.Data.IsDead=true;
    Check(!SabotageTools.Request(SabotageKind.Reactor,false));
    Check(SabotageTools.StatusText.Contains("unavailable while dead"));
    Active(s,false);Check(SabotageTools.Request(SabotageKind.Reactor,true));
    SabotageTools.Process(s);Check(s.Requests.Count==1);
    Utils.isMeeting=true;Check(!SabotageTools.Request(SabotageKind.Oxygen,true));
    SabotageTools.Process(s);Check(s.Requests.Count==1);
});
Test("Lights Repair button can clear a host unfixable-light edge",()=>
{
    var s=Setup();Utils.isHost=true;SabotageTools.Process(s);
    CheatToggles.unfixableLights=true;SabotageTools.Process(s);Active(s,true);
    Check(SabotageTools.Request(SabotageKind.Lights,false));SabotageTools.Process(s);
    Check(!CheatToggles.unfixableLights);
    Check(s.Requests.SequenceEqual(new[]{(SystemTypes.Electrical,(byte)69),(SystemTypes.Electrical,(byte)69)}));
});
var failures=0;foreach(var test in tests){try{test.Run();Console.WriteLine("PASS "+test.Name);}catch(Exception ex){failures++;Console.WriteLine("FAIL "+test.Name+": "+ex);}}
Console.WriteLine($"{tests.Count-failures}/{tests.Count} sabotage state checks passed. No game process was used.");return failures==0?0:1;
