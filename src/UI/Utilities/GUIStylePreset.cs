using UnityEngine;

namespace HAU;

public static class GUIStylePreset
{
    private static GUIStyle _window, _button, _toggle, _label, _tabButton, _selectedTab,
        _title, _subtitle, _hint, _section, _context, _brand, _separator, _darkSeparator, _richLabel, _searchField;
    private static Texture2D _windowTexture, _cardTexture, _buttonTexture, _hoverTexture,
        _selectedTexture, _activeTexture;

    private static Color _accent = new(0.18f, 0.87f, 0.88f, 1f);
    private static bool _transparent;
    public static Color Accent => _accent;

    private static RectOffset Insets(int left, int right, int top, int bottom) =>
        new() { left = left, right = right, top = top, bottom = bottom };

    public static void SetAccent(Color color)
    {
        EnsureStyles();
        color.a = 1f;
        if (_accent == color) return;
        _accent = color;
        _window.normal.textColor = color;
        _window.onNormal.textColor = color;
        _button.hover.textColor = color;
        _toggle.onNormal.textColor = color;
        _toggle.onHover.textColor = color;
        _subtitle.normal.textColor = color;
        _context.normal.textColor = color;
        _brand.normal.textColor = color;
        RefreshBackgrounds();
    }

    public static void SetTransparentBackground(bool transparent)
    {
        EnsureStyles();
        if (_transparent == transparent) return;
        _transparent = transparent;
        RefreshBackgrounds();
    }

    private static void RefreshBackgrounds()
    {
        SetTexture(_windowTexture, new Color(0.035f, 0.055f, 0.075f), 0.72f);
        SetTexture(_cardTexture, new Color(0.08f, 0.11f, 0.14f), 0.72f);
        SetTexture(_buttonTexture, new Color(0.12f, 0.18f, 0.22f), 0.84f);
        SetTexture(_selectedTexture, Color.Lerp(new Color(0.05f, 0.12f, 0.14f), _accent, 0.3f), 0.88f);
        SetTexture(_hoverTexture, Color.Lerp(new Color(0.10f, 0.18f, 0.22f), _accent, 0.15f), 0.90f);
        SetTexture(_activeTexture, Color.Lerp(new Color(0.05f, 0.12f, 0.14f), _accent, 0.4f), 0.94f);
    }

    private static void SetTexture(Texture2D texture, Color color, float transparentAlpha)
    {
        color.a = _transparent ? transparentAlpha : 1f;
        texture.SetPixel(0, 0, color);
        texture.Apply();
    }

    private static Texture2D Solid(Color color)
    {
        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        return texture;
    }

