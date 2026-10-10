using AmongUs.Data;
using AmongUs.Data.Player;
using HAU;
using UnityEngine;
using Mod = HAU.HAUPlugin;

var passed = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException("FAIL: " + name);
    passed++;
}
void Reset(bool lobby = false, bool practice = false)
{
    DataManager.Player = new PlayerData();
    Mod.isPanicked = Utils.isMeeting = Utils.isExiling = Utils.isInGame = false;
    Utils.isLobby = lobby; Utils.isFreePlay = practice;
    Application.isFocused = true; Time.realtimeSinceStartupAsDouble = 100;
    CheatToggles.freeCosmetics = false; ColorCycleHandler.Stops = 0;
    AmongUsClient.Instance = new() { GameState = lobby || practice
        ? InnerNet.InnerNetClient.GameStates.Joined : InnerNet.InnerNetClient.GameStates.NotJoined };
    PlayerControl.LocalPlayer = lobby || practice ? new PlayerControl() : null;
    PlayerControl.AllPlayerControls = PlayerControl.LocalPlayer == null ? [] : [PlayerControl.LocalPlayer];
    ShipStatus.Instance = practice ? new ShipStatus() : null;
    Palette.PlayerColors = [new() { r = 1 }, new(), new(), new()];
    Palette.ShadowColors = [new(), new(), new(), new()];
    DestroyableSingleton<HatManager>.Instance = new()
    {
        Hats = new HatData[] { new() { ProductId = "hat_first", Name = "First hat" }, new() { ProductId = "hat_second", Name = "Second hat" } },
        Skins = new SkinData[] { new() { ProductId = "skin_first", Name = "First skin" } },
        Visors = new VisorData[] { new() { ProductId = "visor_first", Name = "First visor" } },
        Pets = new PetData[] { new() { ProductId = "pet_first", Name = "First pet" } },
        Nameplates = new NamePlateData[] { new() { ProductId = "nameplate_first", Name = "First plate" } }
    };
    OutfitPickerHandler.RefreshCatalog();
}
OutfitCatalogItem Item(OutfitCategory category, string id) => OutfitPickerHandler.GetCatalog(category).Single(x => x.Id == id);
string Saved() => string.Join("|", DataManager.Player.Customization.Color, DataManager.Player.Customization.Hat,
    DataManager.Player.Customization.Skin, DataManager.Player.Customization.Visor,
    DataManager.Player.Customization.Pet, DataManager.Player.Customization.NamePlate);
void Unchanged(Action action, string name)
{
    var before = Saved(); var saves = DataManager.Player.Saves; var requests = PlayerControl.LocalPlayer?.Requests.Count ?? 0;
    action();
    Check(Saved() == before && DataManager.Player.Saves == saves &&
        (PlayerControl.LocalPlayer?.Requests.Count ?? 0) == requests, name);
}

Reset();
Check(Enum.GetValues<OutfitCategory>().Length == 6, "six cosmetic and body categories");
Check(OutfitPickerHandler.GetCatalog(OutfitCategory.Color).Count == 4, "color catalog uses native palette bounds");
Check(OutfitPickerHandler.GetCatalog(OutfitCategory.Nameplate).Count == 2, "native nameplate catalog is selectable");
Check(OutfitPickerHandler.GetCatalog(OutfitCategory.Hat)[0].Id == "", "clear choice is first");
Check(OutfitPickerHandler.Search(OutfitCategory.Hat, "  FIRST ").Single().Id == "hat_first", "trimmed case-insensitive item-name search");
Check(OutfitPickerHandler.Search(OutfitCategory.Hat, "hat_second").Single().Name == "Second hat", "catalog ID search");
Check(OutfitPickerHandler.Search(OutfitCategory.Skin, "Second").Count == 0, "search never leaks another category");
Check(OutfitPickerHandler.Search(OutfitCategory.Hat, null).Count == 3, "null search shows complete category");
var manager = DestroyableSingleton<HatManager>.Instance;
manager.Hats = new HatData[]
{
    null, new() { ProductId = "", Name = "Invalid" }, new() { ProductId = "MISSING" },
    new() { ProductId = HatData.EmptyId }, new() { ProductId = "hat_blocked", Name = "Blocked" },
    new() { ProductId = "hat_z", Name = "Alpha" }, new() { ProductId = "hat_a", Name = "Alpha" },
    new() { ProductId = "hat_a", Name = "Duplicate" }, new() { ProductId = "hat_b", ThrowName = true }
};
manager.Invalid.Add("hat_blocked");
OutfitPickerHandler.RefreshCatalog();
var hats = OutfitPickerHandler.GetCatalog(OutfitCategory.Hat);
Check(hats.Count == 4, "null, duplicate, missing, empty aliases and long-mode-invalid IDs filtered");
Check(hats[1].Id == "hat_a" && hats[2].Id == "hat_z", "same display names sorted stably by ID");
Check(hats[3].Name == "b", "missing localization falls back to readable product ID");
var reads = manager.Reads;
for (var frame = 0; frame < 100; frame++) OutfitPickerHandler.GetCatalog(OutfitCategory.Hat);
Check(manager.Reads == reads, "unchanged native catalog not re-enumerated per frame");
manager.FreeHats = new HatData[] { new() { ProductId = "hat_extra", Name = "Extra" } };
CheatToggles.freeCosmetics = true;
Check(OutfitPickerHandler.Search(OutfitCategory.Hat, "Extra").Count == 1, "existing extra-cosmetics setting invalidates cache");
Check(manager.Reads == reads + 5, "setting change rebuilds catalogs once");
var replacement = new HatManager { Hats = new HatData[] { new() { ProductId = "hat_new", Name = "New" } } };
DestroyableSingleton<HatManager>.Instance = replacement;
Check(OutfitPickerHandler.Search(OutfitCategory.Hat, "New").Count == 1, "manager replacement invalidates old native items");
Check(OutfitPickerHandler.GetCatalog(OutfitCategory.Pet).Single().Id == "", "empty native category still supports Clear");
DestroyableSingleton<HatManager>.Instance = null;
Check(OutfitPickerHandler.GetCatalog(OutfitCategory.Hat).Count == 0, "unavailable manager clears stale catalog");

