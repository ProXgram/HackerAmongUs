using Il2CppInterop.Runtime.InteropTypes;
namespace Il2CppInterop.Runtime.InteropTypes
{
    public class Il2CppObjectBase
    {
        private static int next; public IntPtr Pointer = new(++next); public bool Destroyed, ThrowOnCast;
        public static implicit operator bool(Il2CppObjectBase value) => value != null && !value.Destroyed;
        public static bool operator !(Il2CppObjectBase value) => !(bool)value;
        public T TryCast<T>() where T : Il2CppObjectBase
        { if (ThrowOnCast) throw new InvalidCastException("Test missing cast"); return this as T; }
    }
}
public enum SystemTypes { Reactor,Laboratory,HeliSabotage,LifeSupp,Comms,Electrical,MushroomMixupSabotage }
public class ReactorSystemType : Il2CppObjectBase { public bool IsActive; }
public class HeliSabotageSystem : Il2CppObjectBase { public bool IsActive; }
public class LifeSuppSystemType : Il2CppObjectBase { public bool IsActive; }
public class HqHudSystemType : Il2CppObjectBase { public bool IsActive; }
public class HudOverrideSystemType : Il2CppObjectBase { public bool IsActive; }
public class SwitchSystem : Il2CppObjectBase { public bool IsActive; public byte ActualSwitches,ExpectedSwitches; }
public class ShipStatus : Il2CppObjectBase
{
    public static ShipStatus Instance;
    public Dictionary<SystemTypes,Il2CppObjectBase> Systems = [];
    public List<(SystemTypes System,byte Amount)> Requests = [];
    public void RpcUpdateSystem(SystemTypes system,byte amount) => Requests.Add((system,amount));
}
public class FungleShipStatus : ShipStatus { public Dictionary<int,object> sporeMushrooms = []; }
public class AmongUsClient : Il2CppObjectBase { public static AmongUsClient Instance; }
public class PlayerControl : Il2CppObjectBase
{
    public static PlayerControl LocalPlayer; public bool AmOwner=true; public PlayerData Data=new();
    public void CmdCheckSporeTrigger(object mushroom) { }
}
public class PlayerData : Il2CppObjectBase { public bool IsDead,Disconnected; public RoleBehaviour Role=new(); }
public class RoleBehaviour : Il2CppObjectBase { public bool IsImpostor; }
public static class BoolRange { public static bool Next(float value) => false; }
namespace UnityEngine.SceneManagement
{
    public struct Scene { public int handle; }
    public static class SceneManager { public static int Handle=1; public static Scene GetActiveScene()=>new(){handle=Handle}; }
}
namespace HAU
{
    public static class Utils
    { public static bool isHost,isFreePlay,isMeeting,isNormalGame=true; public static byte Map; public static byte GetCurrentMapID()=>Map; }
    public static class OwnedActionContext
    {
        public static bool Ready=true;
        public static bool GameplayReady => Ready && PlayerControl.LocalPlayer && PlayerControl.LocalPlayer.AmOwner &&
            PlayerControl.LocalPlayer.Data && !PlayerControl.LocalPlayer.Data.Disconnected && AmongUsClient.Instance;
    }
    public static class LocalImpostorHandler
    { public static bool Blocks; public static bool BlocksSabotageRequests(PlayerControl player)=>Blocks; }
    public static class CheatToggles
    { public static bool reactorSab,oxygenSab,commsSab,elecSab,unfixableLights,mushSab,mushSpore,closeAllDoors,openAllDoors,spamCloseAllDoors,spamOpenAllDoors; }
    public static class DoorsHandler { public static void CloseAllDoors(){} public static void OpenAllDoors(){} }
}
