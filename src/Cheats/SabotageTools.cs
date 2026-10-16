using System;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine.SceneManagement;

namespace HAU;

public enum SabotageKind { Reactor, Oxygen, Lights, Communications }

public static class SabotageTools
{
    private sealed class LiveState
    {
        private IntPtr system;
        private bool initialized, active;
        internal bool Observe(IntPtr pointer, bool value, ref bool shown)
        {
            if (!initialized || system != pointer || active != value)
            {
                system = pointer; initialized = true; active = value; shown = value;
                return false;
            }
            return shown != active;
        }
        internal void Reset() { system = IntPtr.Zero; initialized = false; active = false; }
        internal void Missing(ref bool shown) { Reset(); shown = false; }
        internal void Mirror(IntPtr pointer, bool value, ref bool shown)
        { system = pointer; initialized = true; active = value; shown = value; }
    }

    private static readonly LiveState Reactor = new(), Oxygen = new(), Comms = new(), Electrical = new();
    private static IntPtr contextShip, contextClient, contextPlayer, contextRole;
    private static int contextScene;
    private static byte contextMap;
    private static bool contextInitialized, contextReady, contextHost, contextPractice, contextDead;
    private static bool _unfixableLights;
    public static string StatusText { get; private set; } = "Choose a switch to send a sabotage or repair request.";

    public static void ResetSession()
    {
        contextInitialized = false;
        Reactor.Reset(); Oxygen.Reset(); Comms.Reset(); Electrical.Reset();
        CheatToggles.reactorSab = CheatToggles.oxygenSab = CheatToggles.commsSab = CheatToggles.elecSab = false;
        CheatToggles.unfixableLights = _unfixableLights = false;
        StatusText = "Choose a switch to send a sabotage or repair request.";
    }

    private static bool Ready => OwnedActionContext.GameplayReady && !Utils.isMeeting && Utils.isNormalGame;
    private static bool Context(ShipStatus ship, byte map)
    {
        var client = AmongUsClient.Instance;
        var local = PlayerControl.LocalPlayer;
        var role = local && local.Data ? local.Data.Role : null;
        var shipPointer = ship ? ship.Pointer : IntPtr.Zero;
        var clientPointer = client ? client.Pointer : IntPtr.Zero;
        var playerPointer = local ? local.Pointer : IntPtr.Zero;
        var rolePointer = role ? role.Pointer : IntPtr.Zero;
        var dead = local && local.Data && local.Data.IsDead;
        var scene = SceneManager.GetActiveScene().handle;
        var ready = ship && Ready;
        var host = Utils.isHost;
        var practice = Utils.isFreePlay;
        if (!contextInitialized || contextShip != shipPointer || contextClient != clientPointer ||
            contextPlayer != playerPointer || contextRole != rolePointer || contextScene != scene ||
            contextMap != map || contextReady != ready || contextHost != host || contextPractice != practice || contextDead != dead)
        {
            ResetSession();
            contextInitialized = true; contextShip = shipPointer; contextClient = clientPointer;
            contextPlayer = playerPointer; contextRole = rolePointer; contextScene = scene;
            contextMap = map; contextReady = ready; contextHost = host; contextPractice = practice; contextDead = dead;
            return true;
        }
        return false;
    }

    private static bool CanRequest(bool start)
    {
        if (!Ready)
        { StatusText = "Sabotage controls require an active Normal round."; return false; }
        if (Utils.isHost || Utils.isFreePlay) return true;
        var local = PlayerControl.LocalPlayer;
        if (!local || !local.Data || !local.Data.Role) return false;
        if (!start && local.Data.IsDead)
        { StatusText = "Repair requests are unavailable while dead."; return false; }


        return true;
    }

    private static void SendRequest(ShipStatus ship, SystemTypes kind, byte amount, bool start)
    {
        ship.RpcUpdateSystem(kind, amount);
        StatusText = (start ? "Sabotage" : "Repair") + " request sent." +
            (Utils.isHost || Utils.isFreePlay ? "" : " The host may reject it.");
    }