    private static void EnsureStyles()
    {
        if (_window != null) return;
        _windowTexture = Solid(new Color(0.035f, 0.055f, 0.075f, 1f));
        _cardTexture = Solid(new Color(0.08f, 0.11f, 0.14f, 1f));
        _buttonTexture = Solid(new Color(0.12f, 0.18f, 0.22f, 1f));
        _hoverTexture = Solid(new Color(0.17f, 0.30f, 0.34f, 1f));
        _selectedTexture = Solid(new Color(0.06f, 0.37f, 0.40f, 1f));
        _activeTexture = Solid(new Color(0.12f, 0.46f, 0.49f, 1f));
        _label = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14, wordWrap = true, richText = false,
            normal = { textColor = new Color(0.90f, 0.95f, 0.97f) },
            margin = Insets(2, 2, 3, 3)
        };
        _richLabel = new GUIStyle(_label) { richText = true };
        _window = new GUIStyle(GUI.skin.window)
        {
            normal = { background = _windowTexture, textColor = Accent },
            onNormal = { background = _windowTexture, textColor = Accent },
            padding = Insets(12, 12, 28, 10),
            border = new RectOffset(), fontSize = 13
        };
        _button = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14, wordWrap = true, richText = false,
            normal = { background = _buttonTexture, textColor = Color.white },
            hover = { background = _hoverTexture, textColor = Accent },
            active = { background = _activeTexture, textColor = Color.white },
            onNormal = { background = _selectedTexture, textColor = Color.white },
            padding = Insets(10, 10, 5, 5),
            margin = Insets(2, 2, 3, 3), border = new RectOffset()
        };
        _toggle = new GUIStyle(GUI.skin.toggle)
        {
            fontSize = 14, wordWrap = true, richText = false,
            normal = { textColor = new Color(0.88f, 0.94f, 0.96f) },
            onNormal = { textColor = Accent }, hover = { textColor = Color.white },
            onHover = { textColor = Accent }, padding = Insets(22, 4, 3, 3),
            margin = Insets(2, 2, 4, 4)
        };
        _tabButton = new GUIStyle(_button)
        {
            alignment = TextAnchor.MiddleLeft, fontSize = 14,
            padding = Insets(11, 6, 4, 4), wordWrap = false,
            normal = { background = _windowTexture, textColor = new Color(0.70f, 0.79f, 0.83f) }
        };
        _selectedTab = new GUIStyle(_tabButton)
        {
            fontStyle = FontStyle.Bold,
            normal = { background = _selectedTexture, textColor = Color.white }
        };
        _searchField = new GUIStyle(_button)
        {
            alignment = TextAnchor.MiddleLeft, wordWrap = false, clipping = TextClipping.Clip
        };
        _title = new GUIStyle(_label)
        {
            fontSize = 22, fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }, margin = Insets(2, 2, 0, 7)
        };
        _subtitle = new GUIStyle(_label)
        {
            fontSize = 16, fontStyle = FontStyle.Bold, normal = { textColor = Accent },
            margin = Insets(2, 2, 2, 7)
        };
        _hint = new GUIStyle(_label)
        {
            fontSize = 13, normal = { textColor = new Color(0.65f, 0.75f, 0.80f) }
        };
        _section = new GUIStyle(GUI.skin.box)
        {
            normal = { background = _cardTexture }, border = new RectOffset(),
            padding = Insets(12, 12, 10, 10), margin = Insets(0, 0, 5, 8)
        };
        _context = new GUIStyle(_hint)
        {
            normal = { textColor = Accent }, alignment = TextAnchor.MiddleRight,
            wordWrap = false, fontStyle = FontStyle.Bold
        };
        _brand = new GUIStyle(_title) { fontSize = 25, normal = { textColor = Accent } };
        _separator = new GUIStyle(GUI.skin.box)
        {
            normal = { background = Texture2D.whiteTexture },
            margin = Insets(0, 0, 4, 4), padding = new RectOffset(), border = new RectOffset()
        };
        _darkSeparator = new GUIStyle(_separator) { normal = { background = _cardTexture } };
    }

    public static GUIStyle Window { get { EnsureStyles(); return _window; } }
    public static GUIStyle NormalButton { get { EnsureStyles(); return _button; } }
    public static GUIStyle NormalToggle { get { EnsureStyles(); return _toggle; } }
    public static GUIStyle Label { get { EnsureStyles(); return _label; } }
    public static GUIStyle RichLabel { get { EnsureStyles(); return _richLabel; } }
    public static GUIStyle TabButton { get { EnsureStyles(); return _tabButton; } }
    public static GUIStyle SelectedTab { get { EnsureStyles(); return _selectedTab; } }
    public static GUIStyle TabTitle { get { EnsureStyles(); return _title; } }
    public static GUIStyle TabSubtitle { get { EnsureStyles(); return _subtitle; } }
    public static GUIStyle Hint { get { EnsureStyles(); return _hint; } }
    public static GUIStyle SectionBody { get { EnsureStyles(); return _section; } }
    public static GUIStyle Context { get { EnsureStyles(); return _context; } }
    public static GUIStyle Brand { get { EnsureStyles(); return _brand; } }
    public static GUIStyle SearchField { get { EnsureStyles(); return _searchField; } }
    public static GUIStyle Separator { get { EnsureStyles(); return _separator; } }
    public static GUIStyle DarkSeparator { get { EnsureStyles(); return _darkSeparator; } }
}
