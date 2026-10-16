using UnityEngine;
using System.Reflection;

namespace HAU;

public class AboutTab : ITab
{
    public string name => "About";

    public void Draw()
    {
        UIHelpers.BeginSection(ModBranding.Name, ModBranding.ShortName + " " + ModBranding.Version + " • By " + ModBranding.Creator);
        UIHelpers.Hint("Player tools, movement, outfit selection and host controls in one organized menu.");
        GUILayout.Label("Among Us version: " + Application.version);
        var loader = typeof(BepInEx.Paths).Assembly;
        var loaderVersion = loader.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? loader.GetName().Version?.ToString() ?? "unavailable";
        GUILayout.Label("BepInEx: " + loaderVersion.Split('+')[0]);
        GUILayout.Label("Open HAU: " + HAUPlugin.menuKeybind.Value);
        UIHelpers.EndSection();
        UIHelpers.BeginSection("Getting started", "Use Find a feature to open any tool, or choose a category on the left.");
        UIHelpers.Hint("Movement: walking, sprint and player stunts.");
        UIHelpers.Hint("Outfits: your outfit, nickname, colours and visual effects.");
        UIHelpers.Hint("ESP: role/player labels, map markers, tracers and camera controls.");
        UIHelpers.Hint("Tasks and Abilities: personal task and role tools.");
        UIHelpers.Hint("Sabotage: sabotage requests, repairs, doors and meeting tools.");
        UIHelpers.Hint("Host Controls: shared settings for a lobby you host.");
        UIHelpers.Hint("Settings: keybinds, profiles, colours and automatic updates.");
        UIHelpers.EndSection();
        UIHelpers.BeginSection("Project", "HAU is maintained by " + ModBranding.Creator + ". Distributed license notices are included with the download.");
        if (GUILayout.Button("Open the HAU project")) Application.OpenURL("https://github.com/ProXgram/HackerAmongUs");
        UIHelpers.EndSection();
    }
}