    private static T System<T>(ShipStatus ship, SystemTypes kind) where T : Il2CppObjectBase
    {
        if (!ship || ship.Systems == null || !ship.Systems.TryGetValue(kind, out var value) || value == null) return null;
        try { return value.TryCast<T>(); }
        catch (Exception ex) when (ex is InvalidCastException or NullReferenceException) { return null; }
    }



    public static void SyncForMenu()
    {
        var ship = ShipStatus.Instance;
        var map = Utils.GetCurrentMapID();
        Context(ship, map);
        ReadReactor(ship, map, false); ReadOxygen(ship, map, false);
        ReadComms(ship, map, false); ReadElectrical(ship, map, false);
    }

    public static bool IsAvailable(SabotageKind kind)
    {
        var ship = ShipStatus.Instance;
        var map = Utils.GetCurrentMapID();
        return kind switch
        {
            SabotageKind.Reactor => map == 4
                ? System<HeliSabotageSystem>(ship, SystemTypes.HeliSabotage) != null
                : System<ReactorSystemType>(ship, map == 2 ? SystemTypes.Laboratory : SystemTypes.Reactor) != null,
            SabotageKind.Oxygen => map is not (2 or 4 or 5) && System<LifeSuppSystemType>(ship, SystemTypes.LifeSupp) != null,
            SabotageKind.Lights => map != 5 && System<SwitchSystem>(ship, SystemTypes.Electrical) != null,
            SabotageKind.Communications => map is 1 or 5
                ? System<HqHudSystemType>(ship, SystemTypes.Comms) != null
                : System<HudOverrideSystemType>(ship, SystemTypes.Comms) != null,
            _ => false
        };
    }

    public static bool IsActive(SabotageKind kind)
    {
        var ship = ShipStatus.Instance;
        var map = Utils.GetCurrentMapID();
        return kind switch
        {
            SabotageKind.Reactor => map == 4
                ? System<HeliSabotageSystem>(ship, SystemTypes.HeliSabotage)?.IsActive == true
                : System<ReactorSystemType>(ship, map == 2 ? SystemTypes.Laboratory : SystemTypes.Reactor)?.IsActive == true,
            SabotageKind.Oxygen => map is not (2 or 4 or 5) && System<LifeSuppSystemType>(ship, SystemTypes.LifeSupp)?.IsActive == true,
            SabotageKind.Lights => map != 5 && System<SwitchSystem>(ship, SystemTypes.Electrical)?.IsActive == true,
            SabotageKind.Communications => map is 1 or 5
                ? System<HqHudSystemType>(ship, SystemTypes.Comms)?.IsActive == true
                : System<HudOverrideSystemType>(ship, SystemTypes.Comms)?.IsActive == true,
            _ => false
        };
    }

    public static bool Request(SabotageKind kind, bool active)
    {
        SyncForMenu();
        if (!CanRequest(active)) return false;
        if (!IsAvailable(kind))
        { StatusText = "This sabotage is unavailable on the current map."; return false; }
        if (IsActive(kind) == active)
        { StatusText = active ? "This sabotage is already active." : "This system is already repaired."; return false; }
        switch (kind)
        {
            case SabotageKind.Reactor: CheatToggles.reactorSab = active; break;
            case SabotageKind.Oxygen: CheatToggles.oxygenSab = active; break;
            case SabotageKind.Lights:
                if (!active && _unfixableLights) CheatToggles.unfixableLights = false;
                CheatToggles.elecSab = active;
                break;
            case SabotageKind.Communications: CheatToggles.commsSab = active; break;
            default: return false;
        }
        StatusText = (active ? "Sabotage" : "Repair") + " request queued.";
        return true;
    }

