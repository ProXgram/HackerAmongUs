using System;
using System.Collections.Generic;
using System.Globalization;
using AmongUs.Data;
using AmongUs.Data.Player;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using InnerNet;
using UnityEngine;

namespace HAU;

public enum OutfitCategory { Color, Hat, Skin, Visor, Pet, Nameplate }

public sealed class OutfitCatalogItem
{
    public OutfitCategory Category { get; }
    public string Id { get; }
    public string Name { get; }
    internal CosmeticData Data { get; }
    internal OutfitCatalogItem(OutfitCategory category, string id, string name, CosmeticData data = null)
    { Category = category; Id = id; Name = name; Data = data; }
}

// Selections use the same unlocked catalogs and cosmetic requests as the game's
// inventory. This picker neither purchases cosmetics nor retries rejected RPCs.
public static class OutfitPickerHandler
{
    private const double CooldownSeconds = 2;
    public const int PageSize = 10;
    private static readonly Dictionary<OutfitCategory, List<OutfitCatalogItem>> Catalogs = new();
    private static IntPtr _managerPointer, _profilePointer;
    private static bool _freeCosmetics;
    private static double _nextActionAt, _lastClock;
    private static OutfitSnapshot _original, _savedLook;

    public static string StatusText { get; private set; } = "Select a category, preview an item, then press Equip.";
    public static bool CanRestore => HasProfileSnapshot(_original);
    public static bool HasSavedLook => HasProfileSnapshot(_savedLook);

    public static IReadOnlyList<OutfitCatalogItem> GetCatalog(OutfitCategory category)
    {
        EnsureCatalog();
        return Catalogs.TryGetValue(category, out var items) ? items : Array.Empty<OutfitCatalogItem>();
    }

    public static void RefreshCatalog()
    {
        _managerPointer = IntPtr.Zero;
        Catalogs.Clear();
        EnsureCatalog();
    }

    public static List<OutfitCatalogItem> Search(OutfitCategory category, string query)
    {
        var result = new List<OutfitCatalogItem>();
        var search = (query ?? string.Empty).Trim();
        foreach (var item in GetCatalog(category))
            if (search.Length == 0 || item.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                item.Id.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) result.Add(item);
        return result;
    }

    public static bool IsEquipped(OutfitCatalogItem item) => item != null &&
        Equivalent(item.Category, item.Id, EquippedId(item.Category));

    public static string EquippedName(OutfitCategory category)
    {
        var id = EquippedId(category);
        foreach (var item in GetCatalog(category)) if (Equivalent(category, item.Id, id)) return item.Name;
        return string.IsNullOrEmpty(id) ? "None" : ReadableId(id);
    }

    public static bool CanChange(out string reason)
    {
        try { return TryContext(out _, out _, out reason); }
        catch { reason = "Wait until your player profile is ready."; return false; }
    }

    public static void Equip(OutfitCatalogItem item)
    {
        if (item == null) { StatusText = "Select an item first."; return; }
        Change(saved => saved.With(item.Category, item.Id), item.Category, "Selected item");
    }

    public static void Clear(OutfitCategory category)
    {
        if (category == OutfitCategory.Color) { StatusText = "Body color cannot be cleared."; return; }
        Change(saved => saved.With(category, string.Empty), category, "Empty slot");
    }

    public static void RestoreOriginal() => Change(_ => _original, null, "Original outfit");
    public static void WearSavedLook() => Change(_ => _savedLook, null, "Saved look");

    public static void SaveLook()
    {
        try
        {
            EnsureCatalog();
            if (!TryContext(out var profile, out _, out var reason)) { StatusText = reason; return; }
            _savedLook = ReadSaved(profile);
            StatusText = "Current outfit saved for this player profile during this session.";
        }
        catch { StatusText = "Your current outfit could not be saved yet."; }
    }

