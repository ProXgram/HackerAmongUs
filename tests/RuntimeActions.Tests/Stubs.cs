using System;
using System.Collections.Generic;
using AmongUs.GameOptions;
using UnityEngine;
public class NativeObject { static int next; public IntPtr Pointer = new(++next); public bool Destroyed; public static implicit operator bool(NativeObject o)=>o!=null&&!o.Destroyed; public static bool operator !(NativeObject o)=>!(bool)o; }
public class PlayerControl:MonoBehaviour {
 public static PlayerControl LocalPlayer; public static List<PlayerControl> AllPlayerControls=[];
 public bool AmOwner=true,moveable=true,inVent,onLadder,inMovingPlat; public bool CanMove=>moveable;
 public NetworkedPlayerInfo Data; public PlayerPhysics MyPhysics=new(); public NetTransform NetTransform=new(); public Transform transform=new(); public Collider Collider=new(); public Cosmetics cosmetics=new(); public List<PlayerTask> myTasks=[]; public int protectedByGuardianId=-1;
 public Vector2 GetTruePosition()=>new(transform.position.x,transform.position.y);
 public void SetKillTimer(float t){} public int Reports,Starts,Protects,Deaths,Revives;
 public void CmdReportDeadBody(NetworkedPlayerInfo d)=>Reports++; public void RpcStartMeeting(object x)=>Starts++;
 public void RpcProtectPlayer(PlayerControl p,int c)=>Protects++; public void Die(DeathReason r,bool b){Deaths++;Data.IsDead=true;} public void Revive(){Revives++;Data.IsDead=false;}
}
public enum DeathReason {Exile} public enum MurderResultFlags {Succeeded}
public class NetworkedPlayerInfo:NativeObject { public PlayerControl Object; public bool IsDead,Disconnected; public RoleBehaviour Role; public RoleTypes RoleType; public RoleTypes? RoleWhenAlive; }
public class RoleBehaviour:NativeObject {public RoleTypes Role;public RoleTeamTypes TeamType;public bool IsDead,CanVent;}
public class RoleManager:NativeObject {public static RoleManager Instance=new();public void SetRole(PlayerControl p,RoleTypes r){p.Data.RoleType=r;p.Data.Role=HAU.Utils.GetBehaviourByRoleType(r);}}
public class Collider:NativeObject {public bool enabled=true;} public class Cosmetics:NativeObject {public int ColorId;public Pet CurrentPet;} public class Pet:NativeObject {public Vector2 PettingPlayerPosition;public Transform transform=new();}
public class PlayerPhysics:NativeObject {public uint NetId;public bool AmOwner=true;public PlayerControl myPlayer;}
public class NetTransform:NativeObject {public List<Vector2> Snaps=[];public void RpcSnapTo(Vector2 p)=>Snaps.Add(p);}
public class AmongUsClient:NativeObject {public static AmongUsClient Instance=new();public int Starts,PetRequests;public List<System.Collections.IEnumerator> Pending=[];public void SendStartGame()=>Starts++;public void StartCoroutine(System.Collections.IEnumerator e)=>Pending.Add(e);public void LateBroadcastReliableMessage(IGameDataMessage m)=>PetRequests++; }
public class ShipStatus:NativeObject {public static ShipStatus Instance=new();public float EmergencyCooldown;public List<Vent> AllVents=[];public int SecurityRequests;public void RpcUpdateSystem(SystemTypes s,int b)=>SecurityRequests++;}
public class Vent {public int Id;}
public static class VentilationSystem {public enum Operation{BootImpostors} public static int Boots;public static void Update(Operation o,int id)=>Boots++;}
public class MeetingHud:NativeObject {public enum MeetingStates{Discussion,NotVoted,Voted,Results} public static MeetingHud Instance;public MeetingStates state;public struct VoterState{}public bool DespawnOnDestroy;public GameObject gameObject=new();public int Votes;public void RpcVotingComplete(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<VoterState> x,NetworkedPlayerInfo p,bool a,bool b,ushort n)=>Votes++;}
public class MeetingRoomManager {public static MeetingRoomManager Instance=new();public void AssignSelf(PlayerControl p,object o){} }
public class HudManager:NativeObject {public static HudManager Instance=new();public static bool InstanceExists=>Instance;public bool IsIntroDisplayed;public AbilityButton ImpostorVentButton=new();public ChatController Chat=new();public Notifier Notifier=new();public AbilityButton AbilityButton=new();public void StartCoroutine(object o){}public object CoFadeFullScreen(Color a,Color b,float t,bool v)=>null;public void SetMapAndInfoButtonsEnabled(bool v){}public void SetHudActive(bool v){}public void OpenMeetingRoom(PlayerControl p){}public void ToggleMapVisible(MapOptions m){} }
public class ChatController:NativeObject {public bool IsOpenOrOpening;}public class Notifier {public void AddDisconnectMessage(string s){} } public class AbilityButton {public GameObject gameObject=new();public void ResetCoolDown(){} public void SetCooldownFill(float v){} }
public class Minigame:NativeObject {public static Minigame Instance;}
public class ExileController:NativeObject {public static ExileController Instance;public void ReEnableGameplay(){} public void WrapUp(){} }
public class ControllerManager {public static ControllerManager Instance=new();public void CloseAndResetAll(){} }
public class GameManager {public static GameManager Instance=new();public Options LogicOptions=new();public bool CheckTaskCompletion()=>true;}
public class Options {public float GetKillCooldown()=>10;public float GetEmergencyCooldown()=>10;public float GetRoleFloat(FloatOptionNames n)=>10;}
public class MapOptions {public enum Modes{Sabotage}public Modes Mode;}
public class MapBehaviour:NativeObject {public static MapBehaviour Instance;public float trackedPointDelayTime;}
public class EngineerRole {public float inVentTimeRemaining,cooldownSecondsRemaining;public float GetCooldown()=>10;}
public class ShapeshifterRole {public float durationSecondsRemaining;}public class ScientistRole {public float currentCooldown,currentCharge;public float RoleCooldownValue=10;}public class TrackerRole {public float cooldownSecondsRemaining,delaySecondsRemaining,durationSecondsRemaining;}
public enum SystemTypes {Security}public enum TaskTypes {PrimeShields,ClearAsteroids,EmptyGarbage}public enum MapNames {Skeld,Dleks,Polus,MiraHQ,Fungle}
public class PlayerTask {public bool IsComplete;}
public class LogicGameFlowNormal {public void CheckEndCriteria(){}public void IsGameOverDueToDeath(){} }public class LogicGameFlowHnS {public void CheckEndCriteria(){}public void IsGameOverDueToDeath(){} }
public class FollowerCamera:NativeObject {public MonoBehaviour Target;public bool enabled=true,Locked;public void SetTarget(MonoBehaviour p)=>Target=p;}
public static class DestroyableSingleton<T> where T:new() {public static T Instance=new();}
public interface IGameDataMessage{}
namespace AmongUs.GameOptions {public enum RoleTypes {Crewmate,Impostor,Shapeshifter,Phantom,Viper,Judge,Tracker,Noisemaker,Engineer,Scientist,Detective,CrewmateGhost,ImpostorGhost} public enum RoleTeamTypes {Crewmate,Impostor}public enum FloatOptionNames {ShapeshifterDuration,TrackerDelay,TrackerDuration} }
namespace AmongUs.InnerNet.GameDataMessages {public interface IGameDataMessage:global::IGameDataMessage{} public class RpcPetMessage:IGameDataMessage {public RpcPetMessage(uint id,Vector2 a,Vector2 b){} } }
namespace Il2CppInterop.Runtime.InteropTypes.Arrays {public class Il2CppStructArray<T> {public Il2CppStructArray(long n){} } }
namespace Il2CppSystem.Collections.Generic {public class List<T>:System.Collections.Generic.List<T>{}}
namespace BepInEx.Unity.IL2CPP.Utils {public static class Unused{}}
namespace HarmonyLib {[AttributeUsage(AttributeTargets.Class)] public class HarmonyPatch:Attribute {public HarmonyPatch(Type t,string m){} } }
namespace UnityEngine {
 public class MonoBehaviour:NativeObject {}
 public class Object {public static void Destroy(object o){} }
 public struct Color {public static Color black,clear;}
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static implicit operator Vector2(Vector3 v)=>new(v.x,v.y);}
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 operator +(Vector3 a,Vector3 b)=>new(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator *(Vector3 a,float b)=>new(a.x*b,a.y*b,a.z*b);}
 public class Transform {public Vector3 position;}
 public class GameObject {public void SetActive(bool v){}public FollowerCamera Follower=new();public T GetComponent<T>() where T:class=>Follower as T;}
 public class Camera:NativeObject {public static Camera main=new();public GameObject gameObject=new();public Transform transform=new();public T GetComponent<T>() where T:class=>gameObject.GetComponent<T>();public Vector3 ScreenToWorldPoint(Vector3 p)=>p;}
 public class WaitForSeconds {public WaitForSeconds(float t){} }
 public static class Application {public static bool isFocused=true;}
 public static class Input {public static bool Right;public static Vector3 mousePosition;public static float H,V;public static bool GetMouseButtonDown(int b)=>Right;public static float GetAxis(string s)=>s=="Horizontal"?H:V;}
 public static class Time {public static float deltaTime=1;public static int frameCount;public static double realtimeSinceStartupAsDouble;}
}
namespace UnityEngine.SceneManagement {public struct Scene {public int handle;}public static class SceneManager {public static int Handle=1;public static Scene GetActiveScene()=>new(){handle=Handle};}}
namespace HAU {
 public static class HAUPlugin {public static bool isPanicked;}
 public static class MenuUI {public static bool isGUIActive;}
 public static class MovementAutomation {public static bool NeedsGroundCollision;}
 public static class LocalImpostorHandler {public static bool Blocks;public static bool BlocksKillRequests(PlayerControl p)=>Blocks;public static bool BlockOwnKill(PlayerControl p)=>Blocks;}
 public static class ProtectUI {public static Il2CppSystem.Collections.Generic.List<PlayerControl> playersToProtect=[];}
 public static class Utils {public static bool isClient=true,isInGame=true,isHost,isFreePlay,isLobby,isMeeting,isExiling;public static int Murders,Tasks;public static byte MapId;public static List<byte> Animations=new();public static bool isPlayer=>PlayerControl.LocalPlayer;public static void MurderPlayer(PlayerControl p,MurderResultFlags f)=>Murders++;public static void CompleteTask(PlayerTask t)=>Tasks++;public static void ForceSetScanner(PlayerControl p,bool v){}public static void ForcePlayAnimation(byte v)=>Animations.Add(v);public static byte GetCurrentMapID()=>MapId;
 public static RoleBehaviour GetBehaviourByRoleType(RoleTypes r)=>new(){Role=r,IsDead=r is RoleTypes.CrewmateGhost or RoleTypes.ImpostorGhost,TeamType=r is RoleTypes.Impostor or RoleTypes.ImpostorGhost or RoleTypes.Shapeshifter or RoleTypes.Phantom or RoleTypes.Viper?RoleTeamTypes.Impostor:RoleTeamTypes.Crewmate};
 public static Il2CppSystem.Collections.Generic.List<NetworkedPlayerInfo> GetAllPlayerData(){var list=new Il2CppSystem.Collections.Generic.List<NetworkedPlayerInfo>();foreach(var p in PlayerControl.AllPlayerControls)if(p&&p.Data!=null)list.Add(p.Data);return list;}}
 public enum OutfitPreset {Crewmate,Dead,Impostor,Shapeshifter,Phantom,Viper,Tracker,Noisemaker,Engineer,Scientist,Detective,Judge}
 public class PlayerPickMenu:NativeObject {public static PlayerPickMenu playerpickMenu;public static NetworkedPlayerInfo targetPlayerData;public static Action Callback;public static Il2CppSystem.Collections.Generic.List<NetworkedPlayerInfo> Choices;public void Close(){playerpickMenu=null;}public static void OpenPlayerPickMenu(Il2CppSystem.Collections.Generic.List<NetworkedPlayerInfo> c,Action callback){Choices=c;Callback=callback;playerpickMenu=new();}public static NetworkedPlayerInfo CustomPPMChoice(string name,OutfitPreset o,RoleBehaviour role)=>new(){Role=role,RoleType=role.Role};}
}

namespace HAU {public static class CheatToggles {public static bool animAsteroids,animCamsInUse,animEmptyGarbage,animMedScan,animPet,animShields,callMeeting,closeMeeting,completeMyTasks,ejectPlayer,endlessBattery,endlessSsDuration,endlessTracking,endlessVentTime,forceRole,forceStartGame,freecam,kickVents,killAll,killAllCrew,killAllImps,killPlayer,noClip,noGameEnd,noTrackingCooldown,noTrackingDelay,noVentCooldown,noVitalsCooldown,reportBody,sabotageMap,setFakeAlive,setFakeRole,skipMeeting,spectate,telekillPlayer,teleportCursor,teleportPlayer,unlockVents,walkInVents;public static AmongUs.GameOptions.RoleTypes? forcedRole;public static void DisablePPMCheats(string n){} }}

namespace HAU {public static class MultiRoleHandler {public static bool VentAccess;} }