Reset();
var beforeEquip = Saved();
OutfitPickerHandler.Equip(Item(OutfitCategory.Hat, "hat_second"));
Check(DataManager.Player.Customization.Hat == "hat_second", "main-menu selected hat saved");
Check(Saved().Replace("hat_second", "hat_first") == beforeEquip, "single-slot selection preserves all five other slots");
Check(OutfitPickerHandler.CanRestore, "first successful selection captures original outfit");
Check(OutfitPickerHandler.StatusText.Contains("Join a lobby"), "main-menu save does not claim network acceptance");
Unchanged(() => OutfitPickerHandler.Equip(Item(OutfitCategory.Hat, "hat_first")), "repeat clicks respect cooldown");
Time.realtimeSinceStartupAsDouble += 2;
OutfitPickerHandler.RestoreOriginal();
Check(Saved() == beforeEquip, "restore returns all six saved slots");
Time.realtimeSinceStartupAsDouble += 2;
OutfitPickerHandler.Clear(OutfitCategory.Nameplate);
Check(DataManager.Player.Customization.NamePlate == "", "nameplate Clear uses native empty selection");
Unchanged(() => OutfitPickerHandler.Clear(OutfitCategory.Color), "color cannot be cleared");

Reset(lobby: true);
OutfitPickerHandler.Equip(Item(OutfitCategory.Hat, "hat_second"));
Check(PlayerControl.LocalPlayer.Requests.SequenceEqual(new[] { ("Hat", "hat_second") }), "one normal hat request sent for explicit Equip");
Check(PlayerControl.LocalPlayer.CurrentOutfit.HatId == "hat_first", "recorded request is not treated as confirmed multiplayer equip");
Check(OutfitPickerHandler.IsEquipped(Item(OutfitCategory.Hat, "hat_first")), "equipped marker uses current actual player outfit");
Check(!OutfitPickerHandler.IsEquipped(Item(OutfitCategory.Hat, "hat_second")), "unaccepted requested item is not marked equipped");
Check(OutfitPickerHandler.StatusText.Contains("requested") && !OutfitPickerHandler.StatusText.Contains("confirmed"), "status distinguishes submission from acceptance");
Time.realtimeSinceStartupAsDouble += 2;
OutfitPickerHandler.Clear(OutfitCategory.Nameplate);
Check(PlayerControl.LocalPlayer.Requests.Last() == ("Nameplate", ""), "nameplate normal RPC uses one empty ID");
Time.realtimeSinceStartupAsDouble += 2;
OutfitPickerHandler.Equip(Item(OutfitCategory.Color, "2"));
Check(PlayerControl.LocalPlayer.Requests.Last() == ("Color", "2"), "body color uses normal host-mediated CmdCheckColor");
Check(ColorCycleHandler.Stops == 1, "explicit color selection stops existing color preview");
var other = new PlayerControl(); other.Data.DefaultOutfit.ColorId = 3;
PlayerControl.AllPlayerControls.Add(other);
Time.realtimeSinceStartupAsDouble += 2;
Unchanged(() => OutfitPickerHandler.Equip(Item(OutfitCategory.Color, "3")), "occupied lobby body color not saved or requested");
Check(OutfitPickerHandler.StatusText.Contains("already in use"), "occupied color reason is visible");
Time.realtimeSinceStartupAsDouble += 2;
OutfitPickerHandler.RestoreOriginal();
Check(PlayerControl.LocalPlayer.Requests.TakeLast(6).Select(x => x.Category).SequenceEqual(
    new[] { "Color", "Hat", "Skin", "Visor", "Pet", "Nameplate" }), "restore submits one normal six-slot batch");