    private static void Change(Func<OutfitSnapshot, OutfitSnapshot> select, OutfitCategory? onlyCategory, string label)
    {
        try
        {
            EnsureCatalog();
            if (!TryContext(out var profile, out var local, out var reason)) { StatusText = reason; return; }
            var now = Time.realtimeSinceStartupAsDouble;
            if (!double.IsFinite(now)) { StatusText = "Wait until the game clock is ready."; return; }
            if (now < _lastClock) _nextActionAt = now + CooldownSeconds;
            _lastClock = now;
            if (now < _nextActionAt) { StatusText = "Wait two seconds between outfit changes."; return; }
            var before = ReadSaved(profile);
            var after = select(before);
            if (after == null || after.Profile != profile.Pointer)
            { StatusText = "There is no saved outfit for this player profile."; return; }
            if (!Available(after, onlyCategory))
            { StatusText = "That selection is unavailable in the current catalog or game mode. Refresh the catalog."; return; }
            if ((!onlyCategory.HasValue || onlyCategory == OutfitCategory.Color) && IsOccupiedColor(after.Color, local))
            { StatusText = "That body color is already in use. Choose an available color."; return; }

            _nextActionAt = now + CooldownSeconds;
            try { WriteSaved(profile, after, onlyCategory); profile.Save(); }
            catch
            {
                WriteSaved(profile, before, onlyCategory);
                try { profile.Save(); } catch { }
                StatusText = "The profile could not be saved. Your previous choices were kept.";
                return;
            }
            _original ??= before;
            if (local)
            {
                try { Request(local, after, onlyCategory); }
                catch { StatusText = label + " saved; the appearance request could not be completed. No retry was sent."; return; }
                StatusText = label + " saved and requested. Multiplayer acceptance is decided by the game.";
            }
            else StatusText = label + " saved. Join a lobby to wear it.";
        }
        catch { StatusText = "The outfit could not be changed. Wait until the catalog and player are ready."; }
    }

    private static bool TryContext(out PlayerData profile, out PlayerControl local, out string reason)
    {
        profile = null; local = null; reason = string.Empty;
        var client = AmongUsClient.Instance;
        var practice = Utils.isFreePlay;
        if (HAUPlugin.isPanicked || !Application.isFocused || Utils.isMeeting || Utils.isExiling ||
            (!practice && (Utils.isInGame || (client && client.GameState == InnerNetClient.GameStates.Started))))
        { reason = "Equip outfits before the round or in practice."; return false; }
        local = PlayerControl.LocalPlayer;
        if (Utils.isLobby || practice)
        {
            if (!client || !local || !local.AmOwner || local.Data == null || local.Data.Disconnected ||
                local.Data.IsDead || local.CurrentOutfit == null || local.CurrentOutfitType != PlayerOutfitType.Default ||
                (practice && !ShipStatus.Instance) || local.inVent || local.onLadder || local.inMovingPlat || local.shapeshifting)
            { reason = "Wait until your living player and normal outfit are ready."; return false; }
        }
        else if ((client && client.GameState != InnerNetClient.GameStates.NotJoined) || local || ShipStatus.Instance)
        { reason = "Wait until the main menu or lobby is ready."; return false; }
        profile = DataManager.Player;
        if (profile == null || profile.Customization == null)
        { reason = "Wait until your player profile is ready."; return false; }
        return true;
    }