    private static void ReadReactor(ShipStatus ship, byte map, bool requests)
    {
        if (map == 4)
        {
            var system = System<HeliSabotageSystem>(ship, SystemTypes.HeliSabotage);
            if (system == null) { Reactor.Missing(ref CheatToggles.reactorSab); return; }
            if (Reactor.Observe(system.Pointer, system.IsActive, ref CheatToggles.reactorSab) && requests && CanRequest(CheatToggles.reactorSab))
            {
                if (CheatToggles.reactorSab) SendRequest(ship, SystemTypes.HeliSabotage, 128, true);
                else { SendRequest(ship, SystemTypes.HeliSabotage, 16 | 0, false); SendRequest(ship, SystemTypes.HeliSabotage, 16 | 1, false); }
            }
            if (requests) Reactor.Mirror(system.Pointer, system.IsActive, ref CheatToggles.reactorSab);
            return;
        }
        var kind = map == 2 ? SystemTypes.Laboratory : SystemTypes.Reactor;
        var reactor = System<ReactorSystemType>(ship, kind);
        if (reactor == null) { Reactor.Missing(ref CheatToggles.reactorSab); return; }
        if (Reactor.Observe(reactor.Pointer, reactor.IsActive, ref CheatToggles.reactorSab) && requests && CanRequest(CheatToggles.reactorSab))
            SendRequest(ship, kind, CheatToggles.reactorSab ? (byte)128 : (byte)16, CheatToggles.reactorSab);
        if (requests) Reactor.Mirror(reactor.Pointer, reactor.IsActive, ref CheatToggles.reactorSab);
    }

    private static void ReadOxygen(ShipStatus ship, byte map, bool requests)
    {

        var oxygen = map is 2 or 4 or 5 ? null : System<LifeSuppSystemType>(ship, SystemTypes.LifeSupp);
        if (oxygen == null) { Oxygen.Missing(ref CheatToggles.oxygenSab); return; }
        if (Oxygen.Observe(oxygen.Pointer, oxygen.IsActive, ref CheatToggles.oxygenSab) && requests && CanRequest(CheatToggles.oxygenSab))
            SendRequest(ship, SystemTypes.LifeSupp, CheatToggles.oxygenSab ? (byte)128 : (byte)16, CheatToggles.oxygenSab);
        if (requests) Oxygen.Mirror(oxygen.Pointer, oxygen.IsActive, ref CheatToggles.oxygenSab);
    }

    private static void ReadComms(ShipStatus ship, byte map, bool requests)
    {
        if (map is 1 or 5)
        {
            var hq = System<HqHudSystemType>(ship, SystemTypes.Comms);
            if (hq == null) { Comms.Missing(ref CheatToggles.commsSab); return; }
            if (Comms.Observe(hq.Pointer, hq.IsActive, ref CheatToggles.commsSab) && requests && CanRequest(CheatToggles.commsSab))
            {
                if (CheatToggles.commsSab) SendRequest(ship, SystemTypes.Comms, 128, true);
                else { SendRequest(ship, SystemTypes.Comms, 16 | 0, false); SendRequest(ship, SystemTypes.Comms, 16 | 1, false); }
            }
            if (requests) Comms.Mirror(hq.Pointer, hq.IsActive, ref CheatToggles.commsSab);
            return;
        }
        var comms = System<HudOverrideSystemType>(ship, SystemTypes.Comms);
        if (comms == null) { Comms.Missing(ref CheatToggles.commsSab); return; }
        if (Comms.Observe(comms.Pointer, comms.IsActive, ref CheatToggles.commsSab) && requests && CanRequest(CheatToggles.commsSab))
            SendRequest(ship, SystemTypes.Comms, CheatToggles.commsSab ? (byte)128 : (byte)16, CheatToggles.commsSab);
        if (requests) Comms.Mirror(comms.Pointer, comms.IsActive, ref CheatToggles.commsSab);
    }

