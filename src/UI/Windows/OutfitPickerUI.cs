using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HAU;

public class OutfitPickerUI : MonoBehaviour
{
    private static readonly OutfitCategory[] Categories =
        { OutfitCategory.Color, OutfitCategory.Hat, OutfitCategory.Skin, OutfitCategory.Visor, OutfitCategory.Pet, OutfitCategory.Nameplate };
    public static Rect windowRect;
    public static bool IsOpen { get; private set; }
    private static bool _resetView;
    private static bool _editingSearch, _selectSearchAll;
    private const int SearchMaximumLength = 128;
    private const int SearchPreviewLength = 30;
    private int _lastSearchInputFrame = -1;
    private OutfitCategory _category = OutfitCategory.Hat;
    private string _query = string.Empty;
    private int _page;
    private OutfitCatalogItem _selection;
    private List<OutfitCatalogItem> _results = new();
    private bool _filterDirty = true;
    private IReadOnlyList<OutfitCatalogItem> _lastCatalog;
    private Vector2 _detailScroll, _catalogScroll, _categoryScroll;
    private GUIStyle _card, _chosenCard, _searchStyle;
    private static GUIStyle _thumbnailStyle;

    public static void Open()
    {
        OutfitPickerHandler.RefreshCatalog();
        _resetView = true;
        StopSearchEditing();
        IsOpen = true;
    }

    public static void Close()
    {
        IsOpen = false;
        StopSearchEditing();
    }

    public static void CloseFromPointer()
    {
        PointerInputGuard.ConsumeGuiClose("OutfitPicker.Close", windowRect);
        Close();
    }

    private void Update()
    {
        if (!IsOpen) return;
        if (!MenuUI.isGUIActive || HAUPlugin.isPanicked || Input.GetKeyDown(KeyCode.Escape))
        { Close(); return; }
        if (!Application.isFocused) { StopSearchEditing(); return; }
        TickSearchInput();
    }

