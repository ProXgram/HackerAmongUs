

public class NativeObject
{
    private static int _next;
    public IntPtr Pointer = new(++_next);
    public bool Destroyed;
    public static implicit operator bool(NativeObject value) => value is not null && !value.Destroyed;
    public static bool operator !(NativeObject value) => !(bool)value;
    public T TryCast<T>() where T : class => this as T;
}
public enum PlayerOutfitType { Default, Shapeshift }
public sealed class NetworkedPlayerInfo
{
    public bool IsDead, Disconnected;
    public PlayerOutfit DefaultOutfit = new();
    public sealed class PlayerOutfit
    {
        public const string MISSING_COSMETIC_ID = "MISSING";
        public int ColorId = 1;
        public string HatId = "hat_first", SkinId = "skin_first", VisorId = "visor_first",
            PetId = "pet_first", NamePlateId = "nameplate_first";
    }
}
public sealed class PlayerControl : NativeObject
{
    public static PlayerControl LocalPlayer;
    public static List<PlayerControl> AllPlayerControls = [];
    public NetworkedPlayerInfo Data = new();
    public bool AmOwner = true, inVent, onLadder, inMovingPlat, shapeshifting, ThrowRequest;
    public PlayerOutfitType CurrentOutfitType;
    public NetworkedPlayerInfo.PlayerOutfit CurrentOutfit => Data?.DefaultOutfit;
    public List<(string Category, string Id)> Requests = [];
    private void Request(string category, string id)
    {
        if (ThrowRequest) throw new InvalidOperationException("Native request unavailable");
        Requests.Add((category, id));
    }
    public void CmdCheckColor(byte color) => Request("Color", color.ToString());
    public void RpcSetHat(string id) => Request("Hat", id);
    public void RpcSetSkin(string id) => Request("Skin", id);
    public void RpcSetVisor(string id) => Request("Visor", id);
    public void RpcSetPet(string id) => Request("Pet", id);
    public void RpcSetNamePlate(string id) => Request("Nameplate", id);
}
public sealed class AmongUsClient : NativeObject
{
    public static AmongUsClient Instance;
    public InnerNet.InnerNetClient.GameStates GameState;
}
public sealed class ShipStatus : NativeObject { public static ShipStatus Instance; }
public class CosmeticData : NativeObject
{
    public string ProductId, Name;
    public bool ThrowName;
    public AssetReference PreviewData;
    public string GetItemName() => ThrowName ? throw new InvalidOperationException() : Name;
}
public sealed class AssetReference
{
    public NativeObject Asset;
}
public sealed class HatData : CosmeticData { public const string EmptyId = "hat_NoHat"; public AssetReference ViewDataRef; }
public sealed class SkinData : CosmeticData { public const string EmptyId = "skin_None"; public AssetReference ViewDataRef; }
public sealed class VisorData : CosmeticData { public const string EmptyId = "visor_EmptyVisor"; public AssetReference ViewDataRef; }
public sealed class PetData : CosmeticData { public const string EmptyId = "pet_EmptyPet"; public AssetReference PetPrefabRef; }
public sealed class PetBehaviour : NativeObject { public UnityEngine.SpriteRenderer[] renderers; }
public sealed class NamePlateData : CosmeticData { public const string EmptyId = "nameplate_NoPlate"; public AssetReference ViewDataRef; }
public sealed class HatViewData : NativeObject { public UnityEngine.Sprite MainImage; }
public sealed class SkinViewData : NativeObject { public UnityEngine.Sprite IdleFrame; }
public sealed class VisorViewData : NativeObject { public UnityEngine.Sprite IdleFrame; }
public sealed class NamePlateViewData : NativeObject { public UnityEngine.Sprite Image; }
public sealed class HatManager : NativeObject
{
    public Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<HatData> Hats;
    public Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<SkinData> Skins;
    public Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<VisorData> Visors;
    public Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<PetData> Pets;
    public Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<NamePlateData> Nameplates;
    public Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<HatData> FreeHats;
    public HashSet<string> Invalid = [];
    public int Reads;
    public bool CheckLongModeValidCosmetic(string id, bool _) => !Invalid.Contains(id);
    public Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<HatData> GetUnlockedHats()
    { Reads++; return HAU.CheatToggles.freeCosmetics && FreeHats != null ? FreeHats : Hats; }
    public Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<SkinData> GetUnlockedSkins() { Reads++; return Skins; }
    public Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<VisorData> GetUnlockedVisors() { Reads++; return Visors; }
    public Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<PetData> GetUnlockedPets() { Reads++; return Pets; }
    public Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<NamePlateData> GetUnlockedNamePlates() { Reads++; return Nameplates; }
}
public static class DestroyableSingleton<T> where T : class
{
    public static T Instance;
    public static bool InstanceExists => Instance != null;
}
public static class Palette
{
    public static UnityEngine.Color[] PlayerColors, ShadowColors;
    public static string GetColorName(int i) => new[] { "Red", "Blue", "Green", "Pink" }[i];
}
namespace UnityEngine
{
    public static class Application { public static bool isFocused = true; }
    public static class Time { public static double realtimeSinceStartupAsDouble; }
    public struct Color { public float r; public static Color gray => new() { r = 0.5f }; }
    public sealed class Sprite : NativeObject { }
    public sealed class SpriteRenderer : NativeObject { public Sprite sprite; }
    public sealed class GameObject : NativeObject
    {
        public NativeObject Component;
        public T GetComponent<T>() where T : class => Component as T;
    }
}
namespace InnerNet
{
    public sealed class InnerNetClient { public enum GameStates { NotJoined, Joined, Started, Ended } }
}
namespace Il2CppInterop.Runtime.InteropTypes.Arrays
{
    public sealed class Il2CppReferenceArray<T>
    {
        private readonly T[] _items;
        public int Length => _items.Length;
        public T this[int index] => _items[index];
        public Il2CppReferenceArray(T[] items) => _items = items;
        public static implicit operator Il2CppReferenceArray<T>(T[] items) => items == null ? null : new(items);
    }
}
namespace AmongUs.Data.Player
{
    public sealed class PlayerCustomizationData
    {
        public byte Color = 1;
        public string Hat = "hat_first", Skin = "skin_first", Visor = "visor_first", Pet = "pet_first", NamePlate = "nameplate_first";
    }
    public sealed class PlayerData : NativeObject
    {
        public PlayerCustomizationData Customization = new();
        public int Saves, ThrowSaves;
        public void Save() { Saves++; if (ThrowSaves-- > 0) throw new InvalidOperationException("Save unavailable"); }
    }
}
namespace AmongUs.Data { public static class DataManager { public static Player.PlayerData Player; } }
namespace HAU
{
    public static class HAUPlugin { public static bool isPanicked; }
    public static class Utils { public static bool isLobby, isInGame, isFreePlay, isMeeting, isExiling; }
    public static class CheatToggles { public static bool freeCosmetics; }
    public static class ColorCycleHandler { public static int Stops; public static void Stop() => Stops++; }
}