    private static void ReadElectrical(ShipStatus ship, byte map, bool requests)
    {
        var electrical = map == 5 ? null : System<SwitchSystem>(ship, SystemTypes.Electrical);
        if (electrical == null)
        { Electrical.Missing(ref CheatToggles.elecSab); CheatToggles.unfixableLights = _unfixableLights = false; return; }
        var changed = Electrical.Observe(electrical.Pointer, electrical.IsActive && !_unfixableLights, ref CheatToggles.elecSab);
        if (!requests) return;
        if (ApplyUnfixLights(ship))
        { Electrical.Mirror(electrical.Pointer, electrical.IsActive && !_unfixableLights, ref CheatToggles.elecSab); return; }
        if (changed && CanRequest(CheatToggles.elecSab))
        {
            if (!CheatToggles.elecSab)
            {
                for (var i = 0; i < 5; i++)
                {
                    var mask = 1 << (i & 0x1F);
                    if ((electrical.ActualSwitches & mask) != (electrical.ExpectedSwitches & mask))
                        SendRequest(ship, SystemTypes.Electrical, (byte)i, false);
                }
            }
            else
            {
                CheatToggles.unfixableLights = false;
                byte amount = 4;
                for (var i = 0; i < 5; i++) if (BoolRange.Next(0.5f)) amount |= (byte)(1 << i);
                SendRequest(ship, SystemTypes.Electrical, (byte)(amount | 128), true);
            }
        }
        Electrical.Mirror(electrical.Pointer, electrical.IsActive && !_unfixableLights, ref CheatToggles.elecSab);
    }

    public static void HandleReactor(ShipStatus ship, byte map) { Context(ship, map); ReadReactor(ship, map, true); }
    public static void HandleOxygen(ShipStatus ship, byte map) { Context(ship, map); ReadOxygen(ship, map, true); }
    public static void HandleComms(ShipStatus ship, byte map) { Context(ship, map); ReadComms(ship, map, true); }
    public static void HandleElectrical(ShipStatus ship, byte map) { Context(ship, map); ReadElectrical(ship, map, true); }

    private static bool ApplyUnfixLights(ShipStatus ship)
    {
        if (!Ready || (!Utils.isHost && !Utils.isFreePlay))
        { CheatToggles.unfixableLights = _unfixableLights = false; return false; }
        if (CheatToggles.unfixableLights == _unfixableLights) return false;
        if (!_unfixableLights) CheatToggles.elecSab = false;
        ship.RpcUpdateSystem(SystemTypes.Electrical, 69);
        _unfixableLights = CheatToggles.unfixableLights;
        return true;
    }

    public static void HandleUnfixLights(ShipStatus ship)
    {
        Context(ship, Utils.GetCurrentMapID());
        if (System<SwitchSystem>(ship, SystemTypes.Electrical) != null) ApplyUnfixLights(ship);
    }

    public static void HandleMushMix(ShipStatus ship, byte map)
    {
        if (!CheatToggles.mushSab) return;
        CheatToggles.mushSab = false;
        if (map == 5 && CanRequest(true) && ship && ship.Systems != null &&
            ship.Systems.ContainsKey(SystemTypes.MushroomMixupSabotage))
            SendRequest(ship, SystemTypes.MushroomMixupSabotage, 1, true);
    }

    public static void HandleSpores(FungleShipStatus ship, byte map)
    {
        if (!CheatToggles.mushSpore) return;
        CheatToggles.mushSpore = false;
        if (map != 5 || !CanRequest(true) || !ship || ship.sporeMushrooms == null) return;
        foreach (var mushroom in ship.sporeMushrooms.Values)
            if (mushroom != null) PlayerControl.LocalPlayer.CmdCheckSporeTrigger(mushroom);
    }

    public static void HandleDoors(ShipStatus ship)
    {
        if (CheatToggles.closeAllDoors) { DoorsHandler.CloseAllDoors(); CheatToggles.closeAllDoors = false; }
        if (CheatToggles.openAllDoors) { DoorsHandler.OpenAllDoors(); CheatToggles.openAllDoors = false; }
        if (CheatToggles.spamCloseAllDoors) DoorsHandler.CloseAllDoors();
        if (CheatToggles.spamOpenAllDoors) DoorsHandler.OpenAllDoors();
    }

    public static void Process(ShipStatus ship)
    {
        var map = Utils.GetCurrentMapID();
        Context(ship, map);
        ReadReactor(ship, map, true); ReadOxygen(ship, map, true);
        ReadComms(ship, map, true); ReadElectrical(ship, map, true);
        HandleDoors(ship);
    }

    public static void ProcessFungle(FungleShipStatus ship)
    { Context(ship, Utils.GetCurrentMapID()); HandleMushMix(ship, Utils.GetCurrentMapID()); HandleSpores(ship, Utils.GetCurrentMapID()); }
}