    private static void EnsureCatalog()
    {
        try
        {
            var profile = DataManager.Player;
            var pointer = profile?.Pointer ?? IntPtr.Zero;
            if (_profilePointer != pointer)
            {
                _profilePointer = pointer;
                _original = _savedLook = null;
                _nextActionAt = _lastClock = 0;
                Catalogs.Clear(); _managerPointer = IntPtr.Zero;
            }
            if (!DestroyableSingleton<HatManager>.InstanceExists)
            { Catalogs.Clear(); _managerPointer = IntPtr.Zero; return; }
            var manager = DestroyableSingleton<HatManager>.Instance;
            if (_managerPointer == manager.Pointer && _freeCosmetics == CheatToggles.freeCosmetics && Catalogs.Count == 6) return;
            Catalogs.Clear();
            Catalogs[OutfitCategory.Color] = Colors();
            Catalogs[OutfitCategory.Hat] = Cosmetics(OutfitCategory.Hat, manager.GetUnlockedHats(), manager);
            Catalogs[OutfitCategory.Skin] = Cosmetics(OutfitCategory.Skin, manager.GetUnlockedSkins(), manager);
            Catalogs[OutfitCategory.Visor] = Cosmetics(OutfitCategory.Visor, manager.GetUnlockedVisors(), manager);
            Catalogs[OutfitCategory.Pet] = Cosmetics(OutfitCategory.Pet, manager.GetUnlockedPets(), manager);
            Catalogs[OutfitCategory.Nameplate] = Cosmetics(OutfitCategory.Nameplate, manager.GetUnlockedNamePlates(), manager);
            _managerPointer = manager.Pointer;
            _freeCosmetics = CheatToggles.freeCosmetics;
        }
        catch { Catalogs.Clear(); _managerPointer = IntPtr.Zero; StatusText = "Wait until the outfit catalog is ready, then Refresh."; }
    }

    private static List<OutfitCatalogItem> Colors()
    {
        var items = new List<OutfitCatalogItem>();
        var count = Math.Min(Palette.PlayerColors?.Length ?? 0, Palette.ShadowColors?.Length ?? 0);
        for (var i = 0; i < count && i <= byte.MaxValue; i++)
        {
            string name;
            try { name = Palette.GetColorName(i); } catch { name = "Color " + (i + 1); }
            items.Add(new OutfitCatalogItem(OutfitCategory.Color, i.ToString(CultureInfo.InvariantCulture),
                string.IsNullOrWhiteSpace(name) ? "Color " + (i + 1) : name));
        }
        return items;
    }

