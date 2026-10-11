using System;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine.SceneManagement;

namespace HAU;

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

    public static void ResetSession()
    {
        contextInitialized = false;
        Reactor.Reset(); Oxygen.Reset(); Comms.Reset(); Electrical.Reset();
        CheatToggles.reactorSab = CheatToggles.oxygenSab = CheatToggles.commsSab = CheatToggles.elecSab = false;
        CheatToggles.unfixableLights = _unfixableLights = false;
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
        if (!Ready) return false;
        if (Utils.isHost || Utils.isFreePlay) return true;
        var local = PlayerControl.LocalPlayer;
        if (!local || !local.Data || !local.Data.Role) return false;
        return start ? local.Data.Role.IsImpostor && !LocalImpostorHandler.BlocksSabotageRequests(local) : !local.Data.IsDead;
    }

    private static T System<T>(ShipStatus ship, SystemTypes kind) where T : Il2CppObjectBase
    {
        if (!ship || ship.Systems == null || !ship.Systems.TryGetValue(kind, out var value) || value == null) return null;
        try { return value.TryCast<T>(); }
        catch (Exception ex) when (ex is InvalidCastException or NullReferenceException) { return null; }
    }

    // Called before menu switches. Pending explicit input is kept until the tick;
    // changed remote state or a new context always wins over older switch values.
    public static void SyncForMenu()
    {
        var ship = ShipStatus.Instance;
        var map = Utils.GetCurrentMapID();
        Context(ship, map);
        ReadReactor(ship, map, false); ReadOxygen(ship, map, false);
        ReadComms(ship, map, false); ReadElectrical(ship, map, false);
    }

    private static void ReadReactor(ShipStatus ship, byte map, bool requests)
    {
        if (map == 4)
        {
            var system = System<HeliSabotageSystem>(ship, SystemTypes.HeliSabotage);
            if (system == null) { Reactor.Missing(ref CheatToggles.reactorSab); return; }
            if (Reactor.Observe(system.Pointer, system.IsActive, ref CheatToggles.reactorSab) && requests && CanRequest(CheatToggles.reactorSab))
            {
                if (CheatToggles.reactorSab) ship.RpcUpdateSystem(SystemTypes.HeliSabotage, 128);
                else { ship.RpcUpdateSystem(SystemTypes.HeliSabotage, 16 | 0); ship.RpcUpdateSystem(SystemTypes.HeliSabotage, 16 | 1); }
            }
            if (requests) Reactor.Mirror(system.Pointer, system.IsActive, ref CheatToggles.reactorSab);
            return;
        }
        var kind = map == 2 ? SystemTypes.Laboratory : SystemTypes.Reactor;
        var reactor = System<ReactorSystemType>(ship, kind);
        if (reactor == null) { Reactor.Missing(ref CheatToggles.reactorSab); return; }
        if (Reactor.Observe(reactor.Pointer, reactor.IsActive, ref CheatToggles.reactorSab) && requests && CanRequest(CheatToggles.reactorSab))
            ship.RpcUpdateSystem(kind, CheatToggles.reactorSab ? (byte)128 : (byte)16);
        if (requests) Reactor.Mirror(reactor.Pointer, reactor.IsActive, ref CheatToggles.reactorSab);
    }

    private static void ReadOxygen(ShipStatus ship, byte map, bool requests)
    {
        // Polus, Airship and Fungle have no oxygen sabotage system. Mira HQ does.
        var oxygen = map is 2 or 4 or 5 ? null : System<LifeSuppSystemType>(ship, SystemTypes.LifeSupp);
        if (oxygen == null) { Oxygen.Missing(ref CheatToggles.oxygenSab); return; }
        if (Oxygen.Observe(oxygen.Pointer, oxygen.IsActive, ref CheatToggles.oxygenSab) && requests && CanRequest(CheatToggles.oxygenSab))
            ship.RpcUpdateSystem(SystemTypes.LifeSupp, CheatToggles.oxygenSab ? (byte)128 : (byte)16);
        if (requests) Oxygen.Mirror(oxygen.Pointer, oxygen.IsActive, ref CheatToggles.oxygenSab);
    }

    private static void ReadComms(ShipStatus ship, byte map, bool requests)
    {
        if (map is 1 or 5) // Mira HQ and Fungle use two-console communications.
        {
            var hq = System<HqHudSystemType>(ship, SystemTypes.Comms);
            if (hq == null) { Comms.Missing(ref CheatToggles.commsSab); return; }
            if (Comms.Observe(hq.Pointer, hq.IsActive, ref CheatToggles.commsSab) && requests && CanRequest(CheatToggles.commsSab))
            {
                if (CheatToggles.commsSab) ship.RpcUpdateSystem(SystemTypes.Comms, 128);
                else { ship.RpcUpdateSystem(SystemTypes.Comms, 16 | 0); ship.RpcUpdateSystem(SystemTypes.Comms, 16 | 1); }
            }
            if (requests) Comms.Mirror(hq.Pointer, hq.IsActive, ref CheatToggles.commsSab);
            return;
        }
        var comms = System<HudOverrideSystemType>(ship, SystemTypes.Comms);
        if (comms == null) { Comms.Missing(ref CheatToggles.commsSab); return; }
        if (Comms.Observe(comms.Pointer, comms.IsActive, ref CheatToggles.commsSab) && requests && CanRequest(CheatToggles.commsSab))
            ship.RpcUpdateSystem(SystemTypes.Comms, CheatToggles.commsSab ? (byte)128 : (byte)16);
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
                        ship.RpcUpdateSystem(SystemTypes.Electrical, (byte)i);
                }
            }
            else
            {
                CheatToggles.unfixableLights = false;
                byte amount = 4;
                for (var i = 0; i < 5; i++) if (BoolRange.Next(0.5f)) amount |= (byte)(1 << i);
                ship.RpcUpdateSystem(SystemTypes.Electrical, (byte)(amount | 128));
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
            ship.RpcUpdateSystem(SystemTypes.MushroomMixupSabotage, 1);
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