    private void OnGUI()
    {
        if (!IsOpen || !MenuUI.isGUIActive || HAUPlugin.isPanicked) return;
        using var theme = UIHelpers.UseTheme(false);
        var width = Mathf.Min(860f, Mathf.Max(580f, Screen.width - 30f));
        var height = Mathf.Min(720f, Mathf.Max(420f, Screen.height - 40f));
        if (_card == null)
        {
            _card = new GUIStyle(GUIStylePreset.SectionBody)
            {
                padding = new RectOffset { left = 4, right = 4, top = 4, bottom = 4 },
                margin = new RectOffset { left = 2, right = 2, top = 3, bottom = 3 }
            };
            _chosenCard = new GUIStyle(_card) { normal = { background = GUIStylePreset.SelectedTab.normal.background } };
            _searchStyle = new GUIStyle(GUIStylePreset.NormalButton)
            {
                alignment = TextAnchor.MiddleLeft, richText = false,
                clipping = TextClipping.Clip, wordWrap = false
            };
        }
        if (_resetView)
        {
            windowRect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);
            _query = string.Empty; _page = 0; _selection = null; _filterDirty = true; _resetView = false;
            _lastSearchInputFrame = -1;
        }
        windowRect.width = width; windowRect.height = height;
        windowRect.x = Mathf.Clamp(windowRect.x, 0, Mathf.Max(0, Screen.width - width));
        windowRect.y = Mathf.Clamp(windowRect.y, 0, Mathf.Max(0, Screen.height - height));
        windowRect = GUI.Window((int)WindowId.OutfitPickerUI, windowRect,
            (GUI.WindowFunction)DrawWindow, "HAU / OUTFIT STUDIO", GUIStylePreset.Window);
    }

    private void DrawWindow(int id)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Outfit studio", GUIStylePreset.TabTitle);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Refresh", GUILayout.Width(95)))
        {
            OutfitPickerHandler.RefreshCatalog(); _selection = null; _filterDirty = true;
            StopSearchEditing();
        }
        if (GUILayout.Button("Close", GUILayout.Width(75))) CloseFromPointer();
        GUILayout.EndHorizontal();
        GUILayout.Label("Browse every available slot. Select an item to inspect it; Equip changes only that slot.", GUIStylePreset.Hint);

        var bodyHeight = Mathf.Max(210, windowRect.height - 220);
        GUILayout.BeginHorizontal(GUILayout.Height(bodyHeight));
        GUILayout.BeginVertical(GUIStylePreset.SectionBody, GUILayout.Width(125), GUILayout.Height(bodyHeight));
        GUILayout.Label("Categories", GUIStylePreset.TabSubtitle);
        _categoryScroll = GUILayout.BeginScrollView(_categoryScroll);
        foreach (var category in Categories)
        {
            var count = OutfitPickerHandler.GetCatalog(category).Count;
            var style = category == _category ? GUIStylePreset.SelectedTab : GUIStylePreset.TabButton;
            if (GUILayout.Button(CategoryName(category) + "\n" + count + " choices", style, GUILayout.Height(55)))
            {
                _category = category; _page = 0; _selection = null; _filterDirty = true;
                _detailScroll = Vector2.zero; StopSearchEditing(); GUI.FocusControl(null);
            }
        }
        GUILayout.EndScrollView();
        GUILayout.EndVertical();

        var middleWidth = Mathf.Max(255, (windowRect.width - 158) * 0.57f);
        GUILayout.BeginVertical(GUILayout.Width(middleWidth), GUILayout.Height(bodyHeight));
        GUILayout.BeginHorizontal();
        GUILayout.Label("Search", GUILayout.Width(50));
        var searchLabel = _query.Length == 0 ? "Click to search" : SearchPreview(_query);
        if (GUILayout.Button((_editingSearch ? "Typing: " : "") + searchLabel, _searchStyle,
            GUILayout.Height(34), GUILayout.ExpandWidth(true)))
        {
            _editingSearch = true; _selectSearchAll = false; _lastSearchInputFrame = -1;
            GUI.FocusControl(null);
        }
        if (GUILayout.Button("Paste", GUILayout.Width(60)))
        { PasteSearch(); _editingSearch = true; GUI.FocusControl(null); }
        if (GUILayout.Button("Clear", GUILayout.Width(50)))
        { SetQuery(string.Empty); _editingSearch = true; _selectSearchAll = false; GUI.FocusControl(null); }
        GUILayout.EndHorizontal();
        GUILayout.Label(_editingSearch
            ? _selectSearchAll ? "All selected. Type to replace; Enter finishes."
                : "Type to search. Ctrl+A selects all; Enter finishes."
            : "Click Search to type a name or catalog ID.", GUIStylePreset.Hint);
        var catalog = OutfitPickerHandler.GetCatalog(_category);
        if (!ReferenceEquals(catalog, _lastCatalog))
        { _filterDirty = true; _lastCatalog = catalog; _selection = null; }
        if (_filterDirty) { _results = OutfitPickerHandler.Search(_category, _query); _filterDirty = false; }
        var pages = Math.Max(1, (_results.Count + OutfitPickerHandler.PageSize - 1) / OutfitPickerHandler.PageSize);
        _page = Math.Clamp(_page, 0, pages - 1);
        GUILayout.Label(CategoryName(_category) + " / " + _results.Count + " matching choices", GUIStylePreset.TabSubtitle);

        _catalogScroll = GUILayout.BeginScrollView(_catalogScroll);
        for (var row = 0; row < OutfitPickerHandler.PageSize / 2; row++)
        {
            GUILayout.BeginHorizontal();
            for (var column = 0; column < 2; column++)
            {
                var index = _page * OutfitPickerHandler.PageSize + row * 2 + column;
                if (index < _results.Count) DrawChoice(_results[index], (middleWidth - 10) / 2);
                else GUILayout.Space((middleWidth - 10) / 2);
            }
            GUILayout.EndHorizontal();
        }
        if (_results.Count == 0) GUILayout.Label(string.IsNullOrWhiteSpace(_query)
            ? "The game catalog is not ready. Open a lobby or Freeplay, then Refresh."
            : "No matching items. Try a shorter search or Refresh the catalog.", GUIStylePreset.Hint);
        GUILayout.EndScrollView();
        GUILayout.BeginHorizontal();
        var enabled = GUI.enabled;
        GUI.enabled = enabled && _page > 0;
        if (GUILayout.Button("Previous", GUILayout.Width(95))) { _page--; _selection = null; StopSearchEditing(); }
        GUI.enabled = enabled;
        GUILayout.Label(($"Page {_page + 1} / {pages}"), GUIStylePreset.Hint);
        GUI.enabled = enabled && _page < pages - 1;
        if (GUILayout.Button("Next", GUILayout.Width(75))) { _page++; _selection = null; StopSearchEditing(); }
        GUI.enabled = enabled;
        GUILayout.EndHorizontal();
        GUILayout.Label("Available previews are shown. Text choices remain usable when a preview cannot be drawn.", GUIStylePreset.Hint);
        GUILayout.EndVertical();

        GUILayout.BeginVertical(GUIStylePreset.SectionBody, GUILayout.ExpandWidth(true), GUILayout.Height(bodyHeight));
        DrawDetails();
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        var previousEnabled = GUI.enabled;
        GUI.enabled = previousEnabled && OutfitPickerHandler.CanChange(out _);
        if (GUILayout.Button("Save current look", GUILayout.Width(160))) OutfitPickerHandler.SaveLook();
        GUI.enabled = previousEnabled && OutfitPickerHandler.HasSavedLook;
        if (GUILayout.Button("Wear saved look", GUILayout.Width(150))) OutfitPickerHandler.WearSavedLook();
        GUI.enabled = previousEnabled && OutfitPickerHandler.CanRestore;
        if (GUILayout.Button("Restore original outfit", GUILayout.Width(195))) OutfitPickerHandler.RestoreOriginal();
        GUI.enabled = previousEnabled;
        GUILayout.EndHorizontal();
        GUILayout.Label(OutfitPickerHandler.StatusText, GUIStylePreset.Hint);
        GUI.DragWindow(new Rect(0, 0, windowRect.width, 26));
    }

    [Il2CppInterop.Runtime.Attributes.HideFromIl2Cpp]
    private void DrawChoice(OutfitCatalogItem item, float width)
    {
        var chosen = _selection != null && _selection.Category == item.Category && _selection.Id == item.Id;
        var equipped = OutfitPickerHandler.IsEquipped(item);
        GUILayout.BeginVertical(chosen ? _chosenCard : _card,
            GUILayout.Width(width), GUILayout.Height(63));
        GUILayout.BeginHorizontal();
        var thumbnailRect = GUILayoutUtility.GetRect(37, 37, GUILayout.Width(37), GUILayout.Height(37));
        DrawThumbnail(item, thumbnailRect);
        if (GUILayout.Button(item.Name + (equipped ? "\nEquipped" : string.Empty),
            GUIStylePreset.NormalButton,
            GUILayout.Height(45), GUILayout.ExpandWidth(true)))
        { _selection = item; _detailScroll = Vector2.zero; StopSearchEditing(); GUI.FocusControl(null); }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }

    private void DrawDetails()
    {
        _detailScroll = GUILayout.BeginScrollView(_detailScroll);
        GUILayout.Label("ITEM DETAILS", GUIStylePreset.TabSubtitle);
        if (_selection == null) GUILayout.Label("Select an item to see its preview and details.", GUIStylePreset.Hint);
        else
        {
            var previewRect = GUILayoutUtility.GetRect(98, 98, GUILayout.Width(98), GUILayout.Height(98));
            DrawThumbnail(_selection, previewRect);
            GUILayout.Label(_selection.Name, GUIStylePreset.TabSubtitle);
            GUILayout.Label(CategoryName(_selection.Category), GUIStylePreset.Hint);
            if (_selection.Id.Length > 0 && _selection.Category != OutfitCategory.Color)
                GUILayout.Label("Catalog ID: " + _selection.Id, GUIStylePreset.Hint);
            GUILayout.Label(OutfitPickerHandler.IsEquipped(_selection) ? "Currently equipped" : "Ready to equip", GUIStylePreset.Hint);
            var enabled = GUI.enabled;
            GUI.enabled = enabled && OutfitPickerHandler.CanChange(out _);
            if (GUILayout.Button("Equip selected", GUILayout.Height(36))) OutfitPickerHandler.Equip(_selection);
            GUI.enabled = enabled;
        }
        if (_category != OutfitCategory.Color)
        {
            var enabled = GUI.enabled;
            GUI.enabled = enabled && OutfitPickerHandler.CanChange(out _);
            if (GUILayout.Button("Clear " + CategoryName(_category).ToLowerInvariant())) OutfitPickerHandler.Clear(_category);
            GUI.enabled = enabled;
        }
        if (!OutfitPickerHandler.CanChange(out var reason)) GUILayout.Label(reason, GUIStylePreset.Hint);
        GUILayout.Space(12);
        GUILayout.Label("CURRENT OUTFIT", GUIStylePreset.TabSubtitle);
        foreach (var category in Categories)
        {
            GUILayout.Label(CategoryName(category), GUIStylePreset.Hint);
            GUILayout.Label(OutfitPickerHandler.EquippedName(category), GUIStylePreset.Label);
        }
        GUILayout.Space(10);
        GUILayout.Label("Saved look and Restore original are kept for this player profile during this session.", GUIStylePreset.Hint);
        GUILayout.Label("Uses your available catalog. The game still checks cosmetic and color requests.", GUIStylePreset.Hint);
        GUILayout.EndScrollView();
    }

    private static string CategoryName(OutfitCategory category) => category switch
    {
        OutfitCategory.Hat => "Hats", OutfitCategory.Skin => "Skins", OutfitCategory.Visor => "Visors",
        OutfitCategory.Pet => "Pets", OutfitCategory.Nameplate => "Nameplates", _ => "Body color"
    };



    private void TickSearchInput()
    {
        if (!_editingSearch || _lastSearchInputFrame == Time.frameCount) return;
        _lastSearchInputFrame = Time.frameCount;
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            if (Input.GetKeyDown(KeyCode.A)) _selectSearchAll = true;
            else if (Input.GetKeyDown(KeyCode.V)) PasteSearch();
            return;
        }
        var input = Input.inputString;
        if (string.IsNullOrEmpty(input)) return;
        var text = new StringBuilder(_query);
        for (var i = 0; i < input.Length; i++)
        {
            var character = input[i];
            if (character is '\r' or '\n') { StopSearchEditing(); break; }
            if (character == '\b')
            {
                if (_selectSearchAll) { text.Clear(); _selectSearchAll = false; }
                else RemoveSearchLast(text);
                continue;
            }
            if (!SearchCharacterAllowed(character) || char.IsLowSurrogate(character)) continue;
            var length = 1;
            if (char.IsHighSurrogate(character))
            {
                if (i + 1 >= input.Length || !char.IsLowSurrogate(input[i + 1])) continue;
                length = 2;
            }
            if (_selectSearchAll) { text.Clear(); _selectSearchAll = false; }
            if (text.Length + length <= SearchMaximumLength)
            {
                text.Append(character);
                if (length == 2) text.Append(input[i + 1]);
            }
            if (length == 2) i++;
        }
        SetQuery(text.ToString());
    }

    private void PasteSearch()
    {
        var previous = _selectSearchAll ? string.Empty : _query;
        SetQuery(FilterSearch(previous + (GUIUtility.systemCopyBuffer ?? string.Empty)));
        _selectSearchAll = false;
    }

    private void SetQuery(string value)
    {
        if (_query == value) return;
        _query = value; _page = 0; _filterDirty = true; _catalogScroll = Vector2.zero;
    }

    private static void StopSearchEditing()
    {
        _editingSearch = false; _selectSearchAll = false;
    }

    private static string FilterSearch(string value)
    {
        var result = new StringBuilder(Math.Min(value.Length, SearchMaximumLength));
        for (var i = 0; i < value.Length && result.Length < SearchMaximumLength; i++)
        {
            var character = value[i];
            if (!SearchCharacterAllowed(character) || char.IsLowSurrogate(character)) continue;
            if (char.IsHighSurrogate(character))
            {
                if (i + 1 >= value.Length || !char.IsLowSurrogate(value[i + 1])) continue;
                if (result.Length + 2 > SearchMaximumLength) break;
                result.Append(character).Append(value[++i]);
            }
            else result.Append(character);
        }
        return result.ToString();
    }

    private static bool SearchCharacterAllowed(char value) =>
        !char.IsControl(value) && value is not '\u2028' and not '\u2029';

    private static void RemoveSearchLast(StringBuilder value)
    {
        if (value.Length == 0) return;
        var count = char.IsLowSurrogate(value[value.Length - 1]) && value.Length >= 2 &&
            char.IsHighSurrogate(value[value.Length - 2]) ? 2 : 1;
        value.Length -= count;
    }

    private static string SearchPreview(string value)
    {
        if (value.Length <= SearchPreviewLength) return value;
        var end = SearchPreviewLength;
        if (char.IsHighSurrogate(value[end - 1])) end--;
        return value.Substring(0, end) + "...";
    }

    private static void DrawThumbnail(OutfitCatalogItem item, Rect rect)
    {
        if (Event.current.type != EventType.Repaint) return;
        var previousColor = GUI.color;
        bool drawn = false;
        try
        {
            _thumbnailStyle ??= new GUIStyle(GUIStylePreset.Label)
            {
                border = new RectOffset(), padding = new RectOffset(), margin = new RectOffset(),
                alignment = TextAnchor.MiddleCenter
            };
            if (item.Category == OutfitCategory.Color)
            {
                GUI.color = OutfitPickerHandler.BodyColor(item);
                DrawThumbnailBox(rect, Texture2D.whiteTexture);
                drawn = true;
            }
            else
            {
                var sprite = OutfitPickerHandler.CachedSprite(item);
                if (sprite && sprite.texture &&
                    (!sprite.packed || (sprite.packingMode != SpritePackingMode.Tight &&
                        sprite.packingRotation == SpritePackingRotation.None)))
                {
                    var texture = sprite.texture;
                    var source = sprite.textureRect;



                    if (texture.width > 0 && texture.height > 0 && source.width > 0 && source.height > 0 &&
                        Mathf.Approximately(source.x, 0) && Mathf.Approximately(source.y, 0) &&
                        Mathf.Approximately(source.width, texture.width) && Mathf.Approximately(source.height, texture.height))
                    {
                        var scale = Mathf.Min(rect.width / source.width, rect.height / source.height);
                        var target = new Rect(rect.center.x - source.width * scale / 2, rect.center.y - source.height * scale / 2,
                            source.width * scale, source.height * scale);
                        DrawThumbnailBox(target, texture);
                        drawn = true;
                    }
                }
            }
        }
        catch (Exception) { }
        finally { GUI.color = previousColor; }
        if (!drawn)
        {


            try { GUI.Label(rect, item.Id.Length == 0 ? "∅" : "—", GUIStylePreset.Hint); }
            catch (Exception) { }
        }
    }

    private static void DrawThumbnailBox(Rect rect, Texture2D texture)
    {
        _thumbnailStyle.normal.background = texture;
        _thumbnailStyle.hover.background = texture;
        _thumbnailStyle.active.background = texture;
        _thumbnailStyle.focused.background = texture;


        GUI.Box(rect, GUIContent.none, _thumbnailStyle);
    }
}
