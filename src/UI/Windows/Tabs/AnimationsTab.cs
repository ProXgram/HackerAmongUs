using UnityEngine;

namespace HAU;

public class AnimationsTab : ITab
{
    public string name => "Effects";

    public void Draw()
    {
        UIHelpers.BeginSection("Task effects", "Visual effect requests depend on the current map, role and host checks.");
        UIHelpers.Action("Play shield animation / Skeld", ref CheatToggles.animShields, Utils.isShip);
        UIHelpers.Action("Empty garbage animation / Skeld", ref CheatToggles.animEmptyGarbage, Utils.isShip);
        CheatToggles.animAsteroids = GUILayout.Toggle(CheatToggles.animAsteroids, " Repeat asteroid firing / Skeld or Polus");
        CheatToggles.animMedScan = GUILayout.Toggle(CheatToggles.animMedScan, " Show Medbay scan effect");
        CheatToggles.animCamsInUse = GUILayout.Toggle(CheatToggles.animCamsInUse, " Show security cameras in use");
        UIHelpers.Hint("Effects do not complete the matching task. Camera effects are unavailable on Mira HQ and Fungle.");
        UIHelpers.EndSection();
        UIHelpers.BeginSection("Movement appearance", "A local visual effect on your own screen.");
        CheatToggles.moonWalk = GUILayout.Toggle(CheatToggles.moonWalk, " Moonwalk");
        UIHelpers.EndSection();
    }
}