    private static List<OutfitCatalogItem> Cosmetics<T>(OutfitCategory category, Il2CppReferenceArray<T> items,
        HatManager manager) where T : CosmeticData
    {
        var result = new List<OutfitCatalogItem> { new(category, string.Empty, "None / Clear") };
        if (items == null) return result;
        var seen = new HashSet<string>(StringComparer.Ordinal) { string.Empty, EmptyId(category) };
        for (var i = 0; i < items.Length; i++)
        {
            var data = items[i];
            if (!data) continue;
            var id = data.ProductId;
            if (string.IsNullOrWhiteSpace(id) || id == NetworkedPlayerInfo.PlayerOutfit.MISSING_COSMETIC_ID ||
                !seen.Add(id) || !manager.CheckLongModeValidCosmetic(id, false)) continue;
            string name;
            try { name = data.GetItemName(); } catch { name = null; }
            result.Add(new OutfitCatalogItem(category, id, string.IsNullOrWhiteSpace(name) ? ReadableId(id) : name, data));
        }
        result.Sort((a, b) =>
        {
            if (a.Id.Length == 0) return b.Id.Length == 0 ? 0 : -1;
            if (b.Id.Length == 0) return 1;
            var name = StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name);
            return name == 0 ? StringComparer.Ordinal.Compare(a.Id, b.Id) : name;
        });
        return result;
    }

    // Read only assets which the native inventory already has in memory. Opening
    // this window never starts an addressable load or owns a native asset handle.
    public static Sprite CachedSprite(OutfitCatalogItem item)
    {
        if (item?.Data == null) return null;
        try
        {
            var data = item.Data;
            var preview = data.PreviewData?.Asset?.TryCast<Sprite>();
            if (preview) return preview;
            if (item.Category == OutfitCategory.Pet)
            {
                var prefab = data.TryCast<PetData>()?.PetPrefabRef?.Asset?.TryCast<GameObject>();
                var renderers = prefab ? prefab.GetComponent<PetBehaviour>()?.renderers : null;
                if (renderers != null && renderers.Length > 0 && renderers[0]) return renderers[0].sprite;
                return null;
            }
            return item.Category switch
            {
                OutfitCategory.Hat => data.TryCast<HatData>()?.ViewDataRef?.Asset?.TryCast<HatViewData>()?.MainImage,
                OutfitCategory.Skin => data.TryCast<SkinData>()?.ViewDataRef?.Asset?.TryCast<SkinViewData>()?.IdleFrame,
                OutfitCategory.Visor => data.TryCast<VisorData>()?.ViewDataRef?.Asset?.TryCast<VisorViewData>()?.IdleFrame,
                OutfitCategory.Nameplate => data.TryCast<NamePlateData>()?.ViewDataRef?.Asset?.TryCast<NamePlateViewData>()?.Image,
                _ => null
            };
        }
        catch { return null; }
    }

    public static Color BodyColor(OutfitCatalogItem item)
    {
        try
        {
            if (item != null && item.Category == OutfitCategory.Color && int.TryParse(item.Id, out var id) &&
                id >= 0 && id < (Palette.PlayerColors?.Length ?? 0)) return Palette.PlayerColors[id];
        }
        catch { }
        return Color.gray;
    }

    private static bool Available(OutfitSnapshot snapshot, OutfitCategory? onlyCategory)
    {
        if (!DestroyableSingleton<HatManager>.InstanceExists) return false;
        var manager = DestroyableSingleton<HatManager>.Instance;
        foreach (OutfitCategory category in Enum.GetValues(typeof(OutfitCategory)))
        {
            if (onlyCategory.HasValue && category != onlyCategory.Value) continue;
            var found = false;
            foreach (var item in GetCatalog(category)) if (Equivalent(category, item.Id, snapshot.Id(category))) { found = true; break; }
            if (!found) return false;
            // Cached lists keep drawing cheap. Recheck the current native mode
            // at the explicit click so an item cannot outlive its validity.
            var id = NormalizeEmpty(category, snapshot.Id(category));
            if (category != OutfitCategory.Color && id.Length > 0 &&
                !manager.CheckLongModeValidCosmetic(id, false)) return false;
        }
        return true;
    }

    private static bool IsOccupiedColor(string id, PlayerControl local)
    {
        if (!Utils.isLobby || Utils.isFreePlay || !local || !byte.TryParse(id, out var color)) return false;
        foreach (var player in PlayerControl.AllPlayerControls)
            if (player && player.Pointer != local.Pointer && player.Data != null && !player.Data.Disconnected &&
                player.Data.DefaultOutfit != null && player.Data.DefaultOutfit.ColorId == color) return true;
        return false;
    }

    private static string EquippedId(OutfitCategory category)
    {
        try
        {
            var local = PlayerControl.LocalPlayer;
            var outfit = local ? local.CurrentOutfit : null;
            if (outfit != null)
                return category switch
                {
                    OutfitCategory.Color => outfit.ColorId.ToString(CultureInfo.InvariantCulture),
                    OutfitCategory.Hat => outfit.HatId, OutfitCategory.Skin => outfit.SkinId,
                    OutfitCategory.Visor => outfit.VisorId, OutfitCategory.Pet => outfit.PetId,
                    OutfitCategory.Nameplate => outfit.NamePlateId, _ => string.Empty
                };
            var profile = DataManager.Player;
            return profile?.Customization == null ? string.Empty : ReadSaved(profile).Id(category);
        }
        catch { return string.Empty; }
    }

    private static bool HasProfileSnapshot(OutfitSnapshot snapshot)
    {
        try { return snapshot != null && DataManager.Player != null && snapshot.Profile == DataManager.Player.Pointer; }
        catch { return false; }
    }

    private static bool Equivalent(OutfitCategory category, string a, string b) =>
        StringComparer.Ordinal.Equals(NormalizeEmpty(category, a), NormalizeEmpty(category, b));
    private static string NormalizeEmpty(OutfitCategory category, string id) =>
        string.IsNullOrEmpty(id) || (category != OutfitCategory.Color && id == EmptyId(category)) ? string.Empty : id;
    private static string EmptyId(OutfitCategory category) => category switch
    {
        OutfitCategory.Hat => HatData.EmptyId, OutfitCategory.Skin => SkinData.EmptyId,
        OutfitCategory.Visor => VisorData.EmptyId, OutfitCategory.Pet => PetData.EmptyId,
        OutfitCategory.Nameplate => NamePlateData.EmptyId, _ => string.Empty
    };
    private static string ReadableId(string id)
    {
        var value = id ?? string.Empty;
        var separator = value.IndexOf('_');
        if (separator >= 0 && separator < value.Length - 1) value = value.Substring(separator + 1);
        return value.Replace('_', ' ').Replace('-', ' ');
    }

    private static OutfitSnapshot ReadSaved(PlayerData profile) => new(profile.Pointer,
        profile.Customization.Color.ToString(CultureInfo.InvariantCulture), profile.Customization.Hat,
        profile.Customization.Skin, profile.Customization.Visor, profile.Customization.Pet, profile.Customization.NamePlate);

    private static void WriteSaved(PlayerData profile, OutfitSnapshot snapshot, OutfitCategory? category)
    {
        if (!category.HasValue || category == OutfitCategory.Color) profile.Customization.Color = byte.Parse(snapshot.Color, CultureInfo.InvariantCulture);
        if (!category.HasValue || category == OutfitCategory.Hat) profile.Customization.Hat = snapshot.Hat;
        if (!category.HasValue || category == OutfitCategory.Skin) profile.Customization.Skin = snapshot.Skin;
        if (!category.HasValue || category == OutfitCategory.Visor) profile.Customization.Visor = snapshot.Visor;
        if (!category.HasValue || category == OutfitCategory.Pet) profile.Customization.Pet = snapshot.Pet;
        if (!category.HasValue || category == OutfitCategory.Nameplate) profile.Customization.NamePlate = snapshot.Nameplate;
    }

    private static void Request(PlayerControl local, OutfitSnapshot snapshot, OutfitCategory? category)
    {
        if ((!category.HasValue || category == OutfitCategory.Color) && !IsOccupiedColor(snapshot.Color, local))
        {
            ColorCycleHandler.Stop();
            local.CmdCheckColor(byte.Parse(snapshot.Color, CultureInfo.InvariantCulture));
        }
        if (!category.HasValue || category == OutfitCategory.Hat) local.RpcSetHat(snapshot.Hat);
        if (!category.HasValue || category == OutfitCategory.Skin) local.RpcSetSkin(snapshot.Skin);
        if (!category.HasValue || category == OutfitCategory.Visor) local.RpcSetVisor(snapshot.Visor);
        if (!category.HasValue || category == OutfitCategory.Pet) local.RpcSetPet(snapshot.Pet);
        if (!category.HasValue || category == OutfitCategory.Nameplate) local.RpcSetNamePlate(snapshot.Nameplate);
    }

    private sealed class OutfitSnapshot
    {
        internal readonly IntPtr Profile;
        internal readonly string Color, Hat, Skin, Visor, Pet, Nameplate;
        internal OutfitSnapshot(IntPtr profile, string color, string hat, string skin, string visor, string pet, string nameplate)
        { Profile = profile; Color = color; Hat = hat; Skin = skin; Visor = visor; Pet = pet; Nameplate = nameplate; }
        internal string Id(OutfitCategory category) => category switch
        {
            OutfitCategory.Color => Color, OutfitCategory.Hat => Hat, OutfitCategory.Skin => Skin,
            OutfitCategory.Visor => Visor, OutfitCategory.Pet => Pet, OutfitCategory.Nameplate => Nameplate, _ => string.Empty
        };
        internal OutfitSnapshot With(OutfitCategory category, string id) => new(Profile,
            category == OutfitCategory.Color ? id : Color, category == OutfitCategory.Hat ? id : Hat,
            category == OutfitCategory.Skin ? id : Skin, category == OutfitCategory.Visor ? id : Visor,
            category == OutfitCategory.Pet ? id : Pet, category == OutfitCategory.Nameplate ? id : Nameplate);
    }
}