Reset(practice: true);
OutfitPickerHandler.Equip(Item(OutfitCategory.Hat, "hat_second"));
Check(PlayerControl.LocalPlayer.Requests.Count == 1, "living Freeplay allows explicit outfit request");
Reset(lobby: true);
var selected = Item(OutfitCategory.Hat, "hat_second");
var guards = new (string Name, Action Block, Action Undo)[]
{
    ("panic", () => Mod.isPanicked = true, () => Mod.isPanicked = false),
    ("focus lost", () => Application.isFocused = false, () => Application.isFocused = true),
    ("meeting", () => Utils.isMeeting = true, () => Utils.isMeeting = false),
    ("exile", () => Utils.isExiling = true, () => Utils.isExiling = false),
    ("round started", () => Utils.isInGame = true, () => Utils.isInGame = false),
    ("native round started", () => AmongUsClient.Instance.GameState = InnerNet.InnerNetClient.GameStates.Started,
        () => AmongUsClient.Instance.GameState = InnerNet.InnerNetClient.GameStates.Joined),
    ("not owned", () => PlayerControl.LocalPlayer.AmOwner = false, () => PlayerControl.LocalPlayer.AmOwner = true),
    ("dead", () => PlayerControl.LocalPlayer.Data.IsDead = true, () => PlayerControl.LocalPlayer.Data.IsDead = false),
    ("disconnected", () => PlayerControl.LocalPlayer.Data.Disconnected = true, () => PlayerControl.LocalPlayer.Data.Disconnected = false),
    ("disguised", () => PlayerControl.LocalPlayer.CurrentOutfitType = PlayerOutfitType.Shapeshift,
        () => PlayerControl.LocalPlayer.CurrentOutfitType = PlayerOutfitType.Default),
    ("vent", () => PlayerControl.LocalPlayer.inVent = true, () => PlayerControl.LocalPlayer.inVent = false),
    ("ladder", () => PlayerControl.LocalPlayer.onLadder = true, () => PlayerControl.LocalPlayer.onLadder = false),
    ("platform", () => PlayerControl.LocalPlayer.inMovingPlat = true, () => PlayerControl.LocalPlayer.inMovingPlat = false),
    ("shapeshift transition", () => PlayerControl.LocalPlayer.shapeshifting = true, () => PlayerControl.LocalPlayer.shapeshifting = false)
};
foreach (var (name, block, undo) in guards)
{
    block(); Unchanged(() => OutfitPickerHandler.Equip(selected), "selection blocked during " + name);
    Check(!OutfitPickerHandler.CanChange(out var reason) && reason.Length > 0, "disabled UI reason for " + name);
    undo();
}
Unchanged(() => OutfitPickerHandler.Equip(new OutfitCatalogItem(OutfitCategory.Hat, "hat_unavailable", "Unavailable")),
    "unknown selected ID rejected before profile write");
Time.realtimeSinceStartupAsDouble = double.NaN;
Unchanged(() => OutfitPickerHandler.Equip(selected), "invalid clock does not mutate profile or request");
Time.realtimeSinceStartupAsDouble = 100;
OutfitPickerHandler.Equip(selected);
Time.realtimeSinceStartupAsDouble = 10;
Unchanged(() => OutfitPickerHandler.Clear(OutfitCategory.Hat), "clock rewind does not trigger burst request");
Time.realtimeSinceStartupAsDouble = 12;
OutfitPickerHandler.Clear(OutfitCategory.Hat);
Check(PlayerControl.LocalPlayer.Requests.Count == 2, "clock rewind recovery allows later explicit click");

Reset(lobby: true);
var beforeFailure = Saved();
DataManager.Player.ThrowSaves = 1;
OutfitPickerHandler.Equip(Item(OutfitCategory.Hat, "hat_second"));
Check(Saved() == beforeFailure && PlayerControl.LocalPlayer.Requests.Count == 0, "profile-save failure rolls back without sending RPC");
Check(!OutfitPickerHandler.CanRestore, "failed write never replaces original restore snapshot");
Time.realtimeSinceStartupAsDouble += 2;
PlayerControl.LocalPlayer.ThrowRequest = true;
OutfitPickerHandler.Equip(Item(OutfitCategory.Hat, "hat_second"));
Check(DataManager.Player.Customization.Hat == "hat_second" && OutfitPickerHandler.StatusText.Contains("No retry"),
    "failed native request keeps saved preference and reports partial outcome without retry");
