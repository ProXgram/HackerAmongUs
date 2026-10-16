using UnityEngine;

namespace HAU;

public class ColorsTab : ITab, IMenuPage
{
    public string name => "Outfits";
    private int _page;
    private readonly AnimationsTab _effects = new();

    public void OpenPage(int page)
    {
        _page = System.Math.Clamp(page, 0, 2);
        NicknameEntry.StopEditing();
    }

    public void Draw()
    {
        int previous = _page;
        _page = UIHelpers.Subtabs(_page, "Outfit & name", "Colors", "Effects");
        if (previous != _page) NicknameEntry.StopEditing();
        if (_page == 2) { _effects.Draw(); return; }
        if (_page == 1) { DrawColors(); return; }

        UIHelpers.BeginSection("Your outfit", "Choose individual items or try a random combination before a round or in Freeplay.");
        if (GUILayout.Button("Open full outfit picker"))
        {
            NicknameEntry.StopEditing();
            OutfitPickerUI.Open();
        }
        GUILayout.Label(OutfitPickerHandler.StatusText);
        CheatToggles.freeCosmetics = GUILayout.Toggle(CheatToggles.freeCosmetics, " Include extra cosmetic choices");
        UIHelpers.Hint("Availability and other players' view depend on the game's cosmetic checks.");
        if (GUILayout.Button("Pick random outfit")) RandomOutfitHandler.Apply();
        if (RandomOutfitHandler.CanRestore && GUILayout.Button("Restore previous outfit")) RandomOutfitHandler.Restore();
        GUILayout.Label(RandomOutfitHandler.StatusText);
        UIHelpers.EndSection();

        UIHelpers.BeginSection("Your nickname", "Type a draft, then apply it before the round. The game checks the submitted name.");
        NicknameEntry.Draw();
        if (GUILayout.Button("Apply my nickname"))
        {
            NicknameEntry.StopEditing();
            NicknamePreset.Apply();
        }
        GUILayout.Label(NicknamePreset.StatusText);
        UIHelpers.EndSection();
    }

    private static void DrawColors()
    {
        UIHelpers.BeginSection("Rainbow preview", "Cycles your body color on your screen during lobbies and rounds.");
        DrawMode("Rainbow preview", ColorCycleMode.LocalRainbow);
        float interval = Mathf.Round(GUILayout.HorizontalSlider(HAUPlugin.rainbowColorInterval.Value, 0.15f, 3f) * 100f) / 100f;
        if (!Mathf.Approximately(interval, HAUPlugin.rainbowColorInterval.Value))
            HAUPlugin.rainbowColorInterval.Value = interval;
        GUILayout.Label("Preview interval: " + interval.ToString("0.00") + " seconds");
        UIHelpers.Hint("Keeps your hat, skin, visor and pet. Only your screen sees this preview.");
        UIHelpers.EndSection();

        UIHelpers.BeginSection("Lobby color cycling", "Requests available colors for your own player before a round.");
        DrawMode("Lobby color cycle", ColorCycleMode.LobbyCycle);
        float lobby = Mathf.Round(GUILayout.HorizontalSlider(HAUPlugin.lobbyColorInterval.Value, 2f, 10f) * 10f) / 10f;
        if (!Mathf.Approximately(lobby, HAUPlugin.lobbyColorInterval.Value))
            HAUPlugin.lobbyColorInterval.Value = lobby;
        GUILayout.Label("Lobby interval: " + lobby.ToString("0.0") + " seconds");
        UIHelpers.Hint("Uses normal color selection and stops when the round starts. Shared appearance remains untested.");
        UIHelpers.EndSection();
        if (GUILayout.Button("Stop color cycling / F8")) ColorCycleHandler.Stop();
        GUILayout.Label(ColorCycleHandler.StatusText);
    }

    private static void DrawMode(string label, ColorCycleMode mode)
    {
        bool active = ColorCycleHandler.Mode == mode;
        if (GUILayout.Button((active ? "Stop " : "Start ") + label))
        {
            if (active) ColorCycleHandler.Stop();
            else ColorCycleHandler.Start(mode);
        }
    }
}
