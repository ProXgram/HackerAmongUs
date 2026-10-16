using UnityEngine;

namespace HAU;

public static class UIHelpers
{
    public static void RefreshTheme()
    {
        GUIStylePreset.SetTransparentBackground(HAUPlugin.menuTransparentBackground.Value);
        if (CheatToggles.rgbMode)
        {
            GUIStylePreset.SetAccent(Color.HSVToRGB(MenuUI.hue, 0.72f, 0.90f));
            return;
        }
        string value = HAUPlugin.menuHtmlColor.Value ?? "";
        if (!value.StartsWith("#")) value = "#" + value;
        GUIStylePreset.SetAccent(ColorUtility.TryParseHtmlString(value, out var color)
            ? color : new Color(0.2f, 0.82f, 0.9f, 1f));
    }

    public static ThemeScope UseTheme(bool richText = true) => new(richText);

    public sealed class ThemeScope : System.IDisposable
    {
        private readonly GUIStyle _button, _toggle, _label;
        private readonly Color _background, _content;
        public ThemeScope(bool richText)
        {
            RefreshTheme();
            _button = GUI.skin.button;
            _toggle = GUI.skin.toggle;
            _label = GUI.skin.label;
            _background = GUI.backgroundColor;
            _content = GUI.contentColor;
            GUI.skin.button = GUIStylePreset.NormalButton;
            GUI.skin.toggle = GUIStylePreset.NormalToggle;
            GUI.skin.label = richText ? GUIStylePreset.RichLabel : GUIStylePreset.Label;
            GUI.backgroundColor = Color.white;
            GUI.contentColor = Color.white;
        }
        public void Dispose()
        {
            GUI.skin.button = _button;
            GUI.skin.toggle = _toggle;
            GUI.skin.label = _label;
            GUI.backgroundColor = _background;
            GUI.contentColor = _content;
        }
    }

    public static void BeginSection(string title, string description = null)
    {
        GUILayout.BeginVertical(GUIStylePreset.SectionBody);
        GUILayout.Label(title, GUIStylePreset.TabSubtitle);
        if (!string.IsNullOrEmpty(description)) Hint(description);
    }

    public static void EndSection() => GUILayout.EndVertical();
    public static void Hint(string text) => GUILayout.Label(text, GUIStylePreset.Hint);

    public static void Action(string label, ref bool request, bool available = true)
    {
        bool original = GUI.enabled;
        GUI.enabled = original && available;
        if (GUILayout.Button(label)) request = true;
        GUI.enabled = original;
    }

    public static void ChoosePlayer(string label, ref bool request, bool available = true)
    {
        bool original = GUI.enabled;
        GUI.enabled = original && available;
        if (GUILayout.Button(label))
        {
            request = true;
            MenuUI.CloseMenuFromPointer();
        }
        GUI.enabled = original;
    }

    public static int Subtabs(int selected, params string[] labels)
    {
        bool horizontal = MenuUI.ContentWidth >= 300f;
        if (horizontal) GUILayout.BeginHorizontal();
        for (int i = 0; i < labels.Length; i++)
            if (GUILayout.Button(labels[i], i == selected ? GUIStylePreset.SelectedTab : GUIStylePreset.NormalButton,
                GUILayout.Height(34f))) selected = i;
        if (horizontal) GUILayout.EndHorizontal();
        GUILayout.Space(5f);
        return selected;
    }

    public static void ApplyUIColor()
    {
        if (CheatToggles.rgbMode)
        {
            GUI.backgroundColor = Color.HSVToRGB(MenuUI.hue, 1f, 1f);
        }
        else
        {
            var configHtmlColor = HAUPlugin.menuHtmlColor.Value;

            if (!ColorUtility.TryParseHtmlString(configHtmlColor, out var uiColor))
            {
                if (!configHtmlColor.StartsWith("#"))
                {
                    if (ColorUtility.TryParseHtmlString("#" + configHtmlColor, out uiColor))
                    {
                        GUI.backgroundColor = uiColor;
                    }
                }
            }
            else
            {
                GUI.backgroundColor = uiColor;
            }
        }
    }
}