Unchanged(() => OutfitPickerHandler.Equip(Item(OutfitCategory.Hat, "hat_first")), "request failure still consumes click cooldown");

Reset();
OutfitPickerHandler.SaveLook();
Check(OutfitPickerHandler.HasSavedLook && DataManager.Player.Saves == 0, "Save look captures session snapshot without game writes");
OutfitPickerHandler.Equip(Item(OutfitCategory.Hat, "hat_second"));
Time.realtimeSinceStartupAsDouble += 2;
OutfitPickerHandler.WearSavedLook();
Check(DataManager.Player.Customization.Hat == "hat_first", "saved look restores original captured slots");
OutfitPickerHandler.Equip(Item(OutfitCategory.Hat, "hat_second"));
DataManager.Player = new PlayerData();
Check(!OutfitPickerHandler.CanRestore && !OutfitPickerHandler.HasSavedLook, "snapshot never crosses a player-profile pointer change");
Unchanged(OutfitPickerHandler.RestoreOriginal, "different profile cannot be overwritten by old restore");
Unchanged(OutfitPickerHandler.WearSavedLook, "different profile cannot be overwritten by session saved look");

Reset();
DataManager.Player.Customization.Hat = HatData.EmptyId;
Check(OutfitPickerHandler.IsEquipped(Item(OutfitCategory.Hat, "")), "native empty cosmetic ID maps to Clear equipped marker");
var nativeSprite = new Sprite();
var previewHat = new HatData { ProductId = "hat_preview", Name = "Preview", PreviewData = new AssetReference { Asset = nativeSprite } };
var preview = new OutfitCatalogItem(OutfitCategory.Hat, "hat_preview", "Preview", previewHat);
Check(ReferenceEquals(OutfitPickerHandler.CachedSprite(preview), nativeSprite), "already loaded native preview sprite reused");
previewHat.PreviewData = null;
previewHat.ViewDataRef = new AssetReference { Asset = new HatViewData { MainImage = nativeSprite } };
Check(ReferenceEquals(OutfitPickerHandler.CachedSprite(preview), nativeSprite), "already loaded hat view sprite fallback reused");
previewHat.ViewDataRef = null;
Check(OutfitPickerHandler.CachedSprite(preview) == null, "unloaded preview returns placeholder without load action");
Check(OutfitPickerHandler.CachedSprite(Item(OutfitCategory.Hat, "")) == null, "empty slot requires no sprite asset");
Check(OutfitPickerHandler.BodyColor(Item(OutfitCategory.Color, "0")).r == 1, "color swatch uses native palette");
var petPreview = new OutfitCatalogItem(OutfitCategory.Pet, "pet_cached", "Cached pet", new PetData
{
    PetPrefabRef = new AssetReference { Asset = new GameObject
    { Component = new PetBehaviour { renderers = [new SpriteRenderer { sprite = nativeSprite }] } } }
});
Check(ReferenceEquals(OutfitPickerHandler.CachedSprite(petPreview), nativeSprite), "already loaded pet prefab read without instantiating it");
Reset();
OutfitPickerHandler.SaveLook();
DestroyableSingleton<HatManager>.Instance.Invalid.Add("hat_first");
OutfitPickerHandler.RefreshCatalog();
Unchanged(OutfitPickerHandler.WearSavedLook, "unavailable saved look cannot overwrite profile");
Check(OutfitPickerHandler.StatusText.Contains("unavailable"), "unavailable saved-look selection explains catalog change");
Reset(lobby: true);
var becameInvalid = Item(OutfitCategory.Hat, "hat_second");
DestroyableSingleton<HatManager>.Instance.Invalid.Add("hat_second");
Check(OutfitPickerHandler.GetCatalog(OutfitCategory.Hat).Any(x => x.Id == "hat_second"),
    "drawing cache is unchanged during native validity change");
Unchanged(() => OutfitPickerHandler.Equip(becameInvalid), "cached item is revalidated at click before save or RPC");
Check(OutfitPickerHandler.StatusText.Contains("unavailable"), "revalidated item explains native mode change");
OutfitPickerHandler.SaveLook();
DestroyableSingleton<HatManager>.Instance.Invalid.Add("skin_first");
Unchanged(OutfitPickerHandler.WearSavedLook, "saved-look batch revalidates every cosmetic despite drawing cache");
Console.WriteLine($"PASS {passed} outfit picker checks. Native requests are recorded only; live game acceptance and GUI rendering require Freeplay/multiplayer verification.");
